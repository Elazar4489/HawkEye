using FlightTracking.Api.Models;

namespace FlightTracking.Api.Repositories
{
    public interface ICustomerPriorityRepository
    {
        Task<IEnumerable<CustomerPriority>> GetAllAsync();
        Task<IEnumerable<CustomerPriority>> GetByUserIdAsync(int userId);
        Task<CustomerPriority?> GetByIdAsync(int id);
        Task<CustomerPriority> AddAsync(CustomerPriority customerPriority);
        void Update(CustomerPriority customerPriority); // <-- זה מה שצריך להוסיף כאן
        Task DeleteAsync(int id);
        Task<bool> SaveChangesAsync();
    }
}