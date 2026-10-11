using Microsoft.AspNetCore.Mvc;
using FlightTracking.Api.DTOs;
using FlightTracking.Api.Services;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/users/{userId:int}/priorities")]
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
            var priorities = await _service.GetUserPrioritiesAsync(userId);
            return Ok(priorities);
        }

        [HttpPost]
        public async Task<ActionResult<CustomerPriorityResponseDto>> CreatePriority(int userId, [FromBody] CreateCustomerPriorityDto dto)
        {
            if (userId != dto.UserId)
            {
                return BadRequest(new { message = "UserId in route does not match body." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _service.CreatePriorityAsync(dto);
            return CreatedAtAction(nameof(GetUserPriorities), new { userId = created.UserId }, created);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePriority(int userId, int id, [FromBody] UpdateCustomerPriorityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _service.UpdatePriorityAsync(id, dto);
            if (!updated)
            {
                return NotFound(new { message = $"Priority area with ID {id} not found." });
            }

            return NoContent(); // מחזיר 204 הצלחה
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePriority(int id)
        {
            var deleted = await _service.DeletePriorityAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Priority area with ID {id} not found." });
            }

            return NoContent();
        }
    }
}