// Services/MilestoneService.cs (NEW - Basic implementation)
using Ideku.Data.Repositories;
using Ideku.Models.Entities;

namespace Ideku.Services
{
    public class MilestoneService
    {
        private readonly MilestoneRepository _milestoneRepository;
        private readonly IdeaRepository _ideaRepository;
        private readonly AuthService _authService;
        private readonly ILogger<MilestoneService> _logger;

        public MilestoneService(
            MilestoneRepository milestoneRepository,
            IdeaRepository ideaRepository,
            AuthService authService,
            ILogger<MilestoneService> logger)
        {
            _milestoneRepository = milestoneRepository;
            _ideaRepository = ideaRepository;
            _authService = authService;
            _logger = logger;
        }

        public async Task<List<IdeaMilestone>> GetMilestonesByIdeaIdAsync(long ideaId)
        {
            return await _milestoneRepository.GetByIdeaIdAsync(ideaId);
        }

        public async Task<IdeaMilestone?> GetMilestoneByIdAsync(long id)
        {
            return await _milestoneRepository.GetByIdAsync(id);
        }

        public async Task<IdeaMilestone> CreateMilestoneAsync(IdeaMilestone milestone)
        {
            return await _milestoneRepository.CreateAsync(milestone);
        }

        public async Task UpdateMilestoneAsync(IdeaMilestone milestone)
        {
            await _milestoneRepository.UpdateAsync(milestone);
        }

        public async Task DeleteMilestoneAsync(long id)
        {
            await _milestoneRepository.DeleteAsync(id);
        }

        public async Task<List<IdeaMilestone>> GetMilestonesByAssigneeAsync(long assigneeUserId)
        {
            return await _milestoneRepository.GetByAssigneeAsync(assigneeUserId);
        }
    }
}