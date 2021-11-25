using System;
using System.Linq;
using System.Threading.Tasks;
using HealthNotebook.DataService.Data;
using HealthNotebook.DataService.IConfiguration;
using HealthNotebook.Entities.DbSet;
using HealthNotebook.Entities.Dtos.Incoming;
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
        UserManager<IdentityUser> userManager) : base(unitOfWork, userManager)
    {
    }

    // GET all users
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _unitOfWork.Users.All();

        return Ok(users);
    }

    // POST
    [HttpPost]
    public async Task<IActionResult> AddUser(UserDto user)
    {
        var _user = new User();
        _user.LastName = user.LastName;
        _user.FirstName = user.FirstName;
        _user.Email = user.Email;
        _user.DateOfBirth = Convert.ToDateTime(user.DateOfBirth);
        _user.Country = user.Country;
        _user.Phone = user.Phone;
        _user.Status = 1;

        await _unitOfWork.Users.Add(_user);
        await _unitOfWork.CompleteAsync();

        return CreatedAtRoute("GetUser", new { id = _user.Id }, user); // return a 201
    }

    // GET
    [HttpGet]
    [Route("GetUser", Name = "GetUser")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _unitOfWork.Users.GetById(id);

        return Ok(user);
    }
}
