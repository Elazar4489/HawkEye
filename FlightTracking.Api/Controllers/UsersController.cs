using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using FlightTracking.Api.Services;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> GetUserById(int id)
        {
            if (!User.CanAccessUser(id)) return Forbid();

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }
            return Ok(user);
        }

        // Admin creates a user directly; self sign-up goes through /api/auth/register.
        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (await _userService.EmailExistsAsync(createUserDto.Email))
            {
                return Conflict(new { message = "A user with this email already exists." });
            }

            var createdUser = await _userService.CreateUserAsync(createUserDto);
            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.UzerId }, createdUser);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            if (!User.CanAccessUser(id)) return Forbid();

            var updated = await _userService.UpdateUserAsync(id, updateUserDto);
            if (!updated)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }

            return NoContent();
        }

        // Stores the FCM registration token of the browser/device of this user.
        [HttpPut("{id:int}/push-token")]
        public async Task<IActionResult> UpdatePushToken(int id, [FromBody] UpdatePushTokenDto dto)
        {
            if (!User.CanAccessUser(id)) return Forbid();

            var updated = await _userService.UpdatePushTokenAsync(id, dto.PushToken);
            return updated ? NoContent() : NotFound(new { message = $"User with ID {id} not found." });
        }

        [HttpPut("{id:int}/role")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleDto dto)
        {
            var updated = await _userService.UpdateRoleAsync(id, dto.Role);
            return updated ? NoContent() : NotFound(new { message = $"User with ID {id} not found." });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var deleted = await _userService.DeleteUserAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }
            return NoContent();
        }
    }
}
