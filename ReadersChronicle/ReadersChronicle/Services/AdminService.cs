using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;

namespace ReadersChronicle.Services
{
    public class AdminService
    {
        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllUsers()
        {
            var users = await _context.Users
                .Where(f => f.UserType != "admin")
                .ToListAsync();

            return users;
        }

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
