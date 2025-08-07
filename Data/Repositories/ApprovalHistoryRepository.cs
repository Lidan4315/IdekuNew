// Data/Repositories/ApprovalHistoryRepository.cs (NEW)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class ApprovalHistoryRepository
    {
        private readonly AppDbContext _context;

        public ApprovalHistoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ApprovalHistory>> GetByIdeaIdAsync(long ideaId)
        {
            return await _context.ApprovalHistory
                .Where(ah => ah.IdeaId == ideaId)
                .Include(ah => ah.ApproverUser)
                    .ThenInclude(u => u.Employee)
                .Include(ah => ah.Idea)
                .OrderBy(ah => ah.ActionDate)
                .ToListAsync();
        }

        public async Task<List<ApprovalHistory>> GetByApproverAsync(long approverUserId)
        {
            return await _context.ApprovalHistory
                .Where(ah => ah.ApproverUserId == approverUserId)
                .Include(ah => ah.Idea)
                    .ThenInclude(i => i.Initiator)
                        .ThenInclude(u => u.Employee)
                .Include(ah => ah.ApproverUser)
                    .ThenInclude(u => u.Employee)
                .OrderByDescending(ah => ah.ActionDate)
                .ToListAsync();
        }

        public async Task<ApprovalHistory?> GetLatestForIdeaAsync(long ideaId)
        {
            return await _context.ApprovalHistory
                .Where(ah => ah.IdeaId == ideaId)
                .Include(ah => ah.ApproverUser)
                    .ThenInclude(u => u.Employee)
                .OrderByDescending(ah => ah.ActionDate)
                .FirstOrDefaultAsync();
        }

        public async Task<ApprovalHistory> CreateAsync(ApprovalHistory approvalHistory)
        {
            _context.ApprovalHistory.Add(approvalHistory);
            await _context.SaveChangesAsync();
            return approvalHistory;
        }

        public async Task<List<ApprovalHistory>> GetByIdeaAndStageAsync(long ideaId, int stage)
        {
            return await _context.ApprovalHistory
                .Where(ah => ah.IdeaId == ideaId && ah.Stage == stage)
                .Include(ah => ah.ApproverUser)
                    .ThenInclude(u => u.Employee)
                .OrderBy(ah => ah.ActionDate)
                .ToListAsync();
        }
    }
}