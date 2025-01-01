using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using ReadersChronicle.Data;

namespace ReadersChronicle.Hubs
{
    /// <summary>
    /// SignalR hub for handling real-time messaging between users.
    /// </summary>
    public class MessageHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        /// <summary>
        /// Initializes a new instance of the MessageHub class.
        /// </summary>
        /// <param name="context">The database context used to interact with the application's data.</param>
        /// <param name="userManager">The user manager used for handling user-related operations.</param>
        public MessageHub(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Sends a message from the connected user to the specified receiver in real-time using SignalR.
        /// </summary>
        /// <param name="receiverUserId">The ID of the user receiving the message.</param>
        /// <param name="messageText">The content of the message being sent.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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
