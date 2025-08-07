// Data/Repositories/UserRepository.cs (Updated)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class UserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Departement)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Divisi)
                .Include(u => u.Role)
                    .ThenInclude(r => r.RoleFeaturePermissions)
                        .ThenInclude(rfp => rfp.Permission)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            return await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Departement)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Divisi)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmployeeIdAsync(string employeeId)
        {
            return await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Departement)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Divisi)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        }

        public async Task<List<User>> GetUsersByRoleAsync(string roleId)
        {
            return await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Departement)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Divisi)
                .Include(u => u.Role)
                .Where(u => u.RoleId == roleId && u.IsActive)
                .OrderBy(u => u.Employee.Name)
                .ToListAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
    }
}