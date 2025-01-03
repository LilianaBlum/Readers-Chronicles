using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;
using ReadersChronicle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadersChronicleTest
{
    [TestClass]
    public class MessagingServiceTests
    {
        private MessagingService _messagingService;
        private ApplicationDbContext _context;
        private DbContextOptions<ApplicationDbContext> _dbContextOptions;

        [TestInitialize]
        public void Setup()
        {
            // Set up in-memory database options
            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            // Initialize the in-memory context
            _context = new ApplicationDbContext(_dbContextOptions);

            // Initialize MessagingService
            _messagingService = new MessagingService(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up in-memory database after each test
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task GetMessagesAsync_ShouldReturnMessages_WhenMessagesExistBetweenUsers()
        {
            // Arrange
            var userId = "user1";
            var friendId = "user2";
            var message1 = new Message
            {
                SenderID = userId,
                ReceiverID = friendId,
                Content = "Hello, how are you?",
                Timestamp = DateTime.Now.AddMinutes(-5)
            };
            var message2 = new Message
            {
                SenderID = friendId,
                ReceiverID = userId,
                Content = "I'm good, thanks!",
                Timestamp = DateTime.Now
            };
            _context.Messages.Add(message1);
            _context.Messages.Add(message2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _messagingService.GetMessagesAsync(userId, friendId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("Hello, how are you?", result.First().Content);
            Assert.AreEqual("I'm good, thanks!", result.Last().Content);
        }

        [TestMethod]
        public async Task GetMessagesAsync_ShouldReturnEmptyList_WhenNoMessagesExistBetweenUsers()
        {
            // Arrange
            var userId = "user1";
            var friendId = "user2";

            // Act
            var result = await _messagingService.GetMessagesAsync(userId, friendId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task SaveMessageAsync_ShouldAddMessageToDatabase_WhenMessageIsSaved()
        {
            // Arrange
            var userId = "user1";
            var friendId = "user2";
            var message = new Message
            {
                SenderID = userId,
                ReceiverID = friendId,
                Content = "This is a new message",
                Timestamp = DateTime.Now
            };

            // Act
            await _messagingService.SaveMessageAsync(message);

            // Assert
            var savedMessage = await _context.Messages.FindAsync(message.MessageID);
            Assert.IsNotNull(savedMessage);
            Assert.AreEqual(message.Content, savedMessage.Content);
            Assert.AreEqual(userId, savedMessage.SenderID);
            Assert.AreEqual(friendId, savedMessage.ReceiverID);
        }

        [TestMethod]
        public async Task SaveMessageAsync_ShouldSaveMessageToDatabase_WhenMessageIsValid()
        {
            // Arrange
            var message = new Message
            {
                SenderID = "user1",
                ReceiverID = "user2",
                Content = "Hello!",
                Timestamp = DateTime.Now
            };

            // Act
            await _messagingService.SaveMessageAsync(message);

            // Assert
            var savedMessage = await _context.Messages.FirstOrDefaultAsync(m => m.Content == "Hello!");
            Assert.IsNotNull(savedMessage);
            Assert.AreEqual("Hello!", savedMessage.Content);
            Assert.AreEqual("user1", savedMessage.SenderID);
            Assert.AreEqual("user2", savedMessage.ReceiverID);
        }
    }
}
