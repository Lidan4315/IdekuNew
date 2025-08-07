// Controllers/HomeController.cs (Updated for new schema)
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Ideku.Models;
using Ideku.Services;
using Ideku.Helpers;

namespace Ideku.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IdeaService _ideaService;
        private readonly AuthService _authService;
        private readonly IdeaDisplayHelper _ideaDisplayHelper;

        public HomeController(IdeaService ideaService, AuthService authService, IdeaDisplayHelper ideaDisplayHelper)
        {
            _ideaService = ideaService;
            _authService = authService;
            _ideaDisplayHelper = ideaDisplayHelper;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get global statistics for dashboard
                var (total, pending, approved) = await _ideaService.GetGlobalIdeaStatsAsync();

                // Pass data to View via ViewBag
                ViewBag.TotalIdeas = total;
                ViewBag.PendingIdeas = pending;
                ViewBag.ApprovedIdeas = approved;
                ViewBag.CurrentUser = User.Identity?.Name ?? "User";

                return View();
            }
            catch (Exception ex)
            {
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