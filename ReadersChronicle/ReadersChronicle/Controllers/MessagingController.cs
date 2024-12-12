using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;
using ReadersChronicle.Models;
using ReadersChronicle.Services;

namespace ReadersChronicle.Controllers
{
    [Authorize]
    public class MessagingController : Controller
    {
        private readonly MessagingService _messagingService;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public MessagingController(MessagingService messagingService, UserManager<User> userManager, ApplicationDbContext context)
        {
            _messagingService = messagingService;
            _userManager = userManager;
            _context = context;
        }

        public IActionResult OpenChat(string userId)
        {
            return RedirectToAction("Chat", new { userId });
        }

        public async Task<IActionResult> Chat(string userId)
        {
             var currentUserId = _userManager.GetUserId(User);

            // Fetch messages between the two users
            var messages = await _context.Messages
                .Where(m => (m.SenderID == currentUserId && m.ReceiverID == userId) ||
                            (m.SenderID == userId && m.ReceiverID == currentUserId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new MessageViewModel
                {
                    SenderUserId = m.SenderID,
                    ReceiverUserId = m.ReceiverID,
                    Text = m.Content,
                    SentAt = m.Timestamp,
                    SenderUserName = _context.Users
                        .Where(u => u.Id == m.SenderID)
                        .Select(u => u.UserName)
                        .FirstOrDefault() // Fetch the username of the sender
                })
                .ToListAsync();
            var receiverUser = await _userManager.FindByIdAsync(userId);

            var viewModel = new ChatViewModel
            {
                CurrentUserId = currentUserId,
                ReceiverUserId = userId,
                ReceiverUserName = receiverUser.UserName,
                Messages = messages
            };

            return View(viewModel);
        }

    }

}
