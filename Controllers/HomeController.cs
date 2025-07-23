using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Ideku.Models;
using Ideku.Services;

namespace Ideku.Controllers
{
    [Authorize] // Require authentication untuk semua actions
    public class HomeController : Controller
    {
        private readonly IdeaService _ideaService;

        public HomeController(IdeaService ideaService)
        {
            _ideaService = ideaService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get current user
                var currentUser = User.Identity?.Name ?? "";
                
                // Get user's idea statistics
                var (total, pending, approved) = await _ideaService.GetIdeaStatsAsync(currentUser);

                // Pass data ke View via ViewBag
                ViewBag.TotalIdeas = total;
                ViewBag.PendingIdeas = pending;
                ViewBag.ApprovedIdeas = approved;
                ViewBag.CurrentUser = currentUser;

                return View();
            }
            catch (Exception ex)
            {
                // Log error
                TempData["ErrorMessage"] = "Unable to load dashboard data.";
                ViewBag.TotalIdeas = 0;
                ViewBag.PendingIdeas = 0;
                ViewBag.ApprovedIdeas = 0;
                
                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> QuickStats()
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