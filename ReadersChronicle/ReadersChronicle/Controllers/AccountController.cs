using Azure.Identity;
using Jose;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReadersChronicle.Data;
using ReadersChronicle.Models;
using ReadersChronicle.Services;
using System.Net.NetworkInformation;

namespace ReadersChronicle.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly UserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(ApplicationDbContext context, IOptions<JwtSettings> jwtSettings, UserManager<User> userManager, UserService userService, SignInManager<User> signInManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); // Throw if context is null
            _jwtSettings = jwtSettings.Value;
            _userService = userService;
            _signInManager = signInManager;
            _userManager = userManager;
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

            // Instead of manually setting cookies, let ASP.NET Core Identity manage the authentication
            var user = await _userManager.FindByNameAsync(model.UserName);
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (result.Succeeded)
            {
                // Redirect the user to the home page or wherever they need to go
                TempData["SuccessMessage"] = "Login successful!";
                return RedirectToAction("Index", "Home");
            }

            // If sign-in failed, add an error to ModelState
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Remove the AuthToken cookie
            Response.Cookies.Delete("AuthToken");

            // Sign out from the ASP.NET Core Identity
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");  // Redirect to the home page after logout
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();  // No need to pass anything for SecurityQuestions
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if the username and email are unique
                if (!await _userService.IsUsernameUniqueAsync(model.UserName))
                {
                    return BadRequest(new { message = "Username is already taken" });
                }

                if (!await _userService.IsEmailUniqueAsync(model.Email))
                {
                    return BadRequest(new { message = "Email is already taken" });
                }

                // Create a new User object with the provided details
                var user = new User
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    SecurityQuestion = model.SecurityQuestion,
                    SecurityAnswerHash = new PasswordHasher<User>().HashPassword(null, model.SecurityAnswer) // Hash the security answer
                };

                // Register the user using UserManager (will hash the password automatically)
                var result = await _userManager.CreateAsync(user, model.Password);

                // Check if the registration succeeded
                if (result.Succeeded)
                {
                    // Sign in the user automatically
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Optionally, create the user profile
                    await _userService.CreateUserProfile(user.UserName);

                    // Redirect to Home page after successful registration
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // If registration failed, add errors to ModelState and return view
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description); // Log the error messages
                    }

                    // You can also log these errors to a log file for better diagnostics.
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error: {error.Description}");
                    }
                }
            }

            // If the model is invalid, return the view with errors
            return View(model);
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return NotFound("User not found");
            }

            var model = new ProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                Bio = user.Profile?.Bio,
                ProfilePicture = user.Profile?.ImageData
            };

            return View(model);
        }


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> GetSecurityQuestion(ForgotPasswordViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return View("ForgotPassword", model);
            }

            var securityQuestion = await _userService.GetSecurityQuestionAsync(model.UserNameOrEmail);
            if(securityQuestion == null)
            {
                ModelState.AddModelError(string.Empty, "User not found");
                return View("ForgotPassword", model);
            }

            TempData["SecurityQuestion"] = securityQuestion;
            TempData["UserNameOrEmail"] = model.UserNameOrEmail;
            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (TempData["SecurityQuestion"] == null)
            {
                return RedirectToAction("ForgotPassword");
            }

            ViewData["SecurityQuestion"] = TempData["SecurityQuestion"];
            TempData.Keep("UserNameOrEmail");

            return View(new ResetPasswordViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["SecurityQuestion"] = TempData["SecurityQuestion"];
                return View(model);
            }

            var userNameOrEmail = TempData["UserNameOrEmail"] as string;
            if (!await _userService.VerifySecurityAnswerAsync(userNameOrEmail, model.SecurityAnswer))
            {
                ModelState.AddModelError(string.Empty, "Incorrect security answer.");
                ViewData["SecurityQuestion"] = TempData["SecurityQuestion"];
                return View(model);
            }

            var success = await _userService.ResetPasswordAsync(userNameOrEmail, model.NewPassword);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Failed to reset password.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Password has been reset successfully.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> EditProfile()
        {
            // TODO: logic to display profile editing
            return View();
        }

        public async Task<IActionResult> ChangePassword()
        {
            // TODO: Logic to change password
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if(user != null)
            {
                await _userManager.DeleteAsync(user);
                await _signInManager.SignOutAsync();
            }
            return RedirectToAction("Index", "Home");
        }
    }
}