using FlightTracking.Api.DTOs;

namespace FlightTracking.Api.Services
{
    public interface ICustomerPriorityService
    {
        Task<IEnumerable<CustomerPriorityResponseDto>> GetUserPrioritiesAsync(int userId);
        Task<CustomerPriorityResponseDto> CreatePriorityAsync(CreateCustomerPriorityDto dto);
        Task<bool> UpdatePriorityAsync(int id, UpdateCustomerPriorityDto dto);
        Task<bool> DeletePriorityAsync(int id);
    }
}