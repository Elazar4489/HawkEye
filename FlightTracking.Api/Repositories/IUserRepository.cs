using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;

namespace FlightTracking.Api.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<bool> SaveChangesAsync();
    }
}