using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;
using ReadersChronicle.Models;

namespace ReadersChronicle.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); // Throw if context is null
        }
        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Handle login logic here
                // Check if the user exists and redirect accordingly
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create a new User instance
                var user = new User
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    JoinDate = DateTime.UtcNow, // Set join date
                    UserType = "user" // Default user type
                };

                // Hash the password (consider using Identity framework's UserManager)
                var passwordHasher = new PasswordHasher<User>();
                user.Password = passwordHasher.HashPassword(user, model.Password);

                // Save user to the database
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Optionally, you can sign in the user immediately after registration
                // await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Home"); // Redirect to home page or login page
            }

            // If we got this far, something failed; redisplay form
            return View(model);
        }
    }
}