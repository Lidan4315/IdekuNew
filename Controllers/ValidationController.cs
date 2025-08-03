// Controllers/ValidationController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services;
using Ideku.Models.ViewModels.Validation;
using Ideku.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Ideku.Controllers
{
    [Authorize]
    public class ValidationController : Controller
    {
        private readonly IdeaService _ideaService;
        private readonly AuthService _authService;
        private readonly WorkflowService _workflowService;
        private readonly ILogger<ValidationController> _logger;

        public ValidationController(
            IdeaService ideaService,
            AuthService authService,
            WorkflowService workflowService,
            ILogger<ValidationController> logger)
        {
            _ideaService = ideaService;
            _authService = authService;
            _workflowService = workflowService;
            _logger = logger;
        }

        // GET: /Validation/List - List all ideas pending validation for the current user
        [HttpGet]
        public async Task<IActionResult> List()
        {
            try
            {
                var (user, isAuthorized) = await CheckValidationPermissionAsync();
                if (user == null || !isAuthorized)
                {
                    TempData["ErrorMessage"] = "You don't have permission to view the validation list.";
                    return RedirectToAction("Index", "Home");
                }

                // Get only the ideas pending for this specific user, according to our corrected workflow logic
                var pendingIdeas = await _workflowService.GetPendingApprovalsForUserAsync(user.EmployeeId);

                var viewModel = new ValidationListViewModel
                {
                    PendingIdeas = pendingIdeas.Select(idea => new ValidationIdeaViewModel
                    {
                        Id = idea.Id,
                        IdeaName = idea.IdeaName,
                        InitiatorName = idea.Initiator?.Name,
                        DivisionName = idea.TargetDivision?.NamaDivisi,
                        DepartmentName = idea.TargetDepartment?.NamaDepartement,
                        CategoryName = idea.Category?.NamaCategory,
                        CurrentStage = idea.CurrentStage,
                        CurrentStatus = idea.Status,
                        SavingCost = idea.SavingCost,
                        SubmittedDate = idea.SubmittedDate
                    }).ToList(),
                    ValidatorName = user.Name
                };

                return View("ValidationListView", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading validation list for user {Username}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Unable to load your validation list.";
                return RedirectToAction("Index", "Home");
            }
        }


        // GET: /Validation/Review/5 - Review idea for validation
        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var (user, isAuthorized) = await CheckValidationPermissionAsync();
            if (user == null || !isAuthorized)
            {
                TempData["ErrorMessage"] = "You don't have permission to validate ideas.";
                return RedirectToAction("List");
            }

            try
            {
                var idea = await _ideaService.GetIdeaByIdAsync(id);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found.";
                    return RedirectToAction("List");
                }

                // Extra check: Does this idea actually appear in the user's pending list?
                var pendingIdeas = await _workflowService.GetPendingApprovalsForUserAsync(user.EmployeeId);
                if (!pendingIdeas.Any(p => p.Id == id))
                {
                    TempData["ErrorMessage"] = "This idea is not currently awaiting your approval.";
                    return RedirectToAction("List");
                }

                var submitter = await _authService.GetEmployeeByBadgeAsync(idea.InitiatorId);

                var viewModel = new ValidationReviewViewModel
                {
                    IdeaId = idea.Id,
                    IdeaName = idea.IdeaName,
                    IdeaIssueBackground = idea.IssueBackground,
                    IdeaSolution = idea.Solution,
                    SavingCost = idea.SavingCost,
                    AttachmentFile = idea.AttachmentFiles,
                    SubmittedDate = idea.SubmittedDate,
                    CurrentStatus = idea.Status,
                    SubmitterName = submitter?.Name ?? "Unknown",
                    SubmitterEmail = submitter?.Email ?? "",
                    SubmitterId = idea.InitiatorId,
                    CategoryName = idea.Category?.NamaCategory,
                    EventName = idea.Event?.NamaEvent,
                    Division = idea.TargetDivision?.NamaDivisi,
                    Department = idea.TargetDepartment?.NamaDepartement
                };

                return View("ValidationReviewView", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading validation review for idea {id}");
                TempData["ErrorMessage"] = "Unable to load idea for validation.";
                return RedirectToAction("List");
            }
        }

        // POST: /Validation/Approve/5 - Approve idea
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? comments, decimal? validatedSavingCost)
        {
            try
            {
                var currentUser = await _authService.AuthenticateAsync(User.Identity!.Name!);
                if (currentUser?.EmployeeId == null)
                {
                    return Json(new { success = false, message = "User not authenticated or not linked to an employee." });
                }

                var success = await _workflowService.AdvanceStageAsync(id, currentUser.EmployeeId, comments, validatedSavingCost);

                if (success)
                {
                    _logger.LogInformation($"Idea {id} approved by {currentUser.Username}");
                    return Json(new { success = true, message = "Idea approved and advanced to the next stage." });
                }
                else
                {
                    _logger.LogWarning($"Failed to approve idea {id} by {currentUser.Username}");
                    return Json(new { success = false, message = "Failed to approve the idea. It might not be your turn to approve or an error occurred." });
                }
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

                var currentUser = await _authService.AuthenticateAsync(User.Identity!.Name!);
                if (currentUser?.EmployeeId == null)
                {
                    return Json(new { success = false, message = "User not authenticated or not linked to an employee." });
                }

                var success = await _workflowService.RejectIdeaAsync(id, currentUser.EmployeeId, rejectReason);

                if (success)
                {
                    _logger.LogInformation($"Idea {id} rejected by {currentUser.Username}");
                    return Json(new { success = true, message = "Idea has been rejected." });
                }
                else
                {
                    _logger.LogWarning($"Failed to reject idea {id} by {currentUser.Username}");
                    return Json(new { success = false, message = "Failed to reject the idea." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting idea {id}");
                return Json(new { success = false, message = "An error occurred while rejecting the idea." });
            }
        }

        private async Task<(Models.Entities.User? user, bool isAuthorized)> CheckValidationPermissionAsync()
        {
            var currentUserName = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUserName))
            {
                return (null, false);
            }

            var user = await _authService.AuthenticateAsync(currentUserName);
            if (user?.Role == null)
            {
                return (user, false);
            }

            // A user is a validator if their role has an approval level greater than 0.
            var isAuthorized = user.Role.ApprovalLevel > 0;
            
            return (user, isAuthorized);
        }
    }
}
