// Data/Repositories/IdeaRepository.cs (Updated)
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
                .Include(i => i.Initiator)
                    .ThenInclude(u => u.Employee)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.WorkflowDefinition)
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();
        }

        public async Task<List<Idea>> GetAllForValidationAsync()
        {
            return await _context.Ideas
                .Include(i => i.Category)
                .Include(i => i.Event)
                .Include(i => i.Initiator)
                    .ThenInclude(u => u.Employee)
                        .ThenInclude(e => e.Divisi)
                .Include(i => i.Initiator)
                    .ThenInclude(u => u.Employee)
                        .ThenInclude(e => e.Departement)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.WorkflowDefinition)
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();
        }

        public async Task<List<Idea>> GetByInitiatorUserIdAsync(long initiatorUserId)
        {
            return await _context.Ideas
                .Where(i => i.InitiatorUserId == initiatorUserId)
                .Include(i => i.Category)
                .Include(i => i.Event)
                .Include(i => i.Initiator)
                    .ThenInclude(u => u.Employee)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.WorkflowDefinition)
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();
        }

        public async Task<List<Idea>> GetByInitiatorUsernameAsync(string username)
        {
            return await _context.Ideas
                .Where(i => i.Initiator.Username == username)
                .Include(i => i.Category)
                .Include(i => i.Event)
                .Include(i => i.Initiator)
                    .ThenInclude(u => u.Employee)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.WorkflowDefinition)
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();
        }

        public async Task<Idea?> GetByIdAsync(long id)
        {
            return await _context.Ideas
                .Include(i => i.Category)
                .Include(i => i.Event)
                .Include(i => i.Initiator)
                    .ThenInclude(u => u.Employee)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.WorkflowDefinition)
                .Include(i => i.ApprovalHistory)
                    .ThenInclude(ah => ah.ApproverUser)
                        .ThenInclude(u => u.Employee)
                .Include(i => i.Milestones)
                .Include(i => i.SavingMonitoring)
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

        public async Task DeleteAsync(long id)
        {
            var idea = await GetByIdAsync(id);
            if (idea != null)
            {
                _context.Ideas.Remove(idea);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Idea>> GetIdeasByStatusAsync(string status)
        {
            return await _context.Ideas
                .Where(i => i.Status == status)
                .Include(i => i.Category)
                .Include(i => i.Event)
                .Include(i => i.Initiator)
                    .ThenInclude(u => u.Employee)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.WorkflowDefinition)
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();
        }

        public async Task<List<Idea>> GetIdeasByWorkflowAndStageAsync(string workflowId, int stage)
        {
            return await _context.Ideas
                .Where(i => i.WorkflowDefinitionId == workflowId && i.CurrentStage == stage)
                .Include(i => i.Category)
                .Include(i => i.Event)
                .Include(i => i.Initiator)
                    .ThenInclude(u => u.Employee)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.WorkflowDefinition)
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();
        }
    }
}