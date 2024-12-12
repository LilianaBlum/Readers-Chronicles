using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;
using ReadersChronicle.Services;

namespace ReadersChronicle.Hubs
{
    public class MessageHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public MessageHub(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task SendMessage(string receiverUserId, string messageText)
        {
            var senderUser = Context.User;
            var senderUserId = _userManager.GetUserId(senderUser);

            if (string.IsNullOrWhiteSpace(messageText))
            {
                return; // Don't send empty messages
            }

            var senderUserName = senderUser.Identity.Name; // Get sender's username

            var message = new Message
            {
                SenderID = senderUserId,
                ReceiverID = receiverUserId,
                Content = messageText,
                Timestamp = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Broadcast to receiver and sender
            await Clients.User(receiverUserId).SendAsync("ReceiveMessage", senderUserId, senderUserName, messageText, message.Timestamp);
            await Clients.User(senderUserId).SendAsync("ReceiveMessage", senderUserId, senderUserName, messageText, message.Timestamp);
        }

    }

}
