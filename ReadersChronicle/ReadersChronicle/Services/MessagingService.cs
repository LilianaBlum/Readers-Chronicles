using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;

namespace ReadersChronicle.Services
{
    public class MessagingService
    {
        private readonly ApplicationDbContext _context;

        public MessagingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(string userId, string friendId)
        {
            return await _context.Messages
                .Where(m => (m.SenderID == userId && m.ReceiverID == friendId) ||
                            (m.SenderID == friendId && m.ReceiverID == userId))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task SaveMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }
    }

}
