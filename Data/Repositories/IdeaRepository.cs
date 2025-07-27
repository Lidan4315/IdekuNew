using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class IdeaRepository
    {
        private readonly AppDbContext _context;

        public IdeaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Idea>> GetAllAsync()
        {
            return await _context.Ideas
                .Include(i => i.Category)
                .Include(i => i.Event)
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();
        }

        public async Task<List<Idea>> GetByInitiatorAsync(string initiator)
        {
            return await _context.Ideas
                .Where(i => i.InitiatorId == initiator)
                .Include(i => i.Category)
                .Include(i => i.Event)
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();
        }

        public async Task<Idea?> GetByIdAsync(int id)
        {
            return await _context.Ideas
                .Include(i => i.Category)
                .Include(i => i.Event)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Idea> CreateAsync(Idea idea)
        {
            _context.Ideas.Add(idea);
            await _context.SaveChangesAsync();
            return idea;
        }

        public async Task UpdateAsync(Idea idea)
        {
            _context.Ideas.Update(idea);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var idea = await GetByIdAsync(id);
            if (idea != null)
            {
                _context.Ideas.Remove(idea);
                await _context.SaveChangesAsync();
            }
        }
    }
}
