using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
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

namespace HealthNotebook.Api.Controllers.v1
{
    public class AccountsController : BaseController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly JwtConfig _jwtConfig;

        public AccountsController(
            IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            TokenValidationParameters tokenValidationParameters,
            IOptionsMonitor<JwtConfig> optionMonitor) : base(unitOfWork)
        {
            _userManager = userManager;
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
            throw new NotImplementedException();
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
}