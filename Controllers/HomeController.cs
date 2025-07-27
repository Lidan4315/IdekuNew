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
        private readonly AuthService _authService;

        public HomeController(IdeaService ideaService, AuthService authService)
        {
            _ideaService = ideaService;
            _authService = authService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // 🔥 FIX: Mengambil statistik global untuk semua ide
                var (total, pending, approved) = await _ideaService.GetGlobalIdeaStatsAsync();

                // Pass data ke View via ViewBag
                ViewBag.TotalIdeas = total;
                ViewBag.PendingIdeas = pending;
                ViewBag.ApprovedIdeas = approved;
                ViewBag.CurrentUser = User.Identity?.Name ?? "User";

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
                // 🔥 FIX: Mengambil statistik global untuk semua ide
                var (total, pending, approved) = await _ideaService.GetGlobalIdeaStatsAsync();

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
