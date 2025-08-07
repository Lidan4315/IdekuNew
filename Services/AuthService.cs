// Services/AuthService.cs (Updated for new schema)
using Ideku.Data.Repositories;
using Ideku.Models.Entities;

namespace Ideku.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;
        private readonly EmployeeRepository _employeeRepository;

        public AuthService(UserRepository userRepository, EmployeeRepository employeeRepository)
        {
            _userRepository = userRepository;
            _employeeRepository = employeeRepository;
        }

        public async Task<User?> AuthenticateAsync(string username)
        {
            return await _userRepository.GetByUsernameAsync(username);
        }

        public async Task<bool> ValidateUserAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            return user != null && user.IsActive;
        }

        public async Task<Employee?> GetEmployeeByBadgeAsync(string badgeNumber)
        {
            return await _employeeRepository.GetByBadgeAsync(badgeNumber);
        }

        public async Task<User?> GetUserByEmployeeIdAsync(string employeeId)
        {
            return await _userRepository.GetByEmployeeIdAsync(employeeId);
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

        public async Task<User?> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal user)
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

            return await _userRepository.GetByUsernameAsync(username);
        }

        public async Task<long?> GetCurrentUserIdAsync(System.Security.Claims.ClaimsPrincipal user)
        {
            var currentUser = await GetCurrentUserAsync(user);
            return currentUser?.Id;
        }

        public async Task<bool> UserHasPermissionAsync(string username, string permissionName)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user?.Role?.RoleFeaturePermissions == null) return false;

            return user.Role.RoleFeaturePermissions
                .Any(rfp => rfp.Permission.PermissionName == permissionName && rfp.Permission.IsActive);
        }

        public async Task<List<string>> GetUserPermissionsAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user?.Role?.RoleFeaturePermissions == null) return new List<string>();

            return user.Role.RoleFeaturePermissions
                .Where(rfp => rfp.Permission.IsActive)
                .Select(rfp => rfp.Permission.PermissionName)
                .ToList();
        }
    }
}