using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;
using ReadersChronicle.Models;
using System.Security.Claims;

namespace ReadersChronicle.Services
{
    /// <summary>
    /// Provides services related to user account management, including registration, login, profile management, and password reset.
    /// </summary>
    public class UserService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public UserService(ApplicationDbContext context, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Checks if a username is unique within the system.
        /// </summary>
        /// <param name="username">The username to check.</param>
        /// <returns>True if the username is unique, false otherwise.</returns>
        public async Task<bool> IsUsernameUniqueAsync(string username)
        {
            var user = _context.Users.Where(user => user.UserName.Equals(username)).FirstOrDefault();
            return user == null;
        }

        /// <summary>
        /// Checks if an email is unique within the system.
        /// </summary>
        /// <param name="email">The email to check.</param>
        /// <returns>True if the email is unique, false otherwise.</returns>
        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            var user = _context.Users.Where(user => user.Email.Equals(email)).FirstOrDefault();
            return user == null;
        }

        /// <summary>
        /// Checks if a username is unique for profile updates, excluding the current username.
        /// </summary>
        /// <param name="username">The username to check.</param>
        /// <param name="currentUsername">The current username to exclude from the check.</param>
        /// <returns>True if the username is unique for profile updates, false otherwise.</returns>
        public async Task<bool> IsUsernameUniqueForProfileUpdateAsync(string username, string currentUsername)
        {
            var user = await _context.Users
                .Where(u => u.UserName.Equals(username) && u.UserName != currentUsername)
                .FirstOrDefaultAsync();

            return user == null;
        }

        /// <summary>
        /// Checks if an email is unique for profile updates, excluding the current email.
        /// </summary>
        /// <param name="email">The email to check.</param>
        /// <param name="currentEmail">The current email to exclude from the check.</param>
        /// <returns>True if the email is unique for profile updates, false otherwise.</returns>
        public async Task<bool> IsEmailUniqueForProfileUpdateAsync(string email, string currentEmail)
        {
            var user = await _context.Users
                .Where(u => u.Email.Equals(email) && u.Email != currentEmail)
                .FirstOrDefaultAsync();

            return user == null;
        }

        /// <summary>
        /// Logs in a user with their username and password.
        /// </summary>
        /// <param name="model">The login model containing username and password.</param>
        /// <returns>A tuple containing a success flag and a message indicating the login result.</returns>
        public async Task<(bool IsSuccess, string Message)> LoginAsync(LoginViewModel model)
        {
            var user = _context.Users.Where(user => user.UserName.Equals(model.UserName)).FirstOrDefault();

            if (user == null)
            {
                return (false, "Invalid username or password.");
            }

            if(user.isBlocked == true)
            {
                return (false, "You are blocked");
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (result.Succeeded)
            {
                return (true, "Login was succesfull!");
            }

            return (false, "Invalid username or password.");
        }

        /// <summary>
        /// Logs out the currently authenticated user.
        /// </summary>
        /// <returns>Task representing the asynchronous operation.</returns>
        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        /// <summary>
        /// Registers a new user with the provided registration details.
        /// </summary>
        /// <param name="model">The registration model containing user details.</param>
        /// <returns>A tuple containing a success flag and a message indicating the registration result.</returns>
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

        /// <summary>
        /// Creates a default profile for a new user after registration.
        /// </summary>
        /// <param name="username">The username of the user to create a profile for.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Retrieves the security question associated with a user account, identified by username or email.
        /// </summary>
        /// <param name="userNameOrEmail">The username or email of the user.</param>
        /// <returns>The security question for the specified user, or null if not found.</returns>
        public async Task<string> GetSecurityQuestionAsync(string userNameOrEmail)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == userNameOrEmail || u.Email == userNameOrEmail);
            return user?.SecurityQuestion;
        }

        /// <summary>
        /// Verifies the security answer provided by the user.
        /// </summary>
        /// <param name="userNameOrEmail">The username or email of the user.</param>
        /// <param name="answer">The answer to verify.</param>
        /// <returns>True if the security answer is correct, false otherwise.</returns>
        public async Task<bool> VerifySecurityAnswerAsync(string userNameOrEmail, string answer)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == userNameOrEmail || u.Email == userNameOrEmail);

            if (user == null) return false;

            var passwordHasher = new PasswordHasher<User>();
            return passwordHasher.VerifyHashedPassword(null, user.SecurityAnswerHash, answer) == PasswordVerificationResult.Success;
        }

        /// <summary>
        /// Resets the password for a user identified by username or email.
        /// </summary>
        /// <param name="userNameOrEmail">The username or email of the user.</param>
        /// <param name="newPassword">The new password to set.</param>
        /// <returns>True if the password reset was successful, false otherwise.</returns>
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

        /// <summary>
        /// Changes the password for a user.
        /// </summary>
        /// <param name="model">The model containing the old and new passwords.</param>
        /// <param name="user">The user whose password is being changed.</param>
        /// <returns>True if the password change was successful, false otherwise.</returns>
        public async Task<bool> ChangePassword(ChangePasswordViewModel model, User user)
        {
            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (result.Succeeded)
            {
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the profile information for the currently authenticated user.
        /// </summary>
        /// <param name="userPrincipal">The claims principal representing the current user.</param>
        /// <param name="model">The model containing the updated profile information.</param>
        /// <returns>A tuple containing a success flag and a message indicating the profile update result.</returns>
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

            bool isUsernameUnique = await IsUsernameUniqueForProfileUpdateAsync(model.UserName, user.UserName);
            bool isEmailUnique = await IsEmailUniqueForProfileUpdateAsync(model.Email, user.Email);

            if (!isUsernameUnique)
            {
                return (false, "Username is already taken.");
            }

            if (!isEmailUnique)
            {
                return (false, "Email is already taken.");
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

        /// <summary>
        /// Updates the profile image for the currently authenticated user.
        /// </summary>
        /// <param name="userPrincipal">The claims principal representing the current user.</param>
        /// <param name="profileImage">The new profile image to set.</param>
        /// <returns>A tuple containing a success flag and a message indicating the profile image update result.</returns>
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

        /// <summary>
        /// Deletes the user account for the currently authenticated user.
        /// </summary>
        /// <param name="userPrincipal">The claims principal representing the current user.</param>
        /// <param name="password">The password to verify before deleting the account.</param>
        /// <returns>A tuple containing a success flag and a message indicating the account deletion result.</returns>
        public async Task<(bool IsSuccess, string Message)> DeleteUserAccountAsync(ClaimsPrincipal userPrincipal, string password)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                return (false, "User not found. Please log in again.");
            }

            var passwordHasher = new PasswordHasher<User>();
            var passwordValid = passwordHasher.VerifyHashedPassword(null, user.PasswordHash, password) == PasswordVerificationResult.Success;

           // var passwordValid = await _userManager.CheckPasswordAsync(user, password);
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
