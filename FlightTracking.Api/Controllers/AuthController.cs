using FlightTracking.Api.DTOs;
using FlightTracking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] CreateUserDto dto)
        {
            if (await _userService.EmailExistsAsync(dto.Email))
            {
                return Conflict(new { message = "A user with this email already exists." });
            }

            var response = await _userService.RegisterAsync(dto);
            return Ok(response);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            var response = await _userService.LoginAsync(dto);
            if (response == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            return Ok(response);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserResponseDto>> Me()
        {
            var userId = User.GetUserId();
            var user = userId == null ? null : await _userService.GetUserByIdAsync(userId.Value);
            return user == null ? Unauthorized() : Ok(user);
        }
    }
}
