using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Ideku.Services;
using Ideku.Models.Entities;
using Ideku.Models.ViewModels.Idea;
using Ideku.Data.Context;

namespace Ideku.Controllers
{
    [Authorize] // Require authentication untuk semua actions
    public class IdeaController : Controller
    {
        private readonly IdeaService _ideaService;
        private readonly AuthService _authService;
        private readonly FileService _fileService;
        private readonly AppDbContext _context;

        public IdeaController(
            IdeaService ideaService, 
            AuthService authService,
            FileService fileService,
            AppDbContext context)
        {
            _ideaService = ideaService;
            _authService = authService;
            _fileService = fileService;
            _context = context;
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
                // Log error
                TempData["ErrorMessage"] = "Unable to load your ideas.";
                return View(new IdeaIndexViewModel());
            }
        }

        // GET: /Idea/Create - Tampilkan form create idea
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = new IdeaCreateViewModel();
                
                // Load dropdown data dengan relationships
                ViewBag.Divisions = await _context.Divisi.ToListAsync();
                ViewBag.Categories = await _context.Category.ToListAsync();
                ViewBag.Events = await _context.Event.ToListAsync();
                
                // 🔥 NEW: Load Departements dengan Divisi info
                ViewBag.Departements = await _context.Departement
                    .Include(d => d.Divisi)
                    .Select(d => new {
                        Id = d.Id,
                        Name = $"{d.NamaDepartement} ({d.Divisi.NamaDivisi})",
                        DivisiId = d.DivisiId
                    })
                    .ToListAsync();
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
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
                // Validate employee exists
                var employee = await _authService.GetEmployeeByBadgeAsync(model.BadgeNumber);
                if (employee == null)
                {
                    ModelState.AddModelError("BadgeNumber", "Employee with this Badge Number not found.");
                }

                // Validate file size jika ada attachment
                if (model.AttachmentFile != null && model.AttachmentFile.Length > 5 * 1024 * 1024) // 5MB
                {
                    ModelState.AddModelError("AttachmentFile", "File size must be less than 5MB.");
                }

