// Helpers/IdeaDisplayHelper.cs (NEW - moved from Services)
using Ideku.Models.Entities;
using Ideku.Models.ViewModels.Idea;
using Ideku.Services;

namespace Ideku.Helpers
{
    public class IdeaDisplayHelper
    {
        private readonly IdeaCodeService _ideaCodeService;

        public IdeaDisplayHelper(IdeaCodeService ideaCodeService)
        {
            _ideaCodeService = ideaCodeService;
        }

        /// <summary>
        /// Convert Idea entity to display view model
        /// </summary>
        public IdeaDisplayViewModel ToDisplayViewModel(Idea idea)
        {
            return new IdeaDisplayViewModel
            {
                Id = idea.Id,
                DisplayId = idea.DisplayId,
                IdeaName = idea.IdeaName,
                Status = idea.Status,
                InitiatorName = idea.InitiatorName,
                SavingCost = idea.SavingCost,
                ValidatedSavingCost = idea.ValidatedSavingCost,
                CurrentStage = idea.CurrentStage,
                SubmittedDate = idea.SubmittedDate,
                CategoryName = idea.CategoryName,
                DivisionName = idea.DivisionName,
                DepartmentName = idea.DepartmentName
            };
        }

        /// <summary>
        /// Convert list of Ideas to display view models
        /// </summary>
        public List<IdeaDisplayViewModel> ToDisplayViewModels(List<Idea> ideas)
        {
            return ideas.Select(ToDisplayViewModel).ToList();
        }

        /// <summary>
        /// Parse various ID formats and return database ID
        /// Supports: "IMS-0000001", "1", "#IMS-0000001", etc.
        /// </summary>
        public long? ParseAnyIdFormat(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            // Remove common prefixes
            var cleanInput = input.Replace("#", "").Trim();

            // Try as display ID first
            var parsedDisplayId = _ideaCodeService.ParseIdeaDisplayId(cleanInput);
            if (parsedDisplayId.HasValue)
                return parsedDisplayId.Value;

            // Try as direct number
            if (long.TryParse(cleanInput, out long directId))
                return directId;

            return null;
        }

        /// <summary>
        /// Format ID for different contexts
        /// </summary>
        public string FormatIdForContext(long id, IdDisplayContext context)
        {
            return context switch
            {
                IdDisplayContext.Full => _ideaCodeService.FormatIdeaDisplayId(id),
                IdDisplayContext.Short => $"IMS-{id:D3}",
                IdDisplayContext.Hash => $"#{_ideaCodeService.FormatIdeaDisplayId(id)}",
                IdDisplayContext.Numeric => id.ToString(),
                _ => _ideaCodeService.FormatIdeaDisplayId(id)
            };
        }

        /// <summary>
        /// Get status badge class for UI
        /// </summary>
        public string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "Submitted" => "badge bg-primary",
                "Under Review" => "badge bg-warning",
                "Approved" => "badge bg-success",
                "Rejected" => "badge bg-danger",
                "Completed" => "badge bg-success",
                _ => "badge bg-secondary"
            };
        }

        /// <summary>
        /// Get progress percentage for workflow stages
        /// </summary>
        public int GetProgressPercentage(int currentStage, int maxStage)
        {
            if (maxStage <= 0) return 0;
            return Math.Min(100, (currentStage * 100) / maxStage);
        }
    }

    public enum IdDisplayContext
    {
        Full,       // IMS-0000001
        Short,      // IMS-001
        Hash,       // #IMS-0000001
        Numeric     // 1
    }
}