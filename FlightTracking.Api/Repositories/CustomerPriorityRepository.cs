using FlightTracking.Api.Models;
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;

namespace FlightTracking.Api.Repositories
{
    public class CustomerPriorityRepository : ICustomerPriorityRepository
    {
        private readonly AppDbContext _context;

        public CustomerPriorityRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CustomerPriority>> GetAllAsync()
        {
            return await _context.CustomerPriorities.ToListAsync();
        }

        public async Task<IEnumerable<CustomerPriority>> GetByUserIdAsync(int userId)
        {
            return await _context.CustomerPriorities
                .Where(cp => cp.UserId == userId)
                .ToListAsync();
        }

        public async Task<CustomerPriority?> GetByIdAsync(int id)
        {
            return await _context.CustomerPriorities.FindAsync(id);
        }

        public async Task<CustomerPriority> AddAsync(CustomerPriority customerPriority)
        {
            await _context.CustomerPriorities.AddAsync(customerPriority);
            return customerPriority;
        }

        // <-- הוסף את מתודת העדכון לכאן
        public void Update(CustomerPriority customerPriority)
        {
            _context.CustomerPriorities.Update(customerPriority);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.CustomerPriorities.Remove(entity);
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}