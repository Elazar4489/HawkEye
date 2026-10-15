using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlightTracking.Api.DTOs;
using FlightTracking.Api.Services;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/users/{userId:int}/priorities")]
    [Authorize]
    public class CustomerPrioritiesController : ControllerBase
    {
        private readonly ICustomerPriorityService _service;

        public CustomerPrioritiesController(ICustomerPriorityService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerPriorityResponseDto>>> GetUserPriorities(int userId)
        {
            if (!User.CanAccessUser(userId)) return Forbid();

            var priorities = await _service.GetUserPrioritiesAsync(userId);
            return Ok(priorities);
        }

        [HttpPost]
        public async Task<ActionResult<CustomerPriorityResponseDto>> CreatePriority(int userId, [FromBody] CreateCustomerPriorityDto dto)
        {
            if (!User.CanAccessUser(userId)) return Forbid();

            // The route is the source of truth for the owner.
            if (dto.UserId == 0) dto.UserId = userId;
            if (userId != dto.UserId)
            {
                return BadRequest(new { message = "UserId in route does not match body." });
            }

            var error = CustomerPriorityService.Validate(
                dto.AreaType, dto.CenterLongitude, dto.CenterLatitude, dto.RadiusMeters, dto.PolygonCoordinates);
            if (error != null)
            {
                return BadRequest(new { message = error });
            }

            var created = await _service.CreatePriorityAsync(dto);
            return CreatedAtAction(nameof(GetUserPriorities), new { userId = created.UserId }, created);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePriority(int userId, int id, [FromBody] UpdateCustomerPriorityDto dto)
        {
            if (!User.CanAccessUser(userId)) return Forbid();

            var existing = await _service.GetPriorityAsync(id);
            if (existing == null || existing.UserId != userId)
            {
                return NotFound(new { message = $"Priority area with ID {id} not found." });
            }

            var error = CustomerPriorityService.Validate(
                dto.AreaType, dto.CenterLongitude, dto.CenterLatitude, dto.RadiusMeters, dto.PolygonCoordinates);
            if (error != null)
            {
                return BadRequest(new { message = error });
            }

            await _service.UpdatePriorityAsync(id, dto);
            return NoContent(); // מחזיר 204 הצלחה
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePriority(int userId, int id)
        {
            if (!User.CanAccessUser(userId)) return Forbid();

            var existing = await _service.GetPriorityAsync(id);
            if (existing == null || existing.UserId != userId)
            {
                return NotFound(new { message = $"Priority area with ID {id} not found." });
            }

            await _service.DeletePriorityAsync(id);
            return NoContent();
        }
    }
}
