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
                .FirstOrDefaultAsync(u => u.Username == username);
        }

    }
}
