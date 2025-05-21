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
        private readonly JwtService _jwtService;

        public AuthController(UserService userService, JwtService jwtService)
        {
            _userService = userService;
            _jwtService = jwtService;
        }

        [HttpPost("signup")]
        public IActionResult SignUp([FromBody] User user)
        {
            Console.WriteLine("User tried to sign up with values:" + user.ToString());
            try
            {
                if (user.Username == "" || user.Password == "") { return BadRequest(new { Message = "Please provide all fields." }); }
                if (_userService.AddUser(user.Username, user.Password))
                {
                    return Ok(new { Message = "Signup successful." });
                }
                return BadRequest(new { Message = "Signup failed. Username may already exist." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
          
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] User user)
        {
            Console.WriteLine("User attempted login");
            try
            {
                if (_userService.Authenticate(user.Username, user.Password))
                {
                    string token = _jwtService.GenerateToken(user.Username);
                    return Ok(new { Message = "Login successful.", Token = token });
                }
                return Unauthorized(new { Message = "Invalid username or password." });
            }
            catch (Exception ex)
            {
                return BadRequest("Something failed in the authentication process");
            }

          
        }
    }
}
