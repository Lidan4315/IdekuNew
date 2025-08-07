// Data/Repositories/EmployeeRepository.cs (Minor updates)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class EmployeeRepository
    {
        private readonly AppDbContext _context;

        public EmployeeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Employee?> GetByBadgeAsync(string badgeNumber)
        {
            if (string.IsNullOrEmpty(badgeNumber))
            {
                return null;
            }

            return await _context.Employees
                .Include(e => e.Departement!)
                    .ThenInclude(d => d.Divisi)
                .Include(e => e.Divisi)
                .Include(e => e.User) // Include User relationship
                    .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(e => e.Id == badgeNumber);
        }

        public async Task<List<Employee>> GetAllActiveAsync()
        {
            return await _context.Employees
                .Where(e => e.EmploymentStatus == "Active")
                .Include(e => e.Departement!)
                    .ThenInclude(d => d.Divisi)
                .Include(e => e.Divisi)
                .Include(e => e.User)
                    .ThenInclude(u => u.Role)
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        public async Task<List<Employee>> GetByDivisionAsync(string divisionId)
        {
            return await _context.Employees
                .Where(e => e.DivisiId == divisionId && e.EmploymentStatus == "Active")
                .Include(e => e.Departement)
                .Include(e => e.User)
                    .ThenInclude(u => u.Role)
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        public async Task<List<Employee>> GetByDepartmentAsync(string departmentId)
        {
            return await _context.Employees
                .Where(e => e.DepartementId == departmentId && e.EmploymentStatus == "Active")
                .Include(e => e.Divisi)
                .Include(e => e.User)
                    .ThenInclude(u => u.Role)
                .OrderBy(e => e.Name)
                .ToListAsync();
        }
    }
}