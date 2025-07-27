using Ideku.Data.Repositories;
using Ideku.Models.Entities;

namespace Ideku.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;

        public AuthService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User?> AuthenticateAsync(string username)
        {
            return await _userRepository.GetByUsernameAsync(username);
        }

        public async Task<bool> ValidateUserAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            return user != null;
        }

        public async Task<Employee?> GetEmployeeByBadgeAsync(string badgeNumber)
        {
            return await _userRepository.GetEmployeeByBadgeAsync(badgeNumber);
        }

        public async Task<string?> GetCurrentUserBadgeNumberAsync(System.Security.Claims.ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var username = user.Identity.Name;
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            var appUser = await _userRepository.GetByUsernameAsync(username);
            return appUser?.EmployeeId;
        }
    }
}
