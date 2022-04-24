using AutoMapper;
using HealthNotebook.Configuration.Messages;
using HealthNotebook.DataService.IConfiguration;
using HealthNotebook.Entities.DbSet;
using HealthNotebook.Entities.Dtos.Generic;
using HealthNotebook.Entities.Dtos.Incoming;
using HealthNotebook.Entities.Dtos.Outgoing.Profile;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthNotebook.Api.Controllers.v1;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UsersController : BaseController
{
    public UsersController(
        IUnitOfWork unitOfWork,
        UserManager<IdentityUser> userManager,
        IMapper mapper) : base(unitOfWork, userManager, mapper)
    {
    }

    // GET all users
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _unitOfWork.Users.All();
        var result = new PagedResult<User>();
        result.Content = users.ToList();
        result.ResultCount = users.Count();
        return Ok(result);
    }

    // POST
    [HttpPost]
    public async Task<IActionResult> AddUser(UserDto user)
    {
        var _mappedUser = _mapper.Map<User>(user);

        await _unitOfWork.Users.Add(_mappedUser);
        await _unitOfWork.CompleteAsync();

        // TODO: Add the correct return to this action
        var result = new Result<UserDto>();
        result.Content = user;

        return CreatedAtRoute("GetUser", new { id = _mappedUser.Id }, result); // return a 201
    }

    // GET
    [HttpGet]
    [Route("GetUser", Name = "GetUser")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _unitOfWork.Users.GetById(id);

        var result = new Result<ProfileDto>();

        if (user != null)
        {
            var mappedProfile = _mapper.Map<ProfileDto>(user);

            result.Content = mappedProfile;

            return Ok(result);
        }
        result.Error = PopulateError(
            404,
            ErrorMessages.Users.UserNotFound,
            ErrorMessages.Generic.ObjectNotFound
        );
        return BadRequest(result);
    }
}
