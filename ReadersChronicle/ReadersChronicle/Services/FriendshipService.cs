using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;

namespace ReadersChronicle.Services
{
    public class FriendshipService
    {
        private readonly ApplicationDbContext _context;

        public FriendshipService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> SearchUsersAsync(string usernameQuery, string currentUserId)
        {
            return await _context.Users
                .Where(u => u.UserType != "admin" && u.Id != currentUserId)
                .Where(u => u.UserName.Contains(usernameQuery))
                .ToListAsync();
        }

        public async Task<List<User>> GetAllUsersAsync(string currentUserId)
        {
            return await _context.Users
                .Where(u => u.UserType != "admin" && u.Id != currentUserId)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> SendFriendRequestAsync(string initiatorUserId, string approvingUserId)
        {
            var existingRequest = await _context.PendingFriendships
        .FirstOrDefaultAsync(pf => pf.InitiatorUserID == initiatorUserId && pf.ApprovingUserID == approvingUserId || pf.InitiatorUserID == approvingUserId && pf.ApprovingUserID == initiatorUserId);

            if (existingRequest != null)
            {
                return (false, "You already have a pending friendship request with this user.");
            }

            var existingFriendship = await _context.Friendships
        .FirstOrDefaultAsync(f =>
            (f.User1ID == initiatorUserId && f.User2ID == approvingUserId) ||
            (f.User1ID == approvingUserId && f.User2ID == initiatorUserId));

            if (existingFriendship != null)
            {
                return (false, "This user is already your friend.");
            }

            var pending = new PendingFriendship
            {
                InitiatorUserID = initiatorUserId,
                ApprovingUserID = approvingUserId
            };

            _context.PendingFriendships.Add(pending);
            await _context.SaveChangesAsync();

            return (true, "Friendship request sent successfully.");
        }

        public async Task<List<PendingFriendship>> GetSentInvitesAsync(string userId)
        {
            return await _context.PendingFriendships
                .Include(pf => pf.ApprovingUser)
                .Where(pf => pf.InitiatorUserID == userId)
                .ToListAsync();
        }

        public async Task<List<PendingFriendship>> GetReceivedInvitesAsync(string userId)
        {
            return await _context.PendingFriendships
                .Include(pf => pf.InitiatorUser)
                .Where(pf => pf.ApprovingUserID == userId)
                .ToListAsync();
        }

        public async Task CancelSentInviteAsync(int friendshipId, string userId)
        {
            var pending = await _context.PendingFriendships
                .FirstOrDefaultAsync(pf => pf.FriendshipID == friendshipId && pf.InitiatorUserID == userId);

            if (pending != null)
            {
                _context.PendingFriendships.Remove(pending);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ApproveFriendRequestAsync(int friendshipId, string userId)
        {
            var pending = await _context.PendingFriendships
                .FirstOrDefaultAsync(pf => pf.FriendshipID == friendshipId && pf.ApprovingUserID == userId);

            if (pending != null)
            {
                var friendship = new Friendship
                {
                    User1ID = pending.InitiatorUserID,
                    User2ID = pending.ApprovingUserID
                };

                _context.Friendships.Add(friendship);
                _context.PendingFriendships.Remove(pending);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFriendAsync(string currentUserId, string friendId)
        {
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.User1ID == currentUserId && f.User2ID == friendId) ||
                    (f.User1ID == friendId && f.User2ID == currentUserId));

            if (friendship != null)
            {
                _context.Friendships.Remove(friendship);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DenyFriendRequestAsync(int friendshipId, string userId)
        {
            var pending = await _context.PendingFriendships
                .FirstOrDefaultAsync(pf => pf.FriendshipID == friendshipId && pf.ApprovingUserID == userId);

            if (pending != null)
            {
                _context.PendingFriendships.Remove(pending);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<User>> GetFriendsAsync(string userId)
        {
            var friendsAsUser1 = await _context.Friendships
                .Include(f => f.User2)
                .Where(f => f.User1ID == userId)
                .Select(f => f.User2)
                .ToListAsync();

            var friendsAsUser2 = await _context.Friendships
                .Include(f => f.User1)
                .Where(f => f.User2ID == userId)
                .Select(f => f.User1)
                .ToListAsync();

            return friendsAsUser1.Concat(friendsAsUser2).ToList();
        }
    }
}
