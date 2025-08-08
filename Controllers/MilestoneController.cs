using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services;
using Ideku.Data.Context;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ideku.Models.Entities;

namespace Ideku.Controllers
{
    [Authorize]
    public class MilestoneController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;
        private readonly ILogger<MilestoneController> _logger;

        private readonly ApproverService _approverService;

        public MilestoneController(AppDbContext context, AuthService authService, ILogger<MilestoneController> logger, ApproverService approverService)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
            _approverService = approverService;
        }

        public IActionResult Index()
        {
            // Logic to get list of ideas for milestone will be added here later.
            // For now, just return the view.
            return View();
        }

        // GET: /Milestone/Details/5
        public async Task<IActionResult> Details(string id)
        {
            var idea = await _context.Ideas
                .Include(i => i.Milestones)
                .Include(i => i.SavingMonitoring)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (idea == null)
            {
                return NotFound();
            }

            var currentUser = await _authService.AuthenticateAsync(User.Identity.Name);
            var workstreamLeader = await _approverService.GetWorkstreamLeaderAsync(idea.TargetDivisionId, idea.TargetDepartmentId);

            if (currentUser?.EmployeeId != workstreamLeader?.Id && currentUser?.Role.RoleName != "Superuser")
            {
                return Forbid(); // User is not authorized
            }

            var viewModel = new Ideku.Models.ViewModels.Milestone.MilestoneIndexViewModel
            {
                Idea = idea,
                Milestones = idea.Milestones.OrderBy(m => m.CreatedDate).ToList(),
                SavingMonitoring = idea.SavingMonitoring.FirstOrDefault() ?? new SavingMonitoring { IdeaId = id },
                IdeaId = id
            };

            return View(viewModel);
        }

        // POST: /Milestone/CreateMilestone
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMilestone(string ideaId, string milestoneText)
        {
            var idea = await _context.Ideas.FindAsync(ideaId);
            var currentUser = await _authService.AuthenticateAsync(User.Identity.Name);

            if (idea == null || currentUser == null)
            {
                return NotFound();
            }

            var workstreamLeader = await _approverService.GetWorkstreamLeaderAsync(idea.TargetDivisionId, idea.TargetDepartmentId);
            if (currentUser?.EmployeeId != workstreamLeader?.Id && currentUser?.Role.RoleName != "Superuser")
            {
                return Forbid();
            }

            var milestone = new IdeaMilestone
            {
                IdeaId = ideaId,
                MilestoneTitle = milestoneText,
                CreatedBy = currentUser.EmployeeId,
                Status = "Pending" // Or some default status
            };

            _context.IdeaMilestones.Add(milestone);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = ideaId });
        }

        // POST: /Milestone/UpdateMonitoring
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMonitoring(Ideku.Models.ViewModels.Milestone.SavingMonitoringViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // If model is not valid, redisplay the page with existing data
                return RedirectToAction("Details", new { id = model.IdeaId });
            }

            var idea = await _context.Ideas.FindAsync(model.IdeaId);
            var currentUser = await _authService.AuthenticateAsync(User.Identity.Name);

            if (idea == null || currentUser == null)
            {
                return NotFound();
            }

            var workstreamLeader = await _approverService.GetWorkstreamLeaderAsync(idea.TargetDivisionId, idea.TargetDepartmentId);
            if (currentUser?.EmployeeId != workstreamLeader?.Id && currentUser?.Role.RoleName != "Superuser")
            {
                return Forbid();
            }

            var monitoring = await _context.SavingMonitoring.FirstOrDefaultAsync(m => m.IdeaId == model.IdeaId);

            if (monitoring == null)
            {
                // Create new monitoring entry
                monitoring = new SavingMonitoring
                {
                    IdeaId = model.IdeaId,
                    PlannedSaving = model.PlannedSaving,
                    ActualSaving = model.ActualSaving,
                    ReportedBy = currentUser.EmployeeId
                };
                _context.SavingMonitoring.Add(monitoring);
            }
            else
            {
                // Update existing entry
                monitoring.PlannedSaving = model.PlannedSaving;
                monitoring.ActualSaving = model.ActualSaving;
                monitoring.UpdatedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = model.IdeaId });
        }
    }
}
