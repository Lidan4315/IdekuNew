// Controllers/ValidationController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services;
using Ideku.Models.ViewModels.Validation;
using Ideku.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Ideku.Controllers
{
    [Authorize]
    public class ValidationController : Controller
    {
        private readonly IdeaService _ideaService;
        private readonly EmailService _emailService;
        private readonly AuthService _authService;
        private readonly AppDbContext _context;
        private readonly ILogger<ValidationController> _logger;

        public ValidationController(
            IdeaService ideaService,
            EmailService emailService,
            AuthService authService,
            AppDbContext context,
            ILogger<ValidationController> logger)
        {
            _ideaService = ideaService;
            _emailService = emailService;
            _authService = authService;
            _context = context;
            _logger = logger;
        }

        // GET: /Validation/Review/5 - Review idea for validation
        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            try
            {
                var idea = await _ideaService.GetIdeaByIdAsync(id);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Check if user has validation rights (you can customize this logic)
                var currentUser = User.Identity?.Name ?? "";
                var user = await _authService.AuthenticateAsync(currentUser);
                
                var allowedRoles = new List<string> { "R01", "R06", "R07", "R08", "R09", "R10", "R11", "R12" };
                if (user?.Role == null || !allowedRoles.Contains(user.Role.Id))
                {
                    TempData["ErrorMessage"] = "You don't have permission to validate ideas.";
                    return RedirectToAction("Index", "Home");
                }

                // Get submitter details
                var submitter = await _authService.GetEmployeeByBadgeAsync(idea.InitiatorId);

                var viewModel = new ValidationReviewViewModel
                {
                    IdeaId = idea.Id,
                    IdeaName = idea.IdeaName,
                    IdeaIssueBackground = idea.IdeaIssueBackground,
                    IdeaSolution = idea.IdeaSolution,
                    SavingCost = idea.SavingCost,
                    AttachmentFile = idea.AttachmentFile,
                    SubmittedDate = idea.SubmittedDate,
                    CurrentStatus = idea.CurrentStatus ?? "Submitted",
                    SubmitterName = submitter?.Name ?? "Unknown",
                    SubmitterEmail = submitter?.Email ?? "",
                    SubmitterId = idea.InitiatorId,
                    CategoryName = idea.Category?.NamaCategory,
                    EventName = idea.Event?.NamaEvent,
                    Division = idea.Division,
                    Department = idea.Department
                };

                return View("ValidationReviewView", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading validation review for idea {id}");
                TempData["ErrorMessage"] = "Unable to load idea for validation.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Validation/Approve/5 - Approve idea
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? comments, decimal? validatedSavingCost)
        {
            try
            {
                var idea = await _ideaService.GetIdeaByIdAsync(id);
                if (idea == null)
                {
                    return Json(new { success = false, message = "Idea not found." });
                }

                // Update idea status
                idea.CurrentStatus = "Approved";
                idea.UpdatedDate = DateTime.UtcNow;
                idea.CurrentStage = 1; // Set stage to S1 on approval
                
                if (validatedSavingCost.HasValue)
                {
                    idea.SavingCostValidated = validatedSavingCost;
                    idea.SavingCostOptionValidated = "Validated";
                }

                if (!string.IsNullOrEmpty(comments))
                {
                    // Store validation comments (you might want to create a separate table for this)
                    idea.Payload = comments;
                }

                await _ideaService.UpdateIdeaAsync(idea);

                // Kirim email notifikasi di latar belakang
                var submitter = await _authService.GetEmployeeByBadgeAsync(idea.InitiatorId);
                if (submitter != null && !string.IsNullOrEmpty(submitter.Email))
                {
                    _ = _emailService.SendIdeaStatusUpdateAsync(
                        submitter.Email,
                        idea.IdeaName,
                        "Approved",
                        comments
                    ).ConfigureAwait(false);
                }

                _logger.LogInformation($"Idea {id} approved by {User.Identity?.Name}");
                return Json(new { success = true, message = "Idea approved successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving idea {id}");
                return Json(new { success = false, message = "An error occurred while approving the idea." });
            }
        }

        // POST: /Validation/Reject/5 - Reject idea
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectReason)
        {
            try
            {
                if (string.IsNullOrEmpty(rejectReason))
                {
                    return Json(new { success = false, message = "Rejection reason is required." });
                }

                var idea = await _ideaService.GetIdeaByIdAsync(id);
                if (idea == null)
                {
                    return Json(new { success = false, message = "Idea not found." });
                }

                // Update idea status
                idea.CurrentStatus = "Rejected";
                idea.UpdatedDate = DateTime.UtcNow;
                idea.RejectReason = rejectReason;
                // CurrentStage does not change on rejection

                await _ideaService.UpdateIdeaAsync(idea);

                // Kirim email notifikasi di latar belakang
                var submitter = await _authService.GetEmployeeByBadgeAsync(idea.InitiatorId);
                if (submitter != null && !string.IsNullOrEmpty(submitter.Email))
                {
                    _ = _emailService.SendIdeaStatusUpdateAsync(
                        submitter.Email,
                        idea.IdeaName,
                        "Rejected",
                        rejectReason
                    ).ConfigureAwait(false);
                }

                _logger.LogInformation($"Idea {id} rejected by {User.Identity?.Name}");
                return Json(new { success = true, message = "Idea rejected successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting idea {id}");
                return Json(new { success = false, message = "An error occurred while rejecting the idea." });
            }
        }


        // GET: /Validation/List - List all ideas pending validation
        [HttpGet]
        public async Task<IActionResult> List()
        {
            try
            {
                // Check if user has validation rights
                var currentUser = User.Identity?.Name ?? "";
                var user = await _authService.AuthenticateAsync(currentUser);

                var allowedRoles = new List<string> { "R01", "R06", "R07", "R08", "R09", "R10", "R11", "R12" };
                if (user?.Role == null || !allowedRoles.Contains(user.Role.Id))
                {
                    TempData["ErrorMessage"] = "You don't have permission to view validation list.";
                    return RedirectToAction("Index", "Home");
                }

                // Get all ideas for the validation list
                var pendingIdeas = await _context.Ideas
                    .Include(i => i.Category)
                    .Include(i => i.Event)
                    .Include(i => i.Initiator)
                        .ThenInclude(e => e.Divisi)
                    .Include(i => i.Initiator)
                        .ThenInclude(e => e.Departement)
                    .OrderByDescending(i => i.SubmittedDate)
                    .ToListAsync();

                var viewModel = new ValidationListViewModel
                {
                    PendingIdeas = pendingIdeas,
                    ValidatorName = user.Name
                };

                return View("ValidationListView", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading validation list");
                TempData["ErrorMessage"] = "Unable to load validation list.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Validation/Delete/5 - Hapus ide
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var idea = await _context.Ideas.FindAsync(id);
                if (idea == null)
                {
                    return Json(new { success = false, message = "Ide tidak ditemukan." });
                }

                _context.Ideas.Remove(idea);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Idea {id} dihapus oleh {User.Identity?.Name}");
                return Json(new { success = true, message = "Ide berhasil dihapus!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kesalahan saat menghapus ide {id}");
                return Json(new { success = false, message = "Terjadi kesalahan saat menghapus ide." });
            }
        }
    }
}
