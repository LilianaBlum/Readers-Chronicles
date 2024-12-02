using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ReadersChronicle.Data;
using ReadersChronicle.Models;
using ReadersChronicle.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ReadersChronicle.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<User> _signInManager;

        public UserService(ApplicationDbContext context, IOptions<JwtSettings> jwtSettings, UserManager<User> userManager, IConfiguration configuration, SignInManager<User> signInManager)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _userManager = userManager;
            _configuration = configuration;
            _signInManager = signInManager;
        }

        public async Task<bool> IsUsernameUniqueAsync(string username)
        {
            var user = _context.Users.Where(user => user.UserName.Equals(username)).FirstOrDefault();
            return user == null;
        }

        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            var user = _context.Users.Where(user => user.UserName.Equals(email)).FirstOrDefault();
            return user == null;
        }


        public async Task<(bool IsSuccess, string Message)> LoginAsync(LoginViewModel model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);

            if (user == null)
            {
                return (false, "Invalid username or password.");
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (result.Succeeded)
            {
                var token = GenerateJwtToken(user);
                return (true, token);
            }

            return (false, "Invalid username or password.");
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
            new Claim("userName", user.UserName),
            new Claim("userId", user.Id.ToString()),
            new Claim("userRole", user.UserType)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(5),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<(bool IsSuccess, string Message)> RegisterAsync(RegisterViewModel model)
        {
            if (!await IsUsernameUniqueAsync(model.UserName))
            {
                return (false, "Username is already taken.");
            }

            if (!await IsEmailUniqueAsync(model.Email))
            {
                return (false, "Email is already taken.");
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
                await CreateUserProfile(user.UserName);
                return (true, "Registration successful.");
            }

            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, errors);
        }


        public async Task CreateUserProfile(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null) return;

            var profilePicturePath = Path.Combine("wwwroot", "Common", "profile-picture.png");
            byte[] profilePictureData = await File.ReadAllBytesAsync(profilePicturePath);
            string mimeType = "image/png";

            var profile = new Profile
            {
                UserID = user.Id,
                Bio = "This is your profile bio!",
                ImageMimeType = mimeType,
                ImageData = profilePictureData
            };

            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();
        }

        public async Task<string> GetSecurityQuestionAsync(string userNameOrEmail)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == userNameOrEmail || u.Email == userNameOrEmail);
            return user?.SecurityQuestion;
        }

        public async Task<bool> VerifySecurityAnswerAsync(string userNameOrEmail, string answer)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == userNameOrEmail || u.Email == userNameOrEmail);

            if (user == null) return false;

            var passwordHasher = new PasswordHasher<User>();
            return passwordHasher.VerifyHashedPassword(null, user.SecurityAnswerHash, answer) == PasswordVerificationResult.Success;
        }

        public async Task<bool> ResetPasswordAsync(string userNameOrEmail, string newPassword)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == userNameOrEmail || u.Email == userNameOrEmail);

            if (user == null) return false;

            var passwordHasher = new PasswordHasher<User>();
            user.PasswordHash = passwordHasher.HashPassword(null, newPassword);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<(bool IsSuccess, string Message)> EditUserProfileAsync(ClaimsPrincipal userPrincipal, EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                return (false, "User not found. Please log in again.");
            }

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserID == user.Id);
            if (profile == null)
            {
                return (false, "Profile not found. Please create a profile first.");
            }

            user.UserName = model.UserName;
            user.Email = model.Email;
            profile.Bio = model.Bio;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return (false, $"Error updating user: {errors}");
            }

            await _context.SaveChangesAsync();
            return (true, "Profile updated successfully.");
        }

        public async Task<(bool IsSuccess, string Message)> ChangeUserProfileImageAsync(ClaimsPrincipal userPrincipal, IFormFile profileImage)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                return (false, "User not found. Please log in again.");
            }

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserID == user.Id);
            if (profile == null)
            {
                profile = new Profile { UserID = user.Id };
                _context.Profiles.Add(profile);
            }

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await profileImage.CopyToAsync(memoryStream);
                    profile.ImageData = memoryStream.ToArray();
                    profile.ImageMimeType = profileImage.ContentType;
                }

                await _context.SaveChangesAsync();
                return (true, "Profile image updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred while updating the profile image: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, string Message)> DeleteUserAccountAsync(ClaimsPrincipal userPrincipal, string password)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                return (false, "User not found. Please log in again.");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
            {
                return (false, "Incorrect password.");
            }

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserID == user.Id);
            if (profile != null)
            {
                _context.Profiles.Remove(profile);
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return (false, "Error deleting user account.");
            }

            await _signInManager.SignOutAsync();

            return (true, "Your profile has been deleted successfully.");
        }
    }
}
