using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;

namespace ReadersChronicle.Services
{
    /// <summary>
    /// Service class that handles messaging functionalities between users.
    /// </summary>
    public class MessagingService
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the MessagingService class.
        /// </summary>
        /// <param name="context">The database context used to interact with the application's data.</param>
        public MessagingService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all messages exchanged between two users, ordered by timestamp.
        /// </summary>
        /// <param name="userId">The ID of the first user.</param>
        /// <param name="friendId">The ID of the second user (friend).</param>
        /// <returns>A list of Message objects representing the messages exchanged between the two users.</returns>
        public async Task<IEnumerable<Message>> GetMessagesAsync(string userId, string friendId)
        {
            return await _context.Messages
                .Where(m => (m.SenderID == userId && m.ReceiverID == friendId) ||
                            (m.SenderID == friendId && m.ReceiverID == userId))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Saves a new message to the database.
        /// </summary>
        /// <param name="message">The Message object containing the message details.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SaveMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }
    }

}
