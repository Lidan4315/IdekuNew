using Ideku.Data.Repositories;
using Ideku.Models.Entities;

namespace Ideku.Services
{
    public class IdeaService
    {
        private readonly IdeaRepository _ideaRepository;

        public IdeaService(IdeaRepository ideaRepository)
        {
            _ideaRepository = ideaRepository;
        }

        public async Task<List<Idea>> GetAllIdeasAsync()
        {
            return await _ideaRepository.GetAllAsync();
        }

        public async Task<List<Idea>> GetUserIdeasAsync(string initiator)
        {
            return await _ideaRepository.GetByInitiatorAsync(initiator);
        }

        public async Task<Idea?> GetIdeaByIdAsync(int id)
        {
            return await _ideaRepository.GetByIdAsync(id);
        }

        public async Task<Idea> CreateIdeaAsync(Idea idea)
        {
            // Set default values
            idea.SubmittedDate = DateTime.UtcNow;
            idea.CurrentStage = 0; // Set initial stage to S0
            idea.CurrentStatus = "Submitted";

            return await _ideaRepository.CreateAsync(idea);
        }

        public async Task UpdateIdeaAsync(Idea idea)
        {
            idea.UpdatedDate = DateTime.UtcNow;
            await _ideaRepository.UpdateAsync(idea);
        }

        public async Task DeleteIdeaAsync(int id)
        {
            await _ideaRepository.DeleteAsync(id);
        }

        public async Task<(int total, int pending, int approved)> GetIdeaStatsAsync(string initiator)
        {
            var ideas = await GetUserIdeasAsync(initiator);
            var total = ideas.Count;
            var pending = ideas.Count(i => i.CurrentStatus == "Submitted" || i.CurrentStatus == "Under Review");
            var approved = ideas.Count(i => i.CurrentStatus == "Approved");

            return (total, pending, approved);
        }
    }
}
