using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;

namespace ReadersChronicle.Services
{
    /// <summary>
    /// Service class that provides administrative functionalities for managing users.
    /// </summary>
    public class AdminService
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the AdminService class.
        /// </summary>
        /// <param name="context">The database context used to interact with the application's data.</param>
        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of all users, excluding those with an "admin" user type.
        /// </summary>
        /// <returns>A list of User objects representing the users in the system.</returns>
        public async Task<List<User>> GetAllUsers()
        {
            var users = await _context.Users
                .Where(f => f.UserType != "admin")
                .ToListAsync();

            return users;
        }

        /// <summary>
        /// Searches for users by their username, excluding those with an "admin" user type.
        /// </summary>
        /// <param name="username">The partial or full username to search for.</param>
        /// <returns>A list of User objects that match the search criteria.</returns>
        public async Task<List<User>> SearchUsersAsync(string username)
        {
            return await _context.Users
                .Where(u => u.UserType != "admin" && EF.Functions.Like(u.UserName, $"%{username}%"))
                .ToListAsync();
        }

        /// <summary>
        /// Toggles the "isBlocked" status for a user, effectively blocking or unblocking them.
        /// </summary>
        /// <param name="username">The username of the user whose block status is being changed.</param>
        /// <returns>True if the block status was successfully toggled; otherwise, false if the user was not found.</returns>
        public async Task<bool> ChangeIsBlockedForUser(string username)
        {
            var user = await _context.Users.Where(u => u.UserName == username).FirstOrDefaultAsync();

            if (user == null)
            {
                return false;
            }

            user.isBlocked = !user.isBlocked;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
