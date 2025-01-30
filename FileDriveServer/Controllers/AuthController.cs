using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;

        public AuthController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("signup")]
        public IActionResult SignUp([FromBody] User user)
        {
            if (_userService.AddUser(user.Username, user.Password))
            {
                return Ok(new { Message = "Signup successful." });
            }
            return BadRequest(new { Message = "Signup failed. Username may already exist." });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] User user)
        {
            if (_userService.Authenticate(user.Username, user.Password))
            {
                return Ok(new { Message = "Login successful." });
            }
            return Unauthorized(new { Message = "Invalid username or password." });
        }
    }
}