                if (ModelState.IsValid)
                {
                    // Handle file upload
                    string? attachmentFileName = null;
                    if (model.AttachmentFile != null)
                    {
                        attachmentFileName = await _fileService.SaveFileAsync(model.AttachmentFile);
                    }

                    // Create idea entity
                    var idea = new Idea
                    {
                        Initiator = model.BadgeNumber,
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

                    await _ideaService.CreateIdeaAsync(idea);

                    TempData["SuccessMessage"] = "Idea submitted successfully! Your idea is now under review.";
                    return RedirectToAction("Index");
                }

                // If validation failed, reload dropdown data
                ViewBag.Divisions = await _context.Divisi.ToListAsync();
                ViewBag.Categories = await _context.Category.ToListAsync();
                ViewBag.Events = await _context.Event.ToListAsync();
                
                return View(model);
            }
            catch (Exception ex)
            {
                // Log error
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

                // Check if user owns this idea
                var currentUser = User.Identity?.Name ?? "";
                if (idea.Initiator != currentUser)
                {
                    TempData["ErrorMessage"] = "You don't have permission to view this idea.";
                    return RedirectToAction("Index");
                }

                return View(idea);
            }
            catch (Exception ex)
            {
                // Log error
                TempData["ErrorMessage"] = "Unable to load idea details.";
                return RedirectToAction("Index");
            }
        }

        // GET: /Idea/Edit/5 - Tampilkan form edit idea (hanya untuk status Submitted)
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

                // Check permissions
                var currentUser = User.Identity?.Name ?? "";
                if (idea.Initiator != currentUser)
                {
                    TempData["ErrorMessage"] = "You don't have permission to edit this idea.";
                    return RedirectToAction("Index");
                }

                // Only allow edit if status is Submitted
                if (idea.CurrentStatus != "Submitted")
                {
                    TempData["ErrorMessage"] = "You can only edit ideas that haven't been reviewed yet.";
                    return RedirectToAction("Details", new { id = id });
                }

                // Convert to ViewModel
                var viewModel = new IdeaCreateViewModel
                {
                    BadgeNumber = idea.Initiator,
                    Division = idea.Division,
                    Department = idea.Department,
                    Category = idea.CategoryId,
                    Event = idea.EventId,
                    IdeaName = idea.IdeaName,
                    IdeaIssueBackground = idea.IdeaIssueBackground,
                    IdeaSolution = idea.IdeaSolution,
                    SavingCost = idea.SavingCost
                };

                // Load dropdown data
                ViewBag.Divisions = await _context.Divisi.ToListAsync();
                ViewBag.Categories = await _context.Category.ToListAsync();
                ViewBag.Events = await _context.Event.ToListAsync();
                ViewBag.IsEdit = true;
                ViewBag.IdeaId = id;
                ViewBag.CurrentAttachment = idea.AttachmentFile;

                return View("Create", viewModel); // Reuse Create view
            }
            catch (Exception ex)
            {
                // Log error
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

                // Check permissions
                var currentUser = User.Identity?.Name ?? "";
                if (existingIdea.Initiator != currentUser)
                {
                    TempData["ErrorMessage"] = "You don't have permission to edit this idea.";
                    return RedirectToAction("Index");
                }

                // Validate employee exists
                var employee = await _authService.GetEmployeeByBadgeAsync(model.BadgeNumber);
                if (employee == null)
                {
                    ModelState.AddModelError("BadgeNumber", "Employee with this Badge Number not found.");
                }

                if (ModelState.IsValid)
                {
                    // Handle file upload
                    string? attachmentFileName = existingIdea.AttachmentFile;
                    if (model.AttachmentFile != null)
                    {
                        // Delete old file if exists
                        if (!string.IsNullOrEmpty(existingIdea.AttachmentFile))
                        {
                            _fileService.DeleteFile(existingIdea.AttachmentFile);
                        }

                        // Save new file
                        attachmentFileName = await _fileService.SaveFileAsync(model.AttachmentFile);
                    }

                    // Update idea
                    existingIdea.Initiator = model.BadgeNumber;
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

                // If validation failed, reload dropdown data
                ViewBag.Divisions = await _context.Divisi.ToListAsync();
                ViewBag.Categories = await _context.Category.ToListAsync();
                ViewBag.Events = await _context.Event.ToListAsync();
                ViewBag.IsEdit = true;
                ViewBag.IdeaId = id;
                ViewBag.CurrentAttachment = existingIdea.AttachmentFile;
                
                return View("Create", model);
            }
            catch (Exception ex)
            {
                // Log error
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

                // Check permissions
                var currentUser = User.Identity?.Name ?? "";
                if (idea.Initiator != currentUser)
                {
                    TempData["ErrorMessage"] = "You don't have permission to delete this idea.";
                    return RedirectToAction("Index");
                }

                // Only allow delete if status is Submitted
                if (idea.CurrentStatus != "Submitted")
                {
                    TempData["ErrorMessage"] = "You can only delete ideas that haven't been reviewed yet.";
                    return RedirectToAction("Index");
                }

                // Delete attached file if exists
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
                // Log error
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

                // Verify user has access to this idea
                var idea = await _ideaService.GetIdeaByIdAsync(ideaId);
                if (idea == null || idea.AttachmentFile != filename)
                {
                    return NotFound();
                }

                var currentUser = User.Identity?.Name ?? "";
                if (idea.Initiator != currentUser)
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
                var originalFileName = filename.Substring(37); // Remove GUID prefix
                
                return File(fileBytes, "application/octet-stream", originalFileName);
            }
            catch (Exception ex)
            {
                // Log error
                TempData["ErrorMessage"] = "Unable to download file.";
                return RedirectToAction("Index");
            }
        }

        // API endpoint untuk AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                var currentUser = User.Identity?.Name ?? "";
                var (total, pending, approved) = await _ideaService.GetIdeaStatsAsync(currentUser);

                return Json(new { 
                    success = true, 
                    total, 
                    pending, 
                    approved 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = "Unable to load statistics" 
                });
            }
        }
    }
}