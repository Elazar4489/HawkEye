using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using FlightTracking.Api.Repositories;

namespace FlightTracking.Api.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(u => new UserResponseDto
            {
                UzerId = u.UzerId,
                UserName = u.UserName,
                Email = u.Email,
                Fone = u.Fone
            });
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            var u = await _userRepository.GetByIdAsync(id);
            if (u == null) return null;

            return new UserResponseDto
            {
                UzerId = u.UzerId,
                UserName = u.UserName,
                Email = u.Email,
                Fone = u.Fone
            };
        }

        public async Task<UserResponseDto> CreateUserAsync(CreateUserDto dto)
        {
            var user = new User
            {
                UserName = dto.UserName,
                Password = dto.Password,// Note: In a real project it is recommended to hash the password
                Email = dto.Email,
                Fone = dto.Fone
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return new UserResponseDto
            {
                UzerId = user.UzerId,
                UserName = user.UserName,
                Email = user.Email,
                Fone = user.Fone
            };
        }
        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.UserName = dto.UserName;
            user.Email = dto.Email;
            user.Fone = dto.Fone;

            if (!string.IsNullOrEmpty(dto.Password))
            {
                user.Password = dto.Password;
            }

            await _userRepository.UpdateAsync(user);
            return await _userRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            await _userRepository.DeleteAsync(id);
            return await _userRepository.SaveChangesAsync();
        }
    }
}