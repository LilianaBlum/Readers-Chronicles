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
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _jwtSettings = jwtSettings.Value;
            _userService = userService;
            _signInManager = signInManager;
            _userManager = userManager;
        }
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

            var user = await _userManager.FindByNameAsync(model.UserName);
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Login successful!";
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("AuthToken");

            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (!await _userService.IsUsernameUniqueAsync(model.UserName))
                {
                    return BadRequest(new { message = "Username is already taken" });
                }

                if (!await _userService.IsEmailUniqueAsync(model.Email))
                {
                    return BadRequest(new { message = "Email is already taken" });
                }

                var user = new User
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    SecurityQuestion = model.SecurityQuestion,
                    SecurityAnswerHash = new PasswordHasher<User>().HashPassword(null, model.SecurityAnswer)
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    await _userService.CreateUserProfile(user.UserName);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error: {error.Description}");
                    }
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserID == user.Id);
            if (profile == null)
            {
                return RedirectToAction("CreateProfile");
            }

            var model = new ProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                Bio = profile.Bio,
                ProfileImage = profile.ImageData != null
                    ? $"data:{profile.ImageMimeType};base64,{Convert.ToBase64String(profile.ImageData)}"
                    : null
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

            TempData.Keep("UserNameOrEmail");

            ViewData["SecurityQuestion"] = TempData["SecurityQuestion"];
            TempData.Keep("SecurityQuestion");

            var model = new ResetPasswordViewModel
            {
                UserNameOrEmail = TempData["UserNameOrEmail"] as string
            };

            return View(model);
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

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserID == user.Id);
            if (profile == null)
            {
                return RedirectToAction("CreateProfile");
            }

            var model = new EditProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                Bio = profile.Bio,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserID == user.Id);
                if (profile == null)
                {
                    return RedirectToAction("CreateProfile");
                }

                user.UserName = model.UserName;
                user.Email = model.Email;
                profile.Bio = model.Bio;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeProfileImage(IFormFile ProfileImage)
        {
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserID == user.Id);
                if (profile == null)
                {
                    profile = new Profile
                    {
                        UserID = user.Id
                    };
                    _context.Profiles.Add(profile);
                }

                using (var memoryStream = new MemoryStream())
                {
                    await ProfileImage.CopyToAsync(memoryStream);
                    profile.ImageData = memoryStream.ToArray();
                    profile.ImageMimeType = ProfileImage.ContentType;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profile image updated successfully!";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError(string.Empty, "Please select a valid profile image.");
            return RedirectToAction("Profile");
        }


        public async Task<IActionResult> ChangePassword()
        {
            return View();
        }

        [HttpGet]
        public IActionResult DeleteProfileConfirmation()
        {
            return View(new DeleteProfileConfirmationViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProfileConfirmation(DeleteProfileConfirmationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Profile");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
            {
                ModelState.AddModelError(string.Empty, "Incorrect password.");
                return View(model);
            }

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserID == user.Id);
            if (profile != null)
            {
                _context.Profiles.Remove(profile);
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Error deleting user account.";
                return RedirectToAction("Profile");
            }

            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "Your profile has been deleted successfully.";
            return RedirectToAction("Index", "Home");
        }
    }
}