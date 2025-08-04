using Ideku.Models.Entities;
using System.Collections.Generic;

namespace Ideku.Models.ViewModels.Milestone
{
    public class MilestoneIndexViewModel
    {
        public Entities.Idea Idea { get; set; }
        public List<IdeaMilestone> Milestones { get; set; }
        public SavingMonitoring SavingMonitoring { get; set; }
        public string IdeaId { get; set; }
    }
}
