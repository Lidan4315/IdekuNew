// Data/Repositories/MilestoneRepository.cs (NEW)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class MilestoneRepository
    {
        private readonly AppDbContext _context;

        public MilestoneRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<IdeaMilestone>> GetByIdeaIdAsync(long ideaId)
        {
            return await _context.IdeaMilestones
                .Where(m => m.IdeaId == ideaId)
                .Include(m => m.CreatedByUser)
                    .ThenInclude(u => u.Employee)
                .Include(m => m.AssignedToUser)
                    .ThenInclude(u => u.Employee)
                .Include(m => m.Idea)
                .OrderBy(m => m.Stage)
                .ThenBy(m => m.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<IdeaMilestone>> GetByAssigneeAsync(long assigneeUserId)
        {
            return await _context.IdeaMilestones
                .Where(m => m.AssignedToUserId == assigneeUserId)
                .Include(m => m.Idea)
                    .ThenInclude(i => i.Initiator)
                        .ThenInclude(u => u.Employee)
                .Include(m => m.CreatedByUser)
                    .ThenInclude(u => u.Employee)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();
        }

        public async Task<IdeaMilestone?> GetByIdAsync(long id)
        {
            return await _context.IdeaMilestones
                .Include(m => m.Idea)
                .Include(m => m.CreatedByUser)
                    .ThenInclude(u => u.Employee)
                .Include(m => m.AssignedToUser)
                    .ThenInclude(u => u.Employee)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IdeaMilestone> CreateAsync(IdeaMilestone milestone)
        {
            _context.IdeaMilestones.Add(milestone);
            await _context.SaveChangesAsync();
            return milestone;
        }

        public async Task UpdateAsync(IdeaMilestone milestone)
        {
            _context.IdeaMilestones.Update(milestone);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var milestone = await GetByIdAsync(id);
            if (milestone != null)
            {
                _context.IdeaMilestones.Remove(milestone);
                await _context.SaveChangesAsync();
            }
        }
    }
}