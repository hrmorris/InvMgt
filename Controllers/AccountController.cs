using Microsoft.AspNetCore.Mvc;
using InvoiceManagement.Services;
using InvoiceManagement.Models;

namespace InvoiceManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IAdminService _adminService;

        public AccountController(IAuthService authService, IAdminService adminService)
        {
            _authService = authService;
            _adminService = adminService;
        }

        // GET: Account/Login
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Username and password are required.");
                return View();
            }

            var user = await _authService.AuthenticateAsync(username, password);
            
            if (user != null)
            {
                // Set session
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Role", user.Role);

                // Log login
                await _adminService.LogActionAsync(user.Id, user.Username, "Login", "User", user.Id, $"{user.FullName} logged in");

                TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid username or password.");
            return View();
        }

        // GET: Account/Logout
        public async Task<IActionResult> Logout()
        {
            var username = HttpContext.Session.GetString("Username");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId.HasValue && !string.IsNullOrEmpty(username))
            {
                await _adminService.LogActionAsync(userId, username, "Logout", "User", userId, $"{username} logged out");
            }

            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction(nameof(Login));
        }

        // GET: Account/FirstTimeSetup (Only if no users exist)
        public async Task<IActionResult> FirstTimeSetup()
        {
            var users = await _adminService.GetAllUsersAsync();
            if (users.Any())
            {
                TempData["ErrorMessage"] = "System already has users. Contact an administrator.";
                return RedirectToAction(nameof(Login));
            }
            
            return View(new User());
        }

        // POST: Account/FirstTimeSetup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FirstTimeSetup(User user, string password)
        {
            var users = await _adminService.GetAllUsersAsync();
            if (users.Any())
            {
                TempData["ErrorMessage"] = "System already has users. Contact an administrator.";
                return RedirectToAction(nameof(Login));
            }

            ModelState.Remove("PasswordHash");
            ModelState.Remove("LastLoginDate");

            if (ModelState.IsValid)
            {
                // Force admin role for first user
                user.Role = "Admin";
                user.Status = "Active";
                await _adminService.CreateUserAsync(user, password);
                
                TempData["SuccessMessage"] = "First admin account created successfully! Please login.";
                return RedirectToAction(nameof(Login));
            }

            return View(user);
        }

        // GET: Account/AccessDenied
        public IActionResult AccessDenied()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");
            
            ViewBag.Username = username;
            ViewBag.Role = role;
            
            return View();
        }
    }
}

