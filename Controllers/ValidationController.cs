// Controllers/ValidationController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services;
using Ideku.Models.ViewModels.Validation;
using Ideku.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

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
            var (user, isAuthorized) = await CheckValidationPermissionAsync();
            if (!isAuthorized)
            {
                TempData["ErrorMessage"] = "You don't have permission to validate ideas.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var idea = await _ideaService.GetIdeaByIdAsync(id);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found.";
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
        public async Task<IActionResult> List(string searchString, string selectedDivision, string selectedDepartment, string selectedStatus, int? selectedStage, int pageNumber = 1)
        {
            try
            {
                const int pageSize = 8; // Items per page, adjusted to fit screen
                var (user, isAuthorized) = await CheckValidationPermissionAsync();
                if (!isAuthorized)
                {
                    TempData["ErrorMessage"] = "You don't have permission to view the validation list.";
                    return RedirectToAction("Index", "Home");
                }

                var ideasQuery = GetFilteredIdeasQuery(searchString, selectedDivision, selectedDepartment, selectedStatus, selectedStage);

                var totalItems = await ideasQuery.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var pendingIdeasRaw = await ideasQuery
                    .OrderByDescending(i => i.SubmittedDate)
                    .ThenByDescending(i => i.Id) // Add secondary sort for stable ordering
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Create lookup dictionaries for performance
                var divisions = await _context.Divisi.ToDictionaryAsync(d => d.Id, d => d.NamaDivisi);
                var departments = await _context.Departement.ToDictionaryAsync(d => d.Id, d => d.NamaDepartement);

                var pendingIdeasVms = pendingIdeasRaw.Select(idea => new ValidationIdeaViewModel
                {
                    Id = idea.Id,
                    IdeaName = idea.IdeaName,
                    InitiatorName = idea.Initiator?.Name,
                    DivisionName = idea.Division != null && divisions.ContainsKey(idea.Division) ? divisions[idea.Division] : idea.Division,
                    DepartmentName = idea.Department != null && departments.ContainsKey(idea.Department) ? departments[idea.Department] : idea.Department,
                    CategoryName = idea.Category?.NamaCategory,
                    CurrentStage = idea.CurrentStage,
                    CurrentStatus = idea.CurrentStatus,
                    SavingCost = idea.SavingCost,
                    SubmittedDate = idea.SubmittedDate
                }).ToList();

                var viewModel = new ValidationListViewModel
                {
                    PendingIdeas = pendingIdeasVms,
                    ValidatorName = user.Name,
                    SearchString = searchString,
                    SelectedDivision = selectedDivision,
                    SelectedDepartment = selectedDepartment,
                    SelectedStatus = selectedStatus,
                    SelectedStage = selectedStage,
                    CurrentPage = pageNumber,
                    TotalPages = totalPages,
                    Divisions = await _context.Divisi.Select(d => new SelectListItem { Value = d.Id, Text = d.NamaDivisi }).ToListAsync(),
                    Departments = await _context.Departement.Select(d => new SelectListItem { Value = d.Id, Text = d.NamaDepartement }).ToListAsync(),
                    Statuses = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "Submitted", Text = "Submitted" },
                        new SelectListItem { Value = "Under Review", Text = "Under Review" },
                        new SelectListItem { Value = "Approved", Text = "Approved" },
                        new SelectListItem { Value = "Rejected", Text = "Rejected" }
                    },
                    Stages = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "0", Text = "Stage 0" },
                        new SelectListItem { Value = "1", Text = "Stage 1" },
                        new SelectListItem { Value = "2", Text = "Stage 2" },
                        new SelectListItem { Value = "3", Text = "Stage 3" }
                    }
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

        [HttpGet("Validation/FilterIdeas")] // Add explicit route to resolve ambiguity
        public async Task<IActionResult> FilterIdeas(string searchString, string selectedDivision, string selectedDepartment, string selectedStatus, int? selectedStage, int pageNumber = 1)
        {
            const int pageSize = 8;
            var ideasQuery = GetFilteredIdeasQuery(searchString, selectedDivision, selectedDepartment, selectedStatus, selectedStage);

            var totalItems = await ideasQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var filteredIdeasRaw = await ideasQuery
                .OrderByDescending(i => i.SubmittedDate)
                .ThenByDescending(i => i.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create lookup dictionaries for performance
            var divisions = await _context.Divisi.ToDictionaryAsync(d => d.Id, d => d.NamaDivisi);
            var departments = await _context.Departement.ToDictionaryAsync(d => d.Id, d => d.NamaDepartement);

            var filteredIdeasVms = filteredIdeasRaw.Select(idea => new ValidationIdeaViewModel
            {
                Id = idea.Id,
                IdeaName = idea.IdeaName,
                InitiatorName = idea.Initiator?.Name,
                DivisionName = idea.Division != null && divisions.ContainsKey(idea.Division) ? divisions[idea.Division] : idea.Division,
                DepartmentName = idea.Department != null && departments.ContainsKey(idea.Department) ? departments[idea.Department] : idea.Department,
                CategoryName = idea.Category?.NamaCategory,
                CurrentStage = idea.CurrentStage,
                CurrentStatus = idea.CurrentStatus,
                SavingCost = idea.SavingCost,
                SubmittedDate = idea.SubmittedDate
            }).ToList();

            // We need to build a ViewModel to pass to the pagination partial
            var paginationViewModel = new ValidationListViewModel
            {
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(await ideasQuery.CountAsync() / (double)pageSize),
                // Pass filter values to maintain state in pagination links
                SearchString = searchString,
                SelectedDivision = selectedDivision,
                SelectedDepartment = selectedDepartment,
                SelectedStatus = selectedStatus,
                SelectedStage = selectedStage
            };

            var tableHtml = await RenderPartialViewToStringAsync("_IdeaListPartial", filteredIdeasVms);
            var paginationHtml = await RenderPartialViewToStringAsync("_PaginationPartial", paginationViewModel);

            return Json(new { tableHtml, paginationHtml });
        }

        private IQueryable<Models.Entities.Idea> GetFilteredIdeasQuery(string searchString, string selectedDivision, string selectedDepartment, string selectedStatus, int? selectedStage)
        {
            var ideasQuery = _context.Ideas
                .Include(i => i.Initiator)
                    .ThenInclude(e => e.Divisi)
                .Include(i => i.Initiator)
                    .ThenInclude(e => e.Departement)
                .Include(i => i.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                ideasQuery = ideasQuery.Where(i =>
                    i.IdeaName.Contains(searchString) ||
                    i.Id.ToString().Contains(searchString) ||
                    (i.Initiator != null && i.Initiator.Name.Contains(searchString)));
            }
            if (!string.IsNullOrEmpty(selectedDivision))
            {
                ideasQuery = ideasQuery.Where(i => i.Division == selectedDivision);
            }
            if (!string.IsNullOrEmpty(selectedDepartment))
            {
                ideasQuery = ideasQuery.Where(i => i.Department == selectedDepartment);
            }
            if (!string.IsNullOrEmpty(selectedStatus))
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStatus == selectedStatus);
            }
            if (selectedStage.HasValue)
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStage == selectedStage.Value);
            }

            return ideasQuery;
        }

        private async Task<(Models.Entities.User user, bool isAuthorized)> CheckValidationPermissionAsync()
        {
            var currentUser = User.Identity?.Name ?? "";
            var user = await _authService.AuthenticateAsync(currentUser);
            var allowedRoles = new List<string> { "R01", "R06", "R07", "R08", "R09", "R10", "R11", "R12" };
            var isAuthorized = user?.Role != null && allowedRoles.Contains(user.Role.Id);
            return (user, isAuthorized);
        }

        // Helper method to render partial view to a string
        private async Task<string> RenderPartialViewToStringAsync(string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.ActionDescriptor.ActionName;

            ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                var viewEngine = HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine)) as Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine;
                var viewResult = viewEngine.FindView(ControllerContext, viewName, false);

                if (viewResult.Success == false)
                {
                    return $"A view with the name {viewName} could not be found";
                }

                var viewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext(
                    ControllerContext,
                    viewResult.View,
                    ViewData,
                    TempData,
                    writer,
                    new Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return writer.GetStringBuilder().ToString();
            }
        }

        // POST: /Validation/Delete/5 - Hapus ide
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var idea = await _ideaService.GetIdeaByIdAsync(id);
                if (idea == null)
                {
                    return Json(new { success = false, message = "Ide tidak ditemukan." });
                }

                await _ideaService.DeleteIdeaAsync(id);

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
