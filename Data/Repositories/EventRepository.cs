// Data/Repositories/EventRepository.cs (No major changes)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class EventRepository
    {
        private readonly AppDbContext _context;

        public EventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Event>> GetAllAsync()
        {
            return await _context.Event
                .Where(e => e.IsActive)
                .OrderBy(e => e.NamaEvent)
                .ToListAsync();
        }

        public async Task<Event?> GetByIdAsync(int id)
        {
            return await _context.Event.FindAsync(id);
        }
    }
}