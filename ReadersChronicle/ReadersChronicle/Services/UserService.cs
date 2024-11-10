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

        public UserService(ApplicationDbContext context, IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<bool> IsUsernameUniqueAsync(string username)
        {
            return !await _context.Users.AnyAsync(u => u.UserName == username);
        }

        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            return !await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task RegisterUserAsync(RegisterViewModel model)
        {
            var passwordHasher = new PasswordHasher<User>();
            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                JoinDate = DateTime.UtcNow,
                PasswordHash = passwordHasher.HashPassword(null, model.Password),
                SecurityQuestion = model.SecurityQuestion,
                SecurityAnswerHash = passwordHasher.HashPassword(null, model.SecurityAnswer)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await CreateUserProfile(model.UserName);
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
                UserID = user.UserID,
                Bio = "This is your profile bio!",
                ImageMimeType = mimeType,
                ImageData = profilePictureData
            };

            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();
        }

        public async Task<string> LoginUser(LoginViewModel model)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserName == model.UserName || u.Email == model.UserName);
            if (user == null) return null;

            var passwordHasher = new PasswordHasher<User>();
            var passwordVerification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if(passwordVerification != PasswordVerificationResult.Success) return null;
            return GenerateJwtToken(user);
        }

        public string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim("userName", user.UserName),
                new Claim("userId", user.UserID.ToString()),
                new Claim("userRole", user.UserType)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.Now.AddHours(5),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
