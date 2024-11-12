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

        public UserService(ApplicationDbContext context, IOptions<JwtSettings> jwtSettings, UserManager<User> userManager, IConfiguration configuration)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<bool> IsUsernameUniqueAsync(string username)
        {
            var user = _context.Users.Where(user => user.UserName.Equals(username)).FirstOrDefault(); // Use UserManager to find user by username
            return user == null;
        }

        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            var user = _context.Users.Where(user => user.UserName.Equals(email)).FirstOrDefault();
            return user == null;
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
                UserID = user.Id, // user.Id is a string, which matches IdentityUser
                Bio = "This is your profile bio!",
                ImageMimeType = mimeType,
                ImageData = profilePictureData
            };

            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();
        }

        public async Task<string> LoginUser(LoginViewModel model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);

            if (user == null) return null;

            // You can optionally add more checks here if you want (e.g., account lock, etc.)
            if (await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return GenerateJwtToken(user); // Generate token if the user is valid
            }

            return null;
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
    }
}
