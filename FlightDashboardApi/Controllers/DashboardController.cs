using FlightDashboardApi.Models;
using FlightDashboardApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightDashboardApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly FlightDashboardService _dashboardService;

        public DashboardController(FlightDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateDashboard([FromBody] DashboardRequestDto request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request data.");
            }

            var dashboard = await _dashboardService.GenerateAndSaveDashboardAsync(request);

            return Ok(new
            {
                Message = "Dashboard generated and saved to Elasticsearch successfully",
                DashboardId = dashboard.Id,
                Data = dashboard
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDashboard(string id)
        {
            var dashboard = await _dashboardService.GetDashboardByIdAsync(id);

            if (dashboard == null)
            {
                return NotFound($"Dashboard with ID {id} was not found.");
            }

            return Ok(dashboard);
        }
    }
}