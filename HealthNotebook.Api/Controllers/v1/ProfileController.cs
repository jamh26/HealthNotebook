using AutoMapper;
using HealthNotebook.Configuration.Messages;
using HealthNotebook.DataService.IConfiguration;
using HealthNotebook.Entities.DbSet;
using HealthNotebook.Entities.Dtos.Generic;
using HealthNotebook.Entities.Dtos.Incoming.Profile;
using HealthNotebook.Entities.Dtos.Outgoing.Profile;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthNotebook.Api.Controllers.v1;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProfileController : BaseController
{
    public ProfileController(
        IUnitOfWork unitOfWork,
        UserManager<IdentityUser> userManager,
        IMapper mapper) : base(unitOfWork, userManager, mapper)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var loggedInUser = await _userManager.GetUserAsync(HttpContext.User);

        var result = new Result<ProfileDto>();
        if (loggedInUser == null)
        {

            result.Error = PopulateError(
                400,
                ErrorMessages.Profile.UserNotFound,
                ErrorMessages.Generic.BadRequest
            );
            return BadRequest(result);
        }

        var identityId = new Guid(loggedInUser.Id);

        var profile = await _unitOfWork.Users.GetByIdentityId(identityId);

        if (profile == null)
        {
            result.Error = PopulateError(
                400,
                ErrorMessages.Profile.UserNotFound,
                ErrorMessages.Generic.BadRequest
            );
            return BadRequest(result);
        }

        var mappedProfile = _mapper.Map<ProfileDto>(profile);

        result.Content = mappedProfile;
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto profileToUpdate)
    {
        var result = new Result<ProfileDto>();
        if (!ModelState.IsValid)
        {
            result.Error = PopulateError(
                400,
                ErrorMessages.Generic.InvalidPayload,
                ErrorMessages.Generic.BadRequest
            );
            return BadRequest(result);
        }

        var loggedInUser = await _userManager.GetUserAsync(HttpContext.User);

        if (loggedInUser == null)
        {
            result.Error = PopulateError(
                400,
                ErrorMessages.Profile.UserNotFound,
                ErrorMessages.Generic.BadRequest
            );
            return BadRequest(result);
        }

        var identityId = new Guid(loggedInUser.Id);

        var userProfile = await _unitOfWork.Users.GetByIdentityId(identityId);

        if (userProfile == null)
        {
            result.Error = PopulateError(
                400,
                ErrorMessages.Profile.UserNotFound,
                ErrorMessages.Generic.BadRequest
            );
            return BadRequest(result);
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

            var mappedProfile = _mapper.Map<ProfileDto>(userProfile);

            result.Content = mappedProfile;

            return Ok(result);
        }

        result.Error = PopulateError(
                    500,
                    ErrorMessages.Generic.SomethingWentWrong,
                    ErrorMessages.Generic.UnableToProcess
        );

        return BadRequest(result);
    }
}
