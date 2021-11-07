using System;
using System.Threading.Tasks;
using HealthNotebook.DataService.IConfiguration;
using HealthNotebook.Entities.Dtos.Incoming.Profile;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthNotebook.Api.Controllers.v1
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProfileController : BaseController
    {
        public ProfileController(
            IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager) : base(unitOfWork, userManager)
        {
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var loggedInUser = await _userManager.GetUserAsync(HttpContext.User);

            if (loggedInUser == null)
            {
                return BadRequest("User not found");
            }

            var identityId = new Guid(loggedInUser.Id);

            var profile = await _unitOfWork.Users.GetByIdentityId(identityId);

            if (profile == null)
            {
                return BadRequest("User not found");
            }

            return Ok(profile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto profileToUpdate)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid payload");
            }

            var loggedInUser = await _userManager.GetUserAsync(HttpContext.User);

            if (loggedInUser == null)
            {
                return BadRequest("User not found");
            }

            var identityId = new Guid(loggedInUser.Id);

            var userProfile = await _unitOfWork.Users.GetByIdentityId(identityId);

            if (userProfile == null)
            {
                return BadRequest("User not found");
            }

            userProfile.Address = profileToUpdate.Address;
            userProfile.Gender = profileToUpdate.Gender;
            userProfile.MobileNumber = profileToUpdate.MobileNumber;
            userProfile.Country = profileToUpdate.Country;
            userProfile.UpdateDate = DateTime.UtcNow;

            var isUpdated = await _unitOfWork.Users.UpdateUserProfile(userProfile);

            if (isUpdated)
            {
                await _unitOfWork.CompleteAsync();
                return Ok(userProfile);
            }

            return BadRequest("Something went wrong, please try again later");
        }
    }
}