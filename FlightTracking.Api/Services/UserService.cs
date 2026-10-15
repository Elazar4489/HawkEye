using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using FlightTracking.Api.Repositories;
using Microsoft.AspNetCore.Identity;

namespace FlightTracking.Api.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(
            IUserRepository userRepository,
            ITokenService tokenService,
            IPasswordHasher<User> passwordHasher)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToDto);
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            var u = await _userRepository.GetByIdAsync(id);
            return u == null ? null : MapToDto(u);
        }

        public async Task<UserResponseDto> CreateUserAsync(CreateUserDto dto)
        {
            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email.Trim(),
                Fone = dto.Fone,
                Role = UserRoles.Operator
            };
            user.Password = _passwordHasher.HashPassword(user, dto.Password);

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return MapToDto(user);
        }
        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.UserName = dto.UserName;
            user.Email = dto.Email.Trim();
            user.Fone = dto.Fone;

            if (!string.IsNullOrEmpty(dto.Password))
            {
                user.Password = _passwordHasher.HashPassword(user, dto.Password);
            }

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            await _userRepository.DeleteAsync(id);
            return await _userRepository.SaveChangesAsync();
        }

        public async Task<bool> EmailExistsAsync(string email) =>
            await _userRepository.GetByEmailAsync(email.Trim()) != null;

        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email.Trim());
            if (user == null || !await VerifyPasswordAsync(user, dto.Password))
                return null;

            return CreateAuthResponse(user);
        }

        public async Task<AuthResponseDto> RegisterAsync(CreateUserDto dto)
        {
            var created = await CreateUserAsync(dto);
            var user = (await _userRepository.GetByIdAsync(created.UzerId))!;
            return CreateAuthResponse(user);
        }

        public async Task<bool> UpdatePushTokenAsync(int id, string? pushToken)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.PushToken = string.IsNullOrWhiteSpace(pushToken) ? null : pushToken.Trim();
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateRoleAsync(int id, string role)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.Role = role;
            await _userRepository.SaveChangesAsync();
            return true;
        }

        // Makes sure the configured admin account exists (first login / demo).
        public async Task EnsureAdminAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return;

            var user = await _userRepository.GetByEmailAsync(email.Trim());
            if (user == null)
            {
                user = new User
                {
                    UserName = "admin",
                    Email = email.Trim(),
                    Role = UserRoles.Admin
                };
                user.Password = _passwordHasher.HashPassword(user, password);
                await _userRepository.AddAsync(user);
            }
            else if (user.Role != UserRoles.Admin)
            {
                user.Role = UserRoles.Admin;
            }

            await _userRepository.SaveChangesAsync();
        }

        private async Task<bool> VerifyPasswordAsync(User user, string password)
        {
            PasswordVerificationResult result;
            try
            {
                result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
            }
            catch (FormatException)
            {
                // Users created before hashing was added still hold a plain-text password:
                // accept it once and upgrade it to a hash.
                result = user.Password == password
                    ? PasswordVerificationResult.SuccessRehashNeeded
                    : PasswordVerificationResult.Failed;
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.Password = _passwordHasher.HashPassword(user, password);
                await _userRepository.SaveChangesAsync();
            }

            return result != PasswordVerificationResult.Failed;
        }

        private AuthResponseDto CreateAuthResponse(User user)
        {
            var (token, expires) = _tokenService.CreateToken(user);
            return new AuthResponseDto
            {
                User = MapToDto(user),
                Token = token,
                ExpiresAtUtc = expires
            };
        }

        private static UserResponseDto MapToDto(User u) => new()
        {
            UzerId = u.UzerId,
            UserName = u.UserName,
            Email = u.Email,
            Fone = u.Fone,
            Role = u.Role,
            HasPushToken = !string.IsNullOrEmpty(u.PushToken)
        };
    }
}
