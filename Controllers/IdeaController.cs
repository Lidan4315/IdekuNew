using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Ideku.Services;
using Ideku.Models.Entities;
using Ideku.Models.ViewModels.Idea;
using Ideku.Data.Context;
using Microsoft.Extensions.Options;
using System.IO.Compression;

namespace Ideku.Controllers
{
    [Authorize]
    public class IdeaController : Controller
    {
        private readonly IdeaService _ideaService;
        private readonly AuthService _authService;
        private readonly FileService _fileService;
        private readonly EmailService _emailService;
        private readonly OrganizationService _organizationService;
        private readonly AppDbContext _context;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<IdeaController> _logger;
        private readonly ApproverService _approverService;

        public IdeaController(
            IdeaService ideaService,
            AuthService authService,
            FileService fileService,
            EmailService emailService,
            OrganizationService organizationService,
            AppDbContext context,
            IOptions<EmailSettings> emailSettings,
            ILogger<IdeaController> logger,
            ApproverService approverService)
        {
            _ideaService = ideaService;
            _authService = authService;
            _fileService = fileService;
            _emailService = emailService;
            _organizationService = organizationService;
            _context = context;
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _approverService = approverService;
        }

        // 🔥 NEW: API endpoint untuk mendapatkan departements berdasarkan division
        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByDivision(string divisionId)
        {
            try
            {
                if (string.IsNullOrEmpty(divisionId))
                {
                    return Json(new { success = false, message = "Division ID is required" });
                }

                var departments = await _organizationService.GetDepartmentsByDivisionIdAsync(divisionId);
                var result = departments.Select(d => new 
                {
                    id = d.Id,
                    name = d.NamaDepartement,
                    fullName = $"{d.NamaDepartement} ({d.Divisi?.NamaDivisi})"
                });

                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                // Log error - in production, use proper logging
                // _logger.LogError(ex, "Error occurred while fetching departments for division: {DivisionId}", divisionId);
                return Json(new { success = false, message = "An error occurred while fetching departments" });
            }
        }

        // 🔥 NEW: API endpoint untuk mendapatkan data employee berdasarkan badge number
        [HttpGet]
        public async Task<IActionResult> GetEmployeeByBadge(string badgeNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(badgeNumber))
                {
                    return Json(new { success = false, message = "Badge number is required" });
                }

                var employee = await _authService.GetEmployeeByBadgeAsync(badgeNumber);

                if (employee == null)
                {
                    return Json(new { success = false, message = "Employee not found" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        name = employee.Name,
                        email = employee.Email,
                        position = employee.PositionTitle,
                        division = employee.Divisi?.NamaDivisi ?? "",
                        department = employee.Departement?.NamaDepartement ?? "",
                        divisionId = employee.DivisiId,
                        departmentId = employee.DepartementId
                    }
                });
            }
            catch (Exception ex)
            {
                // Log error - in production, use proper logging
                // _logger.LogError(ex, "Error occurred while fetching employee data for badge: {BadgeNumber}", badgeNumber);
                return Json(new { success = false, message = "An error occurred while fetching employee data" });
            }
        }

            // GET: /Idea/Index - Tampilkan daftar ideas user dengan filter dan pagination
            public async Task<IActionResult> Index(
                bool success = false,
                string? searchString = null,
                int? selectedCategory = null,
                int? selectedEvent = null,
                string? selectedStatus = null,
                int page = 1,
                int pageSize = 10)
            {
                if (success)
                {
                    TempData["SuccessMessage"] = "Idea submitted successfully! Validation emails have been sent to reviewers.";
                }

                try
                {
                    // Get current user badge
                    var currentUserBadge = await _authService.GetCurrentUserBadgeNumberAsync(User);
                    if (string.IsNullOrEmpty(currentUserBadge))
                    {
                        TempData["ErrorMessage"] = "Could not determine your employee ID.";
                        return View(new IdeaIndexViewModel());
                    }

                    // Get all user ideas first
                    var allIdeas = await _ideaService.GetUserIdeasAsync(currentUserBadge);

                    // Apply filters
                    var filteredIdeas = allIdeas.AsQueryable();

                    if (!string.IsNullOrEmpty(searchString))
                    {
                        filteredIdeas = filteredIdeas.Where(i => 
                            i.IdeaName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                            i.Id.Contains(searchString, StringComparison.OrdinalIgnoreCase));
                    }

                    if (selectedCategory.HasValue)
                    {
                        filteredIdeas = filteredIdeas.Where(i => i.CategoryId == selectedCategory.Value);
                    }

                    if (selectedEvent.HasValue)
                    {
                        filteredIdeas = filteredIdeas.Where(i => i.EventId == selectedEvent.Value);
                    }

                    if (!string.IsNullOrEmpty(selectedStatus))
                    {
                        filteredIdeas = filteredIdeas.Where(i => i.Status == selectedStatus);
                    }

                    // Calculate pagination
                    var totalItems = filteredIdeas.Count();
                    var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                    var paginatedIdeas = filteredIdeas
                        .OrderByDescending(i => i.SubmittedDate)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    // Get statistics for all user ideas (not filtered)
                    var (total, pending, approved) = await _ideaService.GetIdeaStatsAsync(currentUserBadge);

                    // Populate dropdown data
                    var categories = await _organizationService.GetAllCategoriesAsync();
                    var events = await _organizationService.GetAllEventsAsync();

                    var viewModel = new IdeaIndexViewModel
                    {
                        Ideas = paginatedIdeas,
                        CurrentUserName = User.Identity?.Name ?? "User",
                        TotalIdeas = total,
                        PendingIdeas = pending,
                        ApprovedIdeas = approved,
                        
                        // Filter data
                        Categories = categories.Select(c => new SelectListItem 
                        { 
                            Value = c.Id.ToString(), 
                            Text = c.NamaCategory,
                            Selected = c.Id == selectedCategory
                        }).ToList(),
                        
                        Events = events.Select(e => new SelectListItem 
                        { 
                            Value = e.Id.ToString(), 
                            Text = e.NamaEvent,
                            Selected = e.Id == selectedEvent
                        }).ToList(),
                        
                        Statuses = new List<SelectListItem>
                        {
                            new() { Value = "Submitted", Text = "Submitted", Selected = selectedStatus == "Submitted" },
                            new() { Value = "Under Review", Text = "Under Review", Selected = selectedStatus == "Under Review" },
                            new() { Value = "Approved", Text = "Approved", Selected = selectedStatus == "Approved" },
                            new() { Value = "Rejected", Text = "Rejected", Selected = selectedStatus == "Rejected" },
                            new() { Value = "Completed", Text = "Completed", Selected = selectedStatus == "Completed" }
                        },

                        // Selected values
                        SelectedCategory = selectedCategory,
                        SelectedEvent = selectedEvent,
                        SelectedStatus = selectedStatus,
                        SearchString = searchString,

                        // Pagination
                        CurrentPage = page,
                        TotalPages = totalPages,
                        PageSize = pageSize
                    };

                    // Handle AJAX requests for filtering
                    if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return PartialView("_MyIdeasTablePartial", viewModel);
                    }

                    return View(viewModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading ideas for user: {Username}", User.Identity?.Name);
                    TempData["ErrorMessage"] = "Unable to load your ideas.";
                    return View(new IdeaIndexViewModel());
                }
            }

        // GET: /Idea/Create - Menampilkan form untuk membuat idea
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                await PopulateDropdownsAsync();
                return View(new IdeaCreateViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create form");
                TempData["ErrorMessage"] = "Unable to load the form.";
                return RedirectToAction("Index");
            }
        }

        // POST: /Idea/Create - Proses create idea
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IdeaCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = "Please correct the validation errors.", errors = errors });
                }

                var (success, createdIdea, serviceErrors) = await _ideaService.CreateIdeaFromViewModelAsync(model);

                if (success)
                {
                    return Json(new { success = true, message = "Idea submitted successfully! A notification will be sent to the validator." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to create idea.", errors = serviceErrors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating idea");
                return Json(new { success = false, message = "An unexpected error occurred while submitting your idea." });
            }
        }

        // GET: /Idea/Details/5 - Tampilkan detail idea
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var idea = await _ideaService.GetIdeaByIdAsync(id);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found.";
                    return RedirectToAction("Index");
                }

                var currentUser = await _authService.AuthenticateAsync(User.Identity.Name);
                if (currentUser == null)
                {
                    return Forbid();
                }

                // General access check
                bool isInitiator = idea.InitiatorId == currentUser.EmployeeId;
                bool isSuperuser = currentUser.Role.RoleName == "Superuser";
                // TODO: Add a more robust check for any approver in the chain
                bool isApprover = true; 

                if (!isInitiator && !isSuperuser && !isApprover)
                {
                     TempData["ErrorMessage"] = "You don't have permission to view this idea.";
                    return RedirectToAction("Index");
                }

                // Specific permission for managing milestones
                var workstreamLeader = await _approverService.GetWorkstreamLeaderAsync(idea.TargetDivisionId, idea.TargetDepartmentId);
                ViewBag.CanManageMilestones = (currentUser.EmployeeId == workstreamLeader?.Id || isSuperuser);

                return View(idea);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading idea details for ID {IdeaId}", id);
                TempData["ErrorMessage"] = "Unable to load idea details.";
                return RedirectToAction("Index");
            }
        }

        // GET: /Idea/Edit/5 - Tampilkan form edit idea
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var idea = await _ideaService.GetIdeaByIdAsync(id);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found.";
                    return RedirectToAction("Index");
                }

                // Check permissions
                var currentUserBadge = await _authService.GetCurrentUserBadgeNumberAsync(User);
                if (idea.InitiatorId != currentUserBadge)
                {
                    TempData["ErrorMessage"] = "You don't have permission to edit this idea.";
                    return RedirectToAction("Index");
                }

                // Only allow edit if status is Submitted
                if (idea.Status != "Submitted")
                {
                    TempData["ErrorMessage"] = "You can only edit ideas that haven't been reviewed yet.";
                    return RedirectToAction("Details", new { id = id });
                }

                // Convert to ViewModel
                var viewModel = new IdeaCreateViewModel
                {
                    BadgeNumber = idea.InitiatorId,
                    ToDivision = idea.TargetDivisionId,
                    ToDepartment = idea.TargetDepartmentId,
                    Category = idea.CategoryId,
                    Event = idea.EventId,
                    IdeaName = idea.IdeaName,
                    IdeaIssueBackground = idea.IssueBackground,
                    IdeaSolution = idea.Solution,
                    SavingCost = idea.SavingCost
                };

                await PopulateDropdownsAsync();

                ViewBag.IsEdit = true;
                ViewBag.IdeaId = id;
                ViewBag.CurrentAttachment = idea.AttachmentFiles;

                return View("Create", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading idea for editing. ID: {IdeaId}", id);
                TempData["ErrorMessage"] = "Unable to load idea for editing.";
                return RedirectToAction("Index");
            }
        }

        // POST: /Idea/Edit/5 - Proses update idea
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, IdeaCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var modelErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = "Please correct the validation errors.", errors = modelErrors });
                }

                var currentUserBadge = await _authService.GetCurrentUserBadgeNumberAsync(User);
                var (success, serviceErrors) = await _ideaService.UpdateIdeaFromViewModelAsync(id, model, currentUserBadge);

                if (success)
                {
                    return Json(new { success = true, message = "Idea updated successfully!", redirectUrl = Url.Action("Index", "Idea") });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update idea.", errors = serviceErrors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating idea {IdeaId}", id);
                return Json(new { success = false, message = "An unexpected error occurred while updating your idea." });
            }
        }

        // POST: /Idea/Delete/5 - Delete idea
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var idea = await _ideaService.GetIdeaByIdAsync(id);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found.";
                    return RedirectToAction("Index");
                }

                // Check permissions
                var currentUserBadge = await _authService.GetCurrentUserBadgeNumberAsync(User);
                if (idea.InitiatorId != currentUserBadge)
                {
                    TempData["ErrorMessage"] = "You don't have permission to delete this idea.";
                    return RedirectToAction("Index");
                }

                // Only allow delete if status is Submitted
                if (idea.Status != "Submitted")
                {
                    TempData["ErrorMessage"] = "You can only delete ideas that haven't been reviewed yet.";
                    return RedirectToAction("Index");
                }

                // Delete attached file if exists
                if (!string.IsNullOrEmpty(idea.AttachmentFiles))
                {
                    _fileService.DeleteFile(idea.AttachmentFiles);
                }

                await _ideaService.DeleteIdeaAsync(id);

                TempData["SuccessMessage"] = "Idea deleted successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting idea {IdeaId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the idea.";
                return RedirectToAction("Index");
            }
        }

        // GET: /Idea/DownloadAll/5 - Download all attachments as a zip file
        [HttpGet]
        public async Task<IActionResult> DownloadAll(string ideaId)
        {
            var (idea, hasAccess) = await CheckFileAccessPermissionAsync(ideaId);
            if (!hasAccess)
            {
                return RedirectToAction("AccessDenied", "Account", new { ReturnUrl = Request.Path });
            }
            if (idea == null || string.IsNullOrEmpty(idea.AttachmentFiles))
            {
                return NotFound();
            }

            try
            {

                var stageString = $"S{idea.CurrentStage}";
                var zipFileName = $"{idea.InitiatorId}_{stageString}.zip";
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var file in idea.AttachmentFiles.Split(';', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", file);
                            if (System.IO.File.Exists(filePath))
                            {
                                var originalFileName = file.Length > 37 ? file.Substring(37) : file;
                                archive.CreateEntryFromFile(filePath, originalFileName);
                            }
                        }
                    }

                    return File(memoryStream.ToArray(), "application/zip", zipFileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating zip file for idea {IdeaId}", ideaId);
                TempData["ErrorMessage"] = "Unable to create zip file for download.";
                return RedirectToAction("Index");
            }
        }

        // GET: /Idea/Download/filename - Download attachment
        [HttpGet]
        public async Task<IActionResult> Download(string filename, string ideaId)
        {
            var (idea, hasAccess) = await CheckFileAccessPermissionAsync(ideaId, filename);
            if (!hasAccess || idea == null)
            {
                return RedirectToAction("AccessDenied", "Account", new { ReturnUrl = Request.Path });
            }

            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", filename);
                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "File not found.";
                    return RedirectToAction("Details", new { id = ideaId });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var originalFileName = filename.Length > 37 ? filename.Substring(37) : filename;

                return File(fileBytes, "application/octet-stream", originalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {Filename} for idea {IdeaId}", filename, ideaId);
                TempData["ErrorMessage"] = "Unable to download file.";
                return RedirectToAction("Index");
            }
        }

        // 🔥 NEW: GET: /Idea/ViewAttachment/filename - View attachment inline
        [HttpGet]
        public async Task<IActionResult> ViewAttachment(string filename, string ideaId)
        {
            var (idea, hasAccess) = await CheckFileAccessPermissionAsync(ideaId, filename);
            if (!hasAccess || idea == null)
            {
                return RedirectToAction("AccessDenied", "Account", new { ReturnUrl = Request.Path });
            }

            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", filename);
                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "File not found.";
                    return RedirectToAction("Details", new { id = ideaId });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                
                // Determine content type for inline viewing
                var contentType = GetContentType(filename);

                // 🔥 FIX: Return the file without a download name.
                // This encourages the browser to display the file inline instead of downloading it.
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing file {Filename} for idea {IdeaId}", filename, ideaId);
                TempData["ErrorMessage"] = "Unable to view file.";
                return RedirectToAction("Index");
            }
        }

        private async Task<(Idea? idea, bool hasAccess)> CheckFileAccessPermissionAsync(string ideaId, string? requiredFilename = null)
        {
            var idea = await _ideaService.GetIdeaByIdAsync(ideaId);
            if (idea == null)
            {
                return (null, false);
            }

            if (requiredFilename != null && (string.IsNullOrEmpty(idea.AttachmentFiles) || !idea.AttachmentFiles.Split(';').Contains(requiredFilename)))
            {
                return (idea, false);
            }

            var currentUserBadge = await _authService.GetCurrentUserBadgeNumberAsync(User);
            if (string.IsNullOrEmpty(currentUserBadge))
            {
                return (idea, false);
            }

            if (idea.InitiatorId == currentUserBadge)
            {
                return (idea, true);
            }

            var user = await _authService.AuthenticateAsync(currentUserBadge);
            var allowedRoles = new List<string> { "R01", "R06", "R07", "R08", "R09", "R10", "R11", "R12" };
            if (user?.Role != null && allowedRoles.Contains(user.Role.Id))
            {
                return (idea, true);
            }

            return (idea, false);
        }

        private async Task PopulateDropdownsAsync()
        {
            ViewBag.Divisions = await _organizationService.GetAllDivisionsAsync();
            ViewBag.Categories = await _organizationService.GetAllCategoriesAsync();
            ViewBag.Events = await _organizationService.GetAllEventsAsync();
        }

        private string GetContentType(string path)
        {
            var types = new Dictionary<string, string>
            {
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"}
            };
            
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.GetValueOrDefault(ext, "application/octet-stream");
        }
        
        // API: /Idea/GetUserStats
        [HttpGet]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                // 🔥 FIX: Menggunakan BadgeNumber dari user yang login untuk statistik
                var currentUserBadge = await _authService.GetCurrentUserBadgeNumberAsync(User);
                if (string.IsNullOrEmpty(currentUserBadge))
                {
                    return Json(new { success = false, message = "Could not determine your employee ID." });
                }

                var (total, pending, approved) = await _ideaService.GetIdeaStatsAsync(currentUserBadge);

                return Json(new
                {
                    success = true,
                    total,
                    pending,
                    approved
                });
            }
            catch (Exception ex)
            {
                // Log error in production: _logger.LogError(ex, "Error getting user stats for: {Username}", User.Identity?.Name);
                return Json(new
                {
                    success = false,
                    message = "Unable to load statistics"
                });
            }
        }
        
        // API: /Idea/ResendValidationEmails
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendValidationEmails(string id)
        {
            try
            {
                var idea = await _ideaService.GetIdeaByIdAsync(id);
                if (idea == null)
                {
                    return Json(new { success = false, message = "Idea not found." });
                }

                var currentUserBadge = User.Identity?.Name ?? "";
                var user = await _authService.AuthenticateAsync(currentUserBadge); // Authenticate to get User object with Role

                var allowedRoles = new List<string> { "R01", "R06", "R07", "R08", "R09", "R10", "R11", "R12" };

                if (user?.Role == null || !allowedRoles.Contains(user.Role.Id))
                {
                    return Json(new { success = false, message = "You don't have permission to resend validation emails." });
                }

                var submitter = await _authService.GetEmployeeByBadgeAsync(idea.InitiatorId);
                var submitterName = submitter?.Name ?? idea.InitiatorId;

                if (_emailSettings.ValidatorEmails?.Any() == true)
                {
                    var emailSent = await _emailService.SendIdeaSubmissionNotificationAsync(
                        idea.IdeaName, submitterName, idea.InitiatorId, idea.Id, _emailSettings.ValidatorEmails);

                    if (emailSent)
                    {
                        _logger.LogInformation("Validation emails resent for idea {IdeaId} by {Username}", idea.Id, currentUserBadge);
                        return Json(new { success = true, message = "Validation emails sent successfully!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Failed to send validation emails." });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "No validator emails configured." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending validation emails for idea {IdeaId}", id);
                return Json(new { success = false, message = "An error occurred while sending emails." });
            }
        }
    }
}
