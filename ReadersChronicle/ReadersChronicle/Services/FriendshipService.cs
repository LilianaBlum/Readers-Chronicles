using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;

namespace ReadersChronicle.Services
{
    /// <summary>
    /// Service class responsible for managing friendship-related operations, such as sending requests, searching for users, and managing pending friendships.
    /// </summary>
    public class FriendshipService
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="FriendshipService"/> class.
        /// </summary>
        /// <param name="context">The database context used for querying user data.</param>
        public FriendshipService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Searches for users based on a given username query, excluding the current user and admins.
        /// </summary>
        /// <param name="usernameQuery">The username query to search for.</param>
        /// <param name="currentUserId">The ID of the current user (to exclude from search).</param>
        /// <returns>A list of users matching the search criteria.</returns>
        public async Task<List<User>> SearchUsersAsync(string usernameQuery, string currentUserId)
        {
            return await _context.Users
                .Where(u => u.UserType != "admin" && u.Id != currentUserId)
                .Where(u => u.UserName.Contains(usernameQuery))
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves all users, excluding the current user and admins.
        /// </summary>
        /// <param name="currentUserId">The ID of the current user (to exclude from the list).</param>
        /// <returns>A list of all users excluding the current user and admins.</returns>
        public async Task<List<User>> GetAllUsersAsync(string currentUserId)
        {
            return await _context.Users
                .Where(u => u.UserType != "admin" && u.Id != currentUserId)
                .ToListAsync();
        }

        /// <summary>
        /// Sends a friendship request from one user to another, ensuring there are no pending requests or existing friendships.
        /// </summary>
        /// <param name="initiatorUserId">The ID of the user initiating the request.</param>
        /// <param name="approvingUserId">The ID of the user receiving the request.</param>
        /// <returns>A tuple indicating whether the operation was successful and a message providing additional information.</returns>
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

        /// <summary>
        /// Retrieves a list of pending friendship requests that the user has sent to others.
        /// </summary>
        /// <param name="userId">The ID of the user whose sent invites are to be fetched.</param>
        /// <returns>A list of pending friendship requests initiated by the user.</returns>
        public async Task<List<PendingFriendship>> GetSentInvitesAsync(string userId)
        {
            return await _context.PendingFriendships
                .Include(pf => pf.ApprovingUser)
                .Where(pf => pf.InitiatorUserID == userId)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a list of pending friendship requests that the user has received from others.
        /// </summary>
        /// <param name="userId">The ID of the user whose received invites are to be fetched.</param>
        /// <returns>A list of pending friendship requests received by the user.</returns>
        public async Task<List<PendingFriendship>> GetReceivedInvitesAsync(string userId)
        {
            return await _context.PendingFriendships
                .Include(pf => pf.InitiatorUser)
                .Where(pf => pf.ApprovingUserID == userId)
                .ToListAsync();
        }

        /// <summary>
        /// Cancels a sent friendship invite that has not been accepted, removing the pending request from the database.
        /// </summary>
        /// <param name="friendshipId">The ID of the pending friendship request to cancel.</param>
        /// <param name="userId">The ID of the user who initiated the invite and wishes to cancel it.</param>
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

        /// <summary>
        /// Approves a pending friendship request and converts it into a friendship.
        /// </summary>
        /// <param name="friendshipId">The ID of the pending friendship request to approve.</param>
        /// <param name="userId">The ID of the user who is approving the request.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Removes an existing friendship between the current user and another user.
        /// </summary>
        /// <param name="currentUserId">The ID of the current user who wants to remove the friendship.</param>
        /// <param name="friendId">The ID of the friend to be removed from the friendship.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Denies a pending friendship request, removing it from the list of pending requests.
        /// </summary>
        /// <param name="friendshipId">The ID of the pending friendship request to deny.</param>
        /// <param name="userId">The ID of the user who is denying the request.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Retrieves a list of all friends of a specified user.
        /// </summary>
        /// <param name="userId">The ID of the user whose friends are to be retrieved.</param>
        /// <returns>A list of users who are friends with the specified user.</returns>
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
