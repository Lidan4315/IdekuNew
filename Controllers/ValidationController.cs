// Controllers/ValidationController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services;
using Ideku.Models.ViewModels.Validation;
using Ideku.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Ideku.Controllers
{
    [Authorize]
    public class ValidationController : Controller
    {
        private readonly IdeaService _ideaService;
        private readonly AuthService _authService;
        private readonly WorkflowService _workflowService;
        private readonly ILogger<ValidationController> _logger;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly OrganizationService _organizationService;
        private readonly AppDbContext _context;

        public ValidationController(
            IdeaService ideaService,
            AuthService authService,
            WorkflowService workflowService,
            ILogger<ValidationController> logger,
            ICompositeViewEngine viewEngine,
            OrganizationService organizationService,
            AppDbContext context)
        {
            _ideaService = ideaService;
            _authService = authService;
            _workflowService = workflowService;
            _logger = logger;
            _viewEngine = viewEngine;
            _organizationService = organizationService;
            _context = context;
        }

        // GET: /Validation/FilterIdeas - AJAX endpoint for filtering ideas
        [HttpGet]
        public async Task<IActionResult> FilterIdeas(string searchString, string selectedDivision, string selectedDepartment, string selectedStatus, string selectedStage, int pageNumber = 1)
        {
            var (user, isAuthorized) = await CheckValidationPermissionAsync();
            if (user == null || !isAuthorized)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var pendingIdeaIds = (await _workflowService.GetPendingApprovalsForUserAsync(user.EmployeeId)).Select(i => i.Id);
            var query = _context.Ideas
                                .Include(i => i.Initiator)
                                .Include(i => i.TargetDivision)
                                .Include(i => i.TargetDepartment)
                                .Include(i => i.Category)
                                .Where(i => pendingIdeaIds.Contains(i.Id));

            if (!string.IsNullOrEmpty(searchString))
            {
                var upperSearchString = searchString.ToUpper();
                query = query.Where(i =>
                    (i.IdeaName != null && i.IdeaName.ToUpper().Contains(upperSearchString)) ||
                    (i.Initiator != null && i.Initiator.Name.ToUpper().Contains(upperSearchString)) ||
                    (i.Id != null && i.Id.ToUpper().Contains(upperSearchString))
                );
            }

            if (!string.IsNullOrEmpty(selectedDivision))
            {
                query = query.Where(i => i.TargetDivisionId == selectedDivision);
            }

            if (!string.IsNullOrEmpty(selectedDepartment))
            {
                query = query.Where(i => i.TargetDepartmentId == selectedDepartment);
            }

            if (!string.IsNullOrEmpty(selectedStatus))
            {
                query = query.Where(i => i.Status == selectedStatus);
            }

            if (!string.IsNullOrEmpty(selectedStage))
            {
                query = query.Where(i => i.CurrentStage.ToString() == selectedStage);
            }

            var pageSize = 10;
            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var pagedIdeas = query
                .OrderByDescending(i => i.SubmittedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new ValidationListViewModel
            {
                PendingIdeas = pagedIdeas.Select(idea => new ValidationIdeaViewModel
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
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                // Pass filter values back to the view to maintain state in pagination links
                SearchString = searchString,
                SelectedDivision = selectedDivision,
                SelectedDepartment = selectedDepartment,
                SelectedStatus = selectedStatus,
                SelectedStage = !string.IsNullOrEmpty(selectedStage) ? int.Parse(selectedStage) : null
            };

            var tableHtml = await RenderViewToStringAsync("_IdeaListPartial", viewModel.PendingIdeas);
            var paginationHtml = await RenderViewToStringAsync("_PaginationPartial", viewModel);

            return Json(new { tableHtml, paginationHtml });
        }

        // GET: /Validation/List - List all ideas pending validation for the current user
        [HttpGet]
        public async Task<IActionResult> List(string searchString, string selectedDivision, string selectedDepartment, string selectedStatus, string selectedStage, int pageNumber = 1)
        {
            try
            {
                var (user, isAuthorized) = await CheckValidationPermissionAsync();
                if (user == null || !isAuthorized)
                {
                    TempData["ErrorMessage"] = "You don't have permission to view the validation list.";
                    return RedirectToAction("Index", "Home");
                }

                // Get base query of ideas pending for this user
                var pendingIdeaIds = (await _workflowService.GetPendingApprovalsForUserAsync(user.EmployeeId)).Select(i => i.Id);
                var query = _context.Ideas
                                    .Include(i => i.Initiator)
                                    .Include(i => i.TargetDivision)
                                    .Include(i => i.TargetDepartment)
                                    .Include(i => i.Category)
                                    .Where(i => pendingIdeaIds.Contains(i.Id));

                // Apply filters
                if (!string.IsNullOrEmpty(searchString))
                {
                    var upperSearchString = searchString.ToUpper();
                    query = query.Where(i =>
                        (i.IdeaName != null && i.IdeaName.ToUpper().Contains(upperSearchString)) ||
                        (i.Initiator != null && i.Initiator.Name.ToUpper().Contains(upperSearchString)) ||
                        (i.Id != null && i.Id.ToUpper().Contains(upperSearchString))
                    );
                }
                if (!string.IsNullOrEmpty(selectedDivision))
                {
                    query = query.Where(i => i.TargetDivisionId == selectedDivision);
                }
                if (!string.IsNullOrEmpty(selectedDepartment))
                {
                    query = query.Where(i => i.TargetDepartmentId == selectedDepartment);
                }
                if (!string.IsNullOrEmpty(selectedStatus))
                {
                    query = query.Where(i => i.Status == selectedStatus);
                }
                if (!string.IsNullOrEmpty(selectedStage))
                {
                    query = query.Where(i => i.CurrentStage.ToString() == selectedStage);
                }

                // Apply pagination
                var pageSize = 10;
                var totalItems = query.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                var pagedIdeas = query
                    .OrderByDescending(i => i.SubmittedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Get data for filter dropdowns
                var divisions = await _organizationService.GetAllDivisionsAsync();
                var statuses = new List<string> { "Submitted", "Under Review", "Approved", "Rejected", "Completed" }.Select(s => new SelectListItem { Value = s, Text = s }).ToList();
                var stages = new List<int> { 1, 2, 3, 4, 5, 6 }.Select(s => new SelectListItem { Value = s.ToString(), Text = $"Stage {s}" }).ToList();

                var viewModel = new ValidationListViewModel
                {
                    PendingIdeas = pagedIdeas.Select(idea => new ValidationIdeaViewModel
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
                    ValidatorName = user.Name,
                    // Filter data and selected values
                    Divisions = divisions.Select(d => new SelectListItem { Value = d.Id, Text = d.NamaDivisi }).ToList(),
                    Departments = new List<SelectListItem>(), // Populated by JS
                    Statuses = statuses,
                    Stages = stages,
                    SearchString = searchString,
                    SelectedDivision = selectedDivision,
                    SelectedDepartment = selectedDepartment,
                    SelectedStatus = selectedStatus,
                    SelectedStage = !string.IsNullOrEmpty(selectedStage) ? int.Parse(selectedStage) : null,
                    // Pagination data
                    CurrentPage = pageNumber,
                    TotalPages = totalPages
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
        public async Task<IActionResult> Review(string id)
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
        public async Task<IActionResult> Approve(string id, string? comments, decimal? validatedSavingCost)
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
        public async Task<IActionResult> Reject(string id, string rejectReason)
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

        private async Task<string> RenderViewToStringAsync(string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                viewName = ControllerContext.ActionDescriptor.ActionName;
            }
            ViewData.Model = model;
            using (var writer = new StringWriter())
            {
                IViewEngine viewEngine = _viewEngine ?? HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                ViewEngineResult viewResult = viewEngine.FindView(ControllerContext, viewName, false);

                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"{viewName} does not match any available view");
                }

                var viewContext = new ViewContext(
                    ControllerContext,
                    viewResult.View,
                    ViewData,
                    TempData,
                    writer,
                    new HtmlHelperOptions()
                );
                await viewResult.View.RenderAsync(viewContext);
                return writer.GetStringBuilder().ToString();
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
