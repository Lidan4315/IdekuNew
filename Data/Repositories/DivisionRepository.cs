using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class DivisionRepository
    {
        private readonly AppDbContext _context;

        public DivisionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Divisi>> GetAllAsync()
        {
            return await _context.Divisi
                .OrderBy(d => d.NamaDivisi)
                .ToListAsync();
        }
    }
}
