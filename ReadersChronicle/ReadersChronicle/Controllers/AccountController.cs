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

            var (isSuccess, message) = await _userService.LoginAsync(model);

        if (isSuccess)
        {
            TempData["SuccessMessage"] = "Login successful!";
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, message);
        return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _userService.LogoutAsync();
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (isSuccess, message) = await _userService.RegisterAsync(model);

            if (isSuccess)
            {
                TempData["SuccessMessage"] = "Registration successful!";
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, message);
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (isSuccess, message) = await _userService.EditUserProfileAsync(User, model);

            if (isSuccess)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeProfileImage(IFormFile ProfileImage)
        {
            if (ProfileImage == null || ProfileImage.Length <= 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a valid profile image.");
                return RedirectToAction("Profile");
            }

            var (isSuccess, message) = await _userService.ChangeUserProfileImageAsync(User, ProfileImage);

            if (isSuccess)
            {
                TempData["SuccessMessage"] = "Profile image updated successfully!";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError(string.Empty, message);
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

            var (isSuccess, message) = await _userService.DeleteUserAccountAsync(User, model.Password);

            if (isSuccess)
            {
                TempData["SuccessMessage"] = "Your profile has been deleted successfully.";
                return RedirectToAction("Index", "Home");
            }

            TempData["ErrorMessage"] = message;
            return RedirectToAction("Profile");
        }
    }
}