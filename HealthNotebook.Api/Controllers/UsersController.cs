using Microsoft.AspNetCore.Mvc;

namespace HealthNotebook.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        [Route("TestRun")]
        [HttpGet]
        public IActionResult TestRun()
        {
            return Ok("Success");
        }
    }
}