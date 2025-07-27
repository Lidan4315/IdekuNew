using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Ideku.Services;
using Ideku.Models.Entities;
using Ideku.Models.ViewModels.Idea;
using Ideku.Data.Context;
using Microsoft.Extensions.Options;

namespace Ideku.Controllers
{
    [Authorize]
    public class IdeaController : Controller
    {
        private readonly IdeaService _ideaService;
        private readonly AuthService _authService;
        private readonly FileService _fileService;
        private readonly EmailService _emailService;
        private readonly AppDbContext _context;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<IdeaController> _logger;

        public IdeaController(
            IdeaService ideaService,
            AuthService authService,
            FileService fileService,
            EmailService emailService,
            AppDbContext context,
            IOptions<EmailSettings> emailSettings,
            ILogger<IdeaController> logger)
        {
            _ideaService = ideaService;
            _authService = authService;
            _fileService = fileService;
            _emailService = emailService;
            _context = context;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        // GET: /Idea/Index - Tampilkan daftar ideas user
        public async Task<IActionResult> Index()
        {
            try
            {
                var currentUser = User.Identity?.Name ?? "";
                var ideas = await _ideaService.GetUserIdeasAsync(currentUser);
                var (total, pending, approved) = await _ideaService.GetIdeaStatsAsync(currentUser);

                var viewModel = new IdeaIndexViewModel
                {
                    Ideas = ideas,
                    CurrentUserName = currentUser,
                    TotalIdeas = total,
                    PendingIdeas = pending,
                    ApprovedIdeas = approved
                };

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
                ViewBag.Divisions = await _context.Divisi.ToListAsync();
                ViewBag.Categories = await _context.Category.ToListAsync();
                ViewBag.Events = await _context.Event.ToListAsync();
                ViewBag.Departements = await _context.Departement
                    .Include(d => d.Divisi)
                    .Select(d => new {
                        Id = d.Id,
                        Name = $"{d.NamaDepartement} ({d.Divisi.NamaDivisi})",
                        DivisiId = d.DivisiId
                    })
                    .ToListAsync();

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
                var employee = await _authService.GetEmployeeByBadgeAsync(model.BadgeNumber);
                if (employee == null)
                {
                    ModelState.AddModelError("BadgeNumber", "Employee with this Badge Number not found.");
                }

                if (model.AttachmentFile != null && model.AttachmentFile.Length > 5 * 1024 * 1024) // 5MB
                {
                    ModelState.AddModelError("AttachmentFile", "File size must be less than 5MB.");
                }

                if (ModelState.IsValid)
                {
                    string? attachmentFileName = null;
                    if (model.AttachmentFile != null)
                    {
                        attachmentFileName = await _fileService.SaveFileAsync(model.AttachmentFile);
                    }

                    var idea = new Idea
                    {
                        InitiatorId = model.BadgeNumber,
                        Division = model.Division,
                        Department = model.Department,
                        CategoryId = model.Category,
                        EventId = model.Event,
                        IdeaName = model.IdeaName,
                        IdeaIssueBackground = model.IdeaIssueBackground,
                        IdeaSolution = model.IdeaSolution,
                        SavingCost = model.SavingCost,
                        AttachmentFile = attachmentFileName
                    };

                    var createdIdea = await _ideaService.CreateIdeaAsync(idea);

                    try
                    {
                        if (_emailSettings.ValidatorEmails?.Any() == true)
                        {
                            var submitterName = employee?.Name ?? model.BadgeNumber;
                            var emailSent = await _emailService.SendIdeaSubmissionNotificationAsync(
                                createdIdea.IdeaName, submitterName, model.BadgeNumber, createdIdea.Id, _emailSettings.ValidatorEmails);

                            if (emailSent)
                            {
                                _logger.LogInformation("Validation emails sent for idea {IdeaId}", createdIdea.Id);
                                createdIdea.CurrentStatus = "Under Review";
                                await _ideaService.UpdateIdeaAsync(createdIdea);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to send validation emails for idea {IdeaId}", createdIdea.Id);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No validator emails configured");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Error sending validation emails for idea {IdeaId}", createdIdea.Id);
                    }

                    TempData["SuccessMessage"] = "Idea submitted successfully! Validation emails have been sent to reviewers.";
                    return RedirectToAction("Index");
                }

                // Reload ViewBag data if validation fails
                ViewBag.Divisions = await _context.Divisi.ToListAsync();
                ViewBag.Categories = await _context.Category.ToListAsync();
                ViewBag.Events = await _context.Event.ToListAsync();
                ViewBag.Departements = await _context.Departement
                    .Include(d => d.Divisi)
                    .Select(d => new { 
                        Id = d.Id, 
                        Name = $"{d.NamaDepartement} ({d.Divisi.NamaDivisi})", 
                        DivisiId = d.DivisiId 
                    })
                    .ToListAsync();
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating idea");
                TempData["ErrorMessage"] = "An error occurred while submitting your idea.";
                return RedirectToAction("Index");
            }
        }

        // GET: /Idea/Details/5 - Tampilkan detail idea
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var idea = await _ideaService.GetIdeaByIdAsync(id);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found.";
                    return RedirectToAction("Index");
                }

                // Check if user owns this idea or is a validator/admin
                var currentUserBadge = User.Identity?.Name ?? "";
                var user = await _authService.AuthenticateAsync(currentUserBadge); // Authenticate to get User object with Role

                bool hasAccess = idea.InitiatorId == currentUserBadge ||
                                 (user?.Role != null && (user.Role.RoleName == "Manager" || user.Role.RoleName == "SuperAdmin"));

                if (!hasAccess)
                {
                    TempData["ErrorMessage"] = "You don't have permission to view this idea.";
                    return RedirectToAction("Index");
                }

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
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var idea = await _ideaService.GetIdeaByIdAsync(id);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found.";
                    return RedirectToAction("Index");
                }

                var currentUser = User.Identity?.Name ?? "";
                if (idea.InitiatorId != currentUser)
                {
                    TempData["ErrorMessage"] = "You don't have permission to edit this idea.";
                    return RedirectToAction("Index");
                }

                if (idea.CurrentStatus != "Submitted")
                {
                    TempData["ErrorMessage"] = "You can only edit ideas that haven't been reviewed yet.";
                    return RedirectToAction("Details", new { id = id });
                }

                var viewModel = new IdeaCreateViewModel
                {
                    BadgeNumber = idea.InitiatorId,
                    Division = idea.Division,
                    Department = idea.Department,
                    Category = idea.CategoryId,
                    Event = idea.EventId,
                    IdeaName = idea.IdeaName,
                    IdeaIssueBackground = idea.IdeaIssueBackground,
                    IdeaSolution = idea.IdeaSolution,
                    SavingCost = idea.SavingCost
                };

                ViewBag.Divisions = await _context.Divisi.ToListAsync();
                ViewBag.Categories = await _context.Category.ToListAsync();
                ViewBag.Events = await _context.Event.ToListAsync();
                ViewBag.Departements = await _context.Departement
                    .Include(d => d.Divisi)
                    .Select(d => new {
                        Id = d.Id,
                        Name = $"{d.NamaDepartement} ({d.Divisi.NamaDivisi})",
                        DivisiId = d.DivisiId
                    })
                    .ToListAsync();
                
                ViewBag.IsEdit = true;
                ViewBag.IdeaId = id;
                ViewBag.CurrentAttachment = idea.AttachmentFile;

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
        public async Task<IActionResult> Edit(int id, IdeaCreateViewModel model)
        {
            try
            {
                var existingIdea = await _ideaService.GetIdeaByIdAsync(id);
                if (existingIdea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found.";
                    return RedirectToAction("Index");
                }

                var currentUser = User.Identity?.Name ?? "";
                if (existingIdea.InitiatorId != currentUser)
                {
                    TempData["ErrorMessage"] = "You don't have permission to edit this idea.";
                    return RedirectToAction("Index");
                }

                var employee = await _authService.GetEmployeeByBadgeAsync(model.BadgeNumber);
                if (employee == null)
                {
                    ModelState.AddModelError("BadgeNumber", "Employee with this Badge Number not found.");
                }

                if (ModelState.IsValid)
                {
                    string? attachmentFileName = existingIdea.AttachmentFile;
                    if (model.AttachmentFile != null)
                    {
                        if (!string.IsNullOrEmpty(existingIdea.AttachmentFile))
                        {
                            _fileService.DeleteFile(existingIdea.AttachmentFile);
                        }
                        attachmentFileName = await _fileService.SaveFileAsync(model.AttachmentFile);
                    }

                    existingIdea.InitiatorId = model.BadgeNumber;
                    existingIdea.Division = model.Division;
                    existingIdea.Department = model.Department;
                    existingIdea.CategoryId = model.Category;
                    existingIdea.EventId = model.Event;
                    existingIdea.IdeaName = model.IdeaName;
                    existingIdea.IdeaIssueBackground = model.IdeaIssueBackground;
                    existingIdea.IdeaSolution = model.IdeaSolution;
                    existingIdea.SavingCost = model.SavingCost;
                    existingIdea.AttachmentFile = attachmentFileName;

                    await _ideaService.UpdateIdeaAsync(existingIdea);

                    TempData["SuccessMessage"] = "Idea updated successfully!";
                    return RedirectToAction("Details", new { id = id });
                }

                // Reload ViewBag data if validation fails
                ViewBag.Divisions = await _context.Divisi.ToListAsync();
                ViewBag.Categories = await _context.Category.ToListAsync();
                ViewBag.Events = await _context.Event.ToListAsync();
                ViewBag.Departements = await _context.Departement
                    .Include(d => d.Divisi)
                    .Select(d => new {
                        Id = d.Id,
                        Name = $"{d.NamaDepartement} ({d.Divisi.NamaDivisi})",
                        DivisiId = d.DivisiId
                    })
                    .ToListAsync();
                
                ViewBag.IsEdit = true;
                ViewBag.IdeaId = id;
                ViewBag.CurrentAttachment = existingIdea.AttachmentFile;
                
                return View("Create", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating idea {IdeaId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating your idea.";
                return RedirectToAction("Index");
            }
        }

        // POST: /Idea/Delete/5 - Delete idea
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var idea = await _ideaService.GetIdeaByIdAsync(id);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found.";
                    return RedirectToAction("Index");
                }

                var currentUser = User.Identity?.Name ?? "";
                if (idea.InitiatorId != currentUser)
                {
                    TempData["ErrorMessage"] = "You don't have permission to delete this idea.";
                    return RedirectToAction("Index");
                }

                if (idea.CurrentStatus != "Submitted")
                {
                    TempData["ErrorMessage"] = "You can only delete ideas that haven't been reviewed yet.";
                    return RedirectToAction("Index");
                }

                if (!string.IsNullOrEmpty(idea.AttachmentFile))
                {
                    _fileService.DeleteFile(idea.AttachmentFile);
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

        // GET: /Idea/Download/filename - Download attachment
        [HttpGet]
        public async Task<IActionResult> Download(string filename, int ideaId)
        {
            try
            {
                if (string.IsNullOrEmpty(filename))
                {
                    return NotFound();
                }

                var idea = await _ideaService.GetIdeaByIdAsync(ideaId);
                if (idea == null || idea.AttachmentFile != filename)
                {
                    return NotFound();
                }

                var currentUserBadge = User.Identity?.Name ?? "";
                var user = await _authService.AuthenticateAsync(currentUserBadge); // Authenticate to get User object with Role

                // Check permissions: user is owner, manager, or superadmin
                bool hasAccess = idea.InitiatorId == currentUserBadge ||
                                 (user?.Role != null && (user.Role.RoleName == "Manager" || user.Role.RoleName == "SuperAdmin"));

                if (!hasAccess)
                {
                    return Forbid();
                }

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

        // API: /Idea/GetUserStats
        [HttpGet]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                var currentUser = User.Identity?.Name ?? "";
                var (total, pending, approved) = await _ideaService.GetIdeaStatsAsync(currentUser);
                return Json(new { success = true, total, pending, approved });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user stats for {Username}", User.Identity?.Name);
                return Json(new { success = false, message = "Unable to load statistics" });
            }
        }

        // API: /Idea/ResendValidationEmails
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendValidationEmails(int id)
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

                if (user?.Role == null || (user.Role.RoleName != "Manager" && user.Role.RoleName != "SuperAdmin"))
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
