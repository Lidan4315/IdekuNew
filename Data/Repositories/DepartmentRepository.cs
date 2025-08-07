// Data/Repositories/DepartmentRepository.cs (No major changes)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class DepartmentRepository
    {
        private readonly AppDbContext _context;

        public DepartmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Departement>> GetByDivisionIdAsync(string divisionId)
        {
            if (string.IsNullOrEmpty(divisionId))
            {
                return new List<Departement>();
            }

            return await _context.Departement
                .Where(d => d.DivisiId == divisionId)
                .Include(d => d.Divisi)
                .OrderBy(d => d.NamaDepartement)
                .ToListAsync();
        }

        public async Task<Departement?> GetByIdAsync(string id)
        {
            return await _context.Departement
                .Include(d => d.Divisi)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<Departement>> GetAllAsync()
        {
            return await _context.Departement
                .Include(d => d.Divisi)
                .OrderBy(d => d.Divisi.NamaDivisi)
                .ThenBy(d => d.NamaDepartement)
                .ToListAsync();
        }
    }
}