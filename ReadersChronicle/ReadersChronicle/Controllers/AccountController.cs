using Jose;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReadersChronicle.Data;
using ReadersChronicle.Models;
using ReadersChronicle.Services;

namespace ReadersChronicle.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly UserService _userService;

        public AccountController(ApplicationDbContext context, IOptions<JwtSettings> jwtSettings, UserService userService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); // Throw if context is null
            _jwtSettings = jwtSettings.Value;
            _userService = userService;
        }
        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var token = await _userService.LoginUser(model);

            if (token == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            // TODO! storing the token in a cookie or session
            TempData["SuccessMessage"] = "Login successful!";
            TempData["Token"] = token; // Storing the token in TempData or other storage for client usage

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!await _userService.IsUsernameUniqueAsync(model.UserName))
            {
                ModelState.AddModelError("UserName", "Username is already taken.");
                return View(model);
            }

            if (!await _userService.IsEmailUniqueAsync(model.Email))
            {
                ModelState.AddModelError("Email", "Email is already taken.");
                return View(model);
            }

            await _userService.RegisterUserAsync(model);
            ViewData["SuccessMessage"] = "Registration successful! Please log in.";

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword()
        {
            
        }

        [HttpGet]
        public async Task<IActionResult> GetSecurityQuestion()
        {

        }

    }
}