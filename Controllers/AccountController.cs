using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Ideku.Services;
using Ideku.Models.ViewModels.Account;

namespace Ideku.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;

        public AccountController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Jika user sudah login, redirect ke Home
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Authenticate user melalui AuthService
                var user = await _authService.AuthenticateAsync(model.Username);

                if (user != null)
                {
                    // Create claims untuk user yang berhasil login
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim("FullName", user.Name),
                        new Claim("EmployeeId", user.EmployeeId),
                        new Claim(ClaimTypes.Role, user.Role.RoleName)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    // Sign in user dengan cookie
                    await HttpContext.SignInAsync("MyCookieAuth", claimsPrincipal);

                    TempData["SuccessMessage"] = $"Welcome back, {user.Name}!";
                    return RedirectToAction("Index", "Home");
                }

                // Login gagal
                ModelState.AddModelError("", "Invalid username. Please check and try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                // Log error (dalam implementasi nyata)
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync("MyCookieAuth");
                TempData["SuccessMessage"] = "You have been logged out successfully.";
            }
            catch (Exception ex)
            {
                // Log error
                TempData["ErrorMessage"] = "An error occurred during logout.";
            }

            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}