// Data/Repositories/SavingMonitoringRepository.cs (NEW)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class SavingMonitoringRepository
    {
        private readonly AppDbContext _context;

        public SavingMonitoringRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SavingMonitoring>> GetByIdeaIdAsync(long ideaId)
        {
            return await _context.SavingMonitoring
                .Where(sm => sm.IdeaId == ideaId)
                .Include(sm => sm.ReportedByUser)
                    .ThenInclude(u => u.Employee)
                .Include(sm => sm.ReviewedByUser)
                    .ThenInclude(u => u.Employee)
                .Include(sm => sm.Idea)
                .OrderByDescending(sm => sm.PeriodStartDate)
                .ToListAsync();
        }

        public async Task<SavingMonitoring?> GetByIdAsync(long id)
        {
            return await _context.SavingMonitoring
                .Include(sm => sm.Idea)
                .Include(sm => sm.ReportedByUser)
                    .ThenInclude(u => u.Employee)
                .Include(sm => sm.ReviewedByUser)
                    .ThenInclude(u => u.Employee)
                .FirstOrDefaultAsync(sm => sm.Id == id);
        }

        public async Task<SavingMonitoring?> GetLatestByIdeaIdAsync(long ideaId)
        {
            return await _context.SavingMonitoring
                .Where(sm => sm.IdeaId == ideaId)
                .Include(sm => sm.ReportedByUser)
                    .ThenInclude(u => u.Employee)
                .Include(sm => sm.ReviewedByUser)
                    .ThenInclude(u => u.Employee)
                .OrderByDescending(sm => sm.CreatedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<SavingMonitoring>> GetByReporterAsync(long reporterUserId)
        {
            return await _context.SavingMonitoring
                .Where(sm => sm.ReportedByUserId == reporterUserId)
                .Include(sm => sm.Idea)
                    .ThenInclude(i => i.Initiator)
                        .ThenInclude(u => u.Employee)
                .Include(sm => sm.ReviewedByUser)
                    .ThenInclude(u => u.Employee)
                .OrderByDescending(sm => sm.CreatedDate)
                .ToListAsync();
        }

        public async Task<SavingMonitoring> CreateAsync(SavingMonitoring savingMonitoring)
        {
            _context.SavingMonitoring.Add(savingMonitoring);
            await _context.SaveChangesAsync();
            return savingMonitoring;
        }

        public async Task UpdateAsync(SavingMonitoring savingMonitoring)
        {
            _context.SavingMonitoring.Update(savingMonitoring);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var monitoring = await GetByIdAsync(id);
            if (monitoring != null)
            {
                _context.SavingMonitoring.Remove(monitoring);
                await _context.SaveChangesAsync();
            }
        }
    }
}