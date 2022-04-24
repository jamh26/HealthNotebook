using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using HealthNotebook.Authentication.Configuration;
using HealthNotebook.Authentication.Models.DTO.Generic;
using HealthNotebook.Authentication.Models.DTO.Incoming;
using HealthNotebook.Authentication.Models.DTO.Outgoing;
using HealthNotebook.DataService.IConfiguration;
using HealthNotebook.Entities.DbSet;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HealthNotebook.Api.Controllers.v1;

public class AccountsController : BaseController
{
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly JwtConfig _jwtConfig;

    public AccountsController(
        IUnitOfWork unitOfWork,
        UserManager<IdentityUser> userManager,
        TokenValidationParameters tokenValidationParameters,
        IOptionsMonitor<JwtConfig> optionMonitor,
        IMapper mapper) : base(unitOfWork, userManager, mapper)
    {
        _jwtConfig = optionMonitor.CurrentValue;
        _tokenValidationParameters = tokenValidationParameters;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="registrationDto"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("Register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto registrationDto)
    {
        // check the model or object we are receiving is valid
        if (ModelState.IsValid)
        {
            // Check if the email already exists
            var userExist = await _userManager.FindByEmailAsync(registrationDto.Email);

            if (userExist != null)
            {
                return BadRequest(new UserRegistrationResponseDto()
                {
                    Success = false,
                    Errors = new List<string>(){
                            "Email already in use"
                        }
                });
            }

            // Add the user
            var newUser = new IdentityUser()
            {
                Email = registrationDto.Email,
                UserName = registrationDto.Email,
                EmailConfirmed = true // ToDo build email functionality to send to the user to confirm email
            };

            // Adding the user to the table
            IdentityResult isCreated = await _userManager.CreateAsync(newUser, registrationDto.Password);
            if (!isCreated.Succeeded) // when the registration has failed
            {
                return BadRequest(new UserRegistrationResponseDto()
                {
                    Success = isCreated.Succeeded,
                    Errors = isCreated.Errors.Select(x => x.Description).ToList()
                });
            }

            // Adding user to the database
            var _user = new User();
            _user.IdentityId = new Guid(newUser.Id);
            _user.LastName = registrationDto.LastName;
            _user.FirstName = registrationDto.FirstName;
            _user.Email = registrationDto.Email;
            _user.DateOfBirth = DateTime.UtcNow;//Convert.ToDateTime(user.DateOfBirth);
            _user.Country = "";
            _user.Phone = "";
            _user.Status = 1;

            await _unitOfWork.Users.Add(_user);
            await _unitOfWork.CompleteAsync();


            // Create a jwt token
            var token = await GenerateJwtToken(newUser);

            // return back to the user
            return Ok(new UserRegistrationResponseDto()
            {
                Success = true,
                Token = token.JwtToken,
                RefreshToken = token.RefreshToken
            });
        }
        else // Invalid Object
        {
            return BadRequest(new UserRegistrationResponseDto()
            {
                Success = false,
                Errors = new List<string>(){
                        "Invalid payload"
                    }
            });
        }
    }

    [HttpPost]
    [Route("Login")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequestDto loginDto)
    {
        if (ModelState.IsValid)
        {
            var userExist = await _userManager.FindByEmailAsync(loginDto.Email);

            // 1 - Check if the user exists
            if (userExist == null)
            {
                return BadRequest(new UserLoginResponseDto()
                {
                    Success = false,
                    Errors = new List<string>()
                        {
                            "Invalid authentication request"
                        }
                });
            }

            // 2 - Check if the user has a valid password
            var isCorrect = await _userManager.CheckPasswordAsync(userExist, loginDto.Password);

            if (isCorrect)
            {
                // We need to generate a Jwt Token
                var jwtToken = await GenerateJwtToken(userExist);

                return Ok(new UserLoginResponseDto()
                {
                    Success = true,
                    Token = jwtToken.JwtToken,
                    RefreshToken = jwtToken.RefreshToken
                });
            }
            else
            {
                // Password doesn't match
                return BadRequest(new UserLoginResponseDto()
                {
                    Success = false,
                    Errors = new List<string>()
                        {
                            "Invalid authentication request"
                        }
                });
            }
        }
        else // Invalid auth request
        {
            return BadRequest(new UserLoginResponseDto()
            {
                Success = false,
                Errors = new List<string>()
                        {
                            "Invalid authentication request"
                        }
            });
        }
    }

    /// <summary>
    /// IActionResult POST method to generate a new RefreshToken
    /// </summary>
    /// <param name="tokenRequestDto">TokenRequestDto containing the JWT token to verify</param>
    /// <returns>201 OK if token is valid, 400 if token validation fails</returns>
    [HttpPost]
    [Route("RefreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto tokenRequestDto)
    {
        if (ModelState.IsValid)
        {
            // Check if the token is valid
            var result = await VerifyToken(tokenRequestDto);

            if (result == null)
            {
                return BadRequest(new UserLoginResponseDto()
                {
                    Success = false,
                    Errors = new List<string>()
                            {
                                "Token Validation Failed"
                            }
                });
            }

            return Ok(result);
        }
        else // Invalid Object
        {
            return BadRequest(new UserLoginResponseDto()
            {
                Success = false,
                Errors = new List<string>(){
                        "Invalid payload"
                    }
            });
        }
    }

    private async Task<AuthResultDto> VerifyToken(TokenRequestDto tokenRequestDto)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            // We need to check the validity of the token
            var principal = tokenHandler.ValidateToken(tokenRequestDto.Token, _tokenValidationParameters, out var validatedToken);

            // We need to validate the results that have been generated for us
            // Validate if the string is an actual JWT token not a random string
            if (validatedToken is JwtSecurityToken jwtSecurityToken)
            {
                // check if the jwt token is created with the same algorithm as our jwt token
                var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);

                if (!result)
                    return new AuthResultDto()
                    {
                        Success = false,
                        Errors = new List<string>()
                            {
                                "Invalid token"
                            }
                    }; ;
            }
            // We need to check the expiry date of the token
            var utcExpiryDate = long.Parse(principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

            // convert to date to check
            var expDate = UnixTimeStampToDateTime(utcExpiryDate);

            // Checking if the JWT token has expired
            if (expDate > DateTime.UtcNow)
            {
                return new AuthResultDto()
                {
                    Success = false,
                    Errors = new List<string>()
                            {
                                "Jwt token has not expired"
                            }
                };
            }

            // Check if the refresh token exists
            var refreshTokenExist = await _unitOfWork.RefreshTokens.GetByRefreshToken(tokenRequestDto.RefreshToken);

            if (refreshTokenExist == null)
            {
                return new AuthResultDto()
                {
                    Success = false,
                    Errors = new List<string>()
                            {
                                "Invalid refresh token"
                            }
                };
            }

            // Check the expiry date of a refresh token
            if (refreshTokenExist.ExpiryDate < DateTime.UtcNow)
            {
                return new AuthResultDto()
                {
                    Success = false,
                    Errors = new List<string>()
                            {
                                "Refresh token has expired, plese login again"
                            }
                };
            }

            // Check if the refresh token has been used or not
            if (refreshTokenExist.IsUsed)
            {
                return new AuthResultDto()
                {
                    Success = false,
                    Errors = new List<string>()
                            {
                                "Refresh token has been used, it cannot be reused"
                            }
                };
            }

            // Check if the refresh token has been revoked
            if (refreshTokenExist.IsRevoked)
            {
                return new AuthResultDto()
                {
                    Success = false,
                    Errors = new List<string>()
                            {
                                "Refresh token has been revoked, it cannot be used"
                            }
                };
            }

            // check that the refresh token matches the JWT token
            var jti = principal.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

            if (refreshTokenExist.JwtId != jti)
            {
                return new AuthResultDto()
                {
                    Success = false,
                    Errors = new List<string>()
                            {
                                "Refresh token reference does not match the JWT token"
                            }
                };
            }

            // Start processing and get a new token
            refreshTokenExist.IsUsed = true;

            var updateResult = await _unitOfWork.RefreshTokens.MarkRefreshTokenAsUsed(refreshTokenExist);

            if (updateResult)
            {
                await _unitOfWork.CompleteAsync();

                // Get the user to generate a new jwt token
                var dbUser = await _userManager.FindByIdAsync(refreshTokenExist.UserId);

                if (dbUser == null)
                {
                    return new AuthResultDto()
                    {
                        Success = false,
                        Errors = new List<string>()
                                {
                                    "Error processing request"
                                }
                    };
                }

                // Generate a jwt token
                var tokens = await GenerateJwtToken(dbUser);

                return new AuthResultDto()
                {
                    Token = tokens.JwtToken,
                    Success = true,
                    RefreshToken = tokens.RefreshToken
                };
            }

            return new AuthResultDto()
            {
                Success = false,
                Errors = new List<string>()
                            {
                                "Error processing request"
                            }
            };

        }
        catch (Exception)
        {
            // TODO: Add better error handling
            // TODO: Add a logger
            return null;
        }
    }

    private DateTime UnixTimeStampToDateTime(long unixDate)
    {
        // Sets the time to 1, Jan, 1970
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        // Add the number of seconds from 1, Jan, 1970
        dateTime = dateTime.AddSeconds(unixDate).ToUniversalTime();
        return dateTime;
    }

    /// <summary>
    /// Private method used to Generate a JWT Token and return it in string format
    /// </summary>
    /// <param name="user">User to generate the JWT token for</param>
    /// <returns>new string JWT token</returns>
    private async Task<TokenDataDto> GenerateJwtToken(IdentityUser user)
    {
        // the handler is going to be responsible for creating the token
        var jwtHandler = new JwtSecurityTokenHandler();

        // Get the security key
        var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                    new Claim("Id", user.Id),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email), // unique id
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // used by the refresh token
                }),
            Expires = DateTime.UtcNow.Add(_jwtConfig.ExpiryTimeFrame), // todo update the expiration time to minutes
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature // todo review the algorithm
            )
        };

        // generate the security token
        var token = jwtHandler.CreateToken(tokenDescriptor);

        // convert the security token into a string
        var jwtToken = jwtHandler.WriteToken(token);

        // Generate a refresh token
        var refreshToken = new RefreshToken
        {
            AddedDate = DateTime.UtcNow,
            Token = $"{RandomStringGenerator(25)}_{Guid.NewGuid()}",
            UserId = user.Id,
            IsRevoked = false,
            IsUsed = false,
            Status = 1,
            JwtId = token.Id,
            ExpiryDate = DateTime.UtcNow.AddMonths(6)
        };

        await _unitOfWork.RefreshTokens.Add(refreshToken);
        await _unitOfWork.CompleteAsync();

        var tokenData = new TokenDataDto
        {
            JwtToken = jwtToken,
            RefreshToken = refreshToken.Token
        };

        return tokenData;
    }

    private string RandomStringGenerator(int length)
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
