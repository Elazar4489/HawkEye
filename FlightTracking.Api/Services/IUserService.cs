using FlightTracking.Api.DTOs;

namespace FlightTracking.Api.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(int id);
        Task<UserResponseDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<bool> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(int id);

        Task<bool> EmailExistsAsync(string email);
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> RegisterAsync(CreateUserDto dto);
        Task<bool> UpdatePushTokenAsync(int id, string? pushToken);
        Task<bool> UpdateRoleAsync(int id, string role);
        Task EnsureAdminAsync(string email, string password);
    }
}
