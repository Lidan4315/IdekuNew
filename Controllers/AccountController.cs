// Controllers/AccountController.cs (Minor updates for new schema)
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
                var user = await _authService.AuthenticateAsync(model.Username);

                if (user?.Employee != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim("FullName", user.Employee.Name),
                        new Claim("EmployeeId", user.EmployeeId),
                        new Claim("UserId", user.Id.ToString()),
                        new Claim(ClaimTypes.Role, user.Role.RoleName)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await HttpContext.SignInAsync("MyCookieAuth", claimsPrincipal);

                    TempData["SuccessMessage"] = $"Welcome back, {user.Employee.Name}!";
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid username. Please check and try again.");
                return View(model);
            }
            catch (Exception ex)
            {
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