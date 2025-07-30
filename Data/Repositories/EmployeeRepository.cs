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
                .FirstOrDefaultAsync(e => e.Id == badgeNumber);
        }
    }
}
