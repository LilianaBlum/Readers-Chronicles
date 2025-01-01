using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using ReadersChronicle.Data;
using ReadersChronicle.Services;

namespace ReadersChronicleTest
{
    [TestClass]
    public class FriendshipServiceTests
    {
        private ApplicationDbContext _context;
        private FriendshipService _friendshipService;
        private UserManager<User> _userManager;
        private SignInManager<User> _signInManager;

        [TestInitialize]
        public void Setup()
        {
            // Initialize the in-memory database and the FriendshipService
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_FriendshipService")
                .Options;

            _context = new ApplicationDbContext(options);

            var mockUserStore = new Mock<IUserStore<User>>();
            var mockUserManager = new Mock<UserManager<User>>(
                mockUserStore.Object,
                null,
                new PasswordHasher<User>(),
                null,
                null,
                null,
                null,
                null,
                null
            );

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockUserClaimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<User>>();

            var mockSignInManager = new Mock<SignInManager<User>>(
                mockUserManager.Object,
                mockHttpContextAccessor.Object,
                mockUserClaimsPrincipalFactory.Object,
                null,
                null,
                null,
                null
            );

            _userManager = mockUserManager.Object;
            _signInManager = mockSignInManager.Object;

            _friendshipService = new FriendshipService(_context);

            // Add test users to the database
            var passwordHasher = new PasswordHasher<User>();
            var testUser1 = new User
            {
                UserName = "user1",
                Email = "user1@example.com",
                Id = "1",
                UserType = "user",
                PasswordHash = passwordHasher.HashPassword(null, "Password123"),
                SecurityQuestion = "MotherMaidenName",
                SecurityAnswerHash = passwordHasher.HashPassword(null, "MyMaidenName")
            };

            var testUser2 = new User
            {
                UserName = "user2",
                Email = "user2@example.com",
                Id = "2",
                UserType = "user",
                PasswordHash = passwordHasher.HashPassword(null, "Password123"),
                SecurityQuestion = "MotherMaidenName",
                SecurityAnswerHash = passwordHasher.HashPassword(null, "MyMaidenName")
            };

            var testUser3 = new User
            {
                UserName = "adminUser",
                Email = "admin@example.com",
                Id = "3",
                UserType = "admin",
                PasswordHash = passwordHasher.HashPassword(null, "Password123"),
                SecurityQuestion = "MotherMaidenName",
                SecurityAnswerHash = passwordHasher.HashPassword(null, "MyMaidenName")
            };

            _context.Users.AddRange(testUser1, testUser2, testUser3);
            _context.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up the in-memory database after each test
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task SearchUsersAsync_ShouldReturnUsers_WhenMatchingUsername()
        {
            // Arrange
            var currentUserId = "1"; // user1
            var searchQuery = "user";

            // Act
            var result = await _friendshipService.SearchUsersAsync(searchQuery, currentUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count); // Only user2 should be returned, because user1 is the current user and admin is excluded
            Assert.AreEqual("user2", result[0].UserName);
        }

        [TestMethod]
        public async Task SearchUsersAsync_ShouldReturnNoUsers_WhenNoMatch()
        {
            // Arrange
            var currentUserId = "1"; // user1
            var searchQuery = "nonexistent";

            // Act
            var result = await _friendshipService.SearchUsersAsync(searchQuery, currentUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count); // No user should match the query "nonexistent"
        }

        [TestMethod]
        public async Task GetAllUsersAsync_ShouldReturnAllNonAdminUsers()
        {
            // Arrange
            var currentUserId = "1"; // user1

            // Act
            var result = await _friendshipService.GetAllUsersAsync(currentUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count); // Only user2 should be returned, because admin and current user are excluded
            Assert.IsTrue(result.Any(u => u.UserName == "user2"));
        }

        [TestMethod]
        public async Task GetAllUsersAsync_ShouldNotReturnCurrentUser()
        {
            // Arrange
            var currentUserId = "2"; // user2

            // Act
            var result = await _friendshipService.GetAllUsersAsync(currentUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count); // Only user1 should be returned, because user2 is excluded
            Assert.IsTrue(result.Any(u => u.UserName == "user1"));
        }

        [TestMethod]
        public async Task SendFriendRequestAsync_ShouldSendRequest_WhenNoExistingRequestOrFriendship()
        {
            // Arrange
            var initiatorUserId = "1"; // user1
            var approvingUserId = "2"; // user2

            // Act
            var result = await _friendshipService.SendFriendRequestAsync(initiatorUserId, approvingUserId);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual("Friendship request sent successfully.", result.Message);

            var pendingRequest = await _context.PendingFriendships
                .FirstOrDefaultAsync(pf => pf.InitiatorUserID == initiatorUserId && pf.ApprovingUserID == approvingUserId);
            Assert.IsNotNull(pendingRequest); // Ensure the request is added to the PendingFriendships table
        }

        [TestMethod]
        public async Task SendFriendRequestAsync_ShouldReturnError_WhenRequestAlreadyPending()
        {
            // Arrange
            var initiatorUserId = "1"; // user1
            var approvingUserId = "2"; // user2

            // Add a pending friendship request between the two users
            var pendingRequest = new PendingFriendship
            {
                InitiatorUserID = initiatorUserId,
                ApprovingUserID = approvingUserId
            };
            _context.PendingFriendships.Add(pendingRequest);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendshipService.SendFriendRequestAsync(initiatorUserId, approvingUserId);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("You already have a pending friendship request with this user.", result.Message);
        }

        [TestMethod]
        public async Task SendFriendRequestAsync_ShouldReturnError_WhenAlreadyFriends()
        {
            // Arrange
            var initiatorUserId = "1"; // user1
            var approvingUserId = "2"; // user2

            // Create an existing friendship between the two users
            var existingFriendship = new Friendship
            {
                User1ID = initiatorUserId,
                User2ID = approvingUserId
            };
            _context.Friendships.Add(existingFriendship);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendshipService.SendFriendRequestAsync(initiatorUserId, approvingUserId);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("This user is already your friend.", result.Message);
        }

        [TestMethod]
        public async Task GetSentInvitesAsync_ShouldReturnSentInvites_WhenUserHasSentInvites()
        {
            // Arrange
            var initiatorUserId = "1"; // user1
            var approvingUserId = "2"; // user2

            // Add a pending friendship request where user1 is the initiator and user2 is the approver
            var pendingRequest = new PendingFriendship
            {
                InitiatorUserID = initiatorUserId,
                ApprovingUserID = approvingUserId
            };
            _context.PendingFriendships.Add(pendingRequest);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendshipService.GetSentInvitesAsync(initiatorUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(initiatorUserId, result[0].InitiatorUserID);
            Assert.AreEqual(approvingUserId, result[0].ApprovingUserID);
        }

        [TestMethod]
        public async Task GetSentInvitesAsync_ShouldReturnEmptyList_WhenUserHasNotSentAnyInvites()
        {
            // Arrange
            var initiatorUserId = "1"; // user1

            // Act
            var result = await _friendshipService.GetSentInvitesAsync(initiatorUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count); // No sent invites
        }

        // Test for GetReceivedInvitesAsync
        [TestMethod]
        public async Task GetReceivedInvitesAsync_ShouldReturnReceivedInvites_WhenUserHasReceivedInvites()
        {
            // Arrange
            var initiatorUserId = "1"; // user1
            var approvingUserId = "2"; // user2

            // Add a pending friendship request where user2 is the initiator and user1 is the approver
            var pendingRequest = new PendingFriendship
            {
                InitiatorUserID = initiatorUserId,
                ApprovingUserID = approvingUserId
            };
            _context.PendingFriendships.Add(pendingRequest);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendshipService.GetReceivedInvitesAsync(approvingUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(initiatorUserId, result[0].InitiatorUserID);
            Assert.AreEqual(approvingUserId, result[0].ApprovingUserID);
        }

        [TestMethod]
        public async Task GetReceivedInvitesAsync_ShouldReturnEmptyList_WhenUserHasNotReceivedAnyInvites()
        {
            // Arrange
            var approvingUserId = "2"; // user2

            // Act
            var result = await _friendshipService.GetReceivedInvitesAsync(approvingUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count); // No received invites
        }

        [TestMethod]
        public async Task CancelSentInviteAsync_ShouldRemovePendingFriendship_WhenRequestExists()
        {
            // Arrange
            var userId = "1"; // user1
            var approvingUserId = "2"; // user2
            var pendingRequest = new PendingFriendship
            {
                InitiatorUserID = userId,
                ApprovingUserID = approvingUserId
            };

            _context.PendingFriendships.Add(pendingRequest);
            await _context.SaveChangesAsync();

            // Act
            await _friendshipService.CancelSentInviteAsync(pendingRequest.FriendshipID, userId);

            // Assert
            var pendingInvite = await _context.PendingFriendships
                .FirstOrDefaultAsync(pf => pf.FriendshipID == pendingRequest.FriendshipID);
            Assert.IsNull(pendingInvite); // The pending invite should be removed
        }

        [TestMethod]
        public async Task CancelSentInviteAsync_ShouldDoNothing_WhenRequestDoesNotExist()
        {
            // Arrange
            var userId = "1"; // user1
            var friendshipId = 999; // Non-existing friendship ID

            // Act
            await _friendshipService.CancelSentInviteAsync(friendshipId, userId);

            // Assert
            var pendingInvite = await _context.PendingFriendships
                .FirstOrDefaultAsync(pf => pf.FriendshipID == friendshipId);
            Assert.IsNull(pendingInvite); // No pending invite should exist
        }

        // Test for ApproveFriendRequestAsync
        [TestMethod]
        public async Task ApproveFriendRequestAsync_ShouldCreateFriendship_WhenRequestExists()
        {
            // Arrange
            var initiatorUserId = "1"; // user1
            var approvingUserId = "2"; // user2
            var pendingRequest = new PendingFriendship
            {
                InitiatorUserID = initiatorUserId,
                ApprovingUserID = approvingUserId
            };

            _context.PendingFriendships.Add(pendingRequest);
            await _context.SaveChangesAsync();

            // Act
            await _friendshipService.ApproveFriendRequestAsync(pendingRequest.FriendshipID, approvingUserId);

            // Assert: Check if the friendship is created
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.User1ID == initiatorUserId && f.User2ID == approvingUserId) ||
                    (f.User1ID == approvingUserId && f.User2ID == initiatorUserId));

            Assert.IsNotNull(friendship); // Friendship should exist
            Assert.AreEqual(initiatorUserId, friendship.User1ID); // user1 should be in the friendship
            Assert.AreEqual(approvingUserId, friendship.User2ID); // user2 should be in the friendship
        }

        [TestMethod]
        public async Task ApproveFriendRequestAsync_ShouldDoNothing_WhenRequestDoesNotExist()
        {
            // Arrange
            var approvingUserId = "2"; // user2
            var friendshipId = 999; // Non-existing friendship ID

            // Act
            await _friendshipService.ApproveFriendRequestAsync(friendshipId, approvingUserId);

            // Assert: Check if no friendship is created
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.FriendshipID == friendshipId);
            Assert.IsNull(friendship); // No friendship should exist
        }

        [TestMethod]
        public async Task RemoveFriendAsync_ShouldRemoveFriendship_WhenFriendshipExists()
        {
            // Arrange
            var userId = "1"; // user1
            var friendId = "2"; // user2
            var friendship = new Friendship
            {
                User1ID = userId,
                User2ID = friendId
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            // Act
            await _friendshipService.RemoveFriendAsync(userId, friendId);

            // Assert
            var removedFriendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.User1ID == userId && f.User2ID == friendId) ||
                    (f.User1ID == friendId && f.User2ID == userId));
            Assert.IsNull(removedFriendship); // Friendship should be removed
        }

        [TestMethod]
        public async Task RemoveFriendAsync_ShouldDoNothing_WhenFriendshipDoesNotExist()
        {
            // Arrange
            var userId = "1"; // user1
            var friendId = "2"; // user2 (no friendship exists)

            // Act
            await _friendshipService.RemoveFriendAsync(userId, friendId);

            // Assert
            var removedFriendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.User1ID == userId && f.User2ID == friendId) ||
                    (f.User1ID == friendId && f.User2ID == userId));
            Assert.IsNull(removedFriendship); // No friendship should exist
        }

        // Test for DenyFriendRequestAsync
        [TestMethod]
        public async Task DenyFriendRequestAsync_ShouldRemovePendingRequest_WhenRequestExists()
        {
            // Arrange
            var userId = "1"; // user1
            var friendId = "2"; // user2
            var pendingRequest = new PendingFriendship
            {
                InitiatorUserID = userId,
                ApprovingUserID = friendId
            };

            _context.PendingFriendships.Add(pendingRequest);
            await _context.SaveChangesAsync();

            // Act
            await _friendshipService.DenyFriendRequestAsync(pendingRequest.FriendshipID, friendId);

            // Assert
            var removedRequest = await _context.PendingFriendships
                .FirstOrDefaultAsync(pf => pf.FriendshipID == pendingRequest.FriendshipID);
            Assert.IsNull(removedRequest); // Pending request should be removed
        }

        [TestMethod]
        public async Task DenyFriendRequestAsync_ShouldDoNothing_WhenRequestDoesNotExist()
        {
            // Arrange
            var userId = "1"; // user1
            var friendshipId = 999; // Non-existing friendship ID

            // Act
            await _friendshipService.DenyFriendRequestAsync(friendshipId, userId);

            // Assert
            var removedRequest = await _context.PendingFriendships
                .FirstOrDefaultAsync(pf => pf.FriendshipID == friendshipId);
            Assert.IsNull(removedRequest); // No request should exist
        }

        // Test for GetFriendsAsync
        [TestMethod]
        public async Task GetFriendsAsync_ShouldReturnListOfFriends_WhenFriendsExist()
        {
            // Arrange
            var userId = "1"; // user1
            var friendId = "2"; // user2

            var friendship = new Friendship
            {
                User1ID = userId,
                User2ID = friendId
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            // Act
            var friends = await _friendshipService.GetFriendsAsync(userId);

            // Assert
            Assert.AreEqual(1, friends.Count); // There should be 1 friend
            Assert.AreEqual(friendId, friends[0].Id); // user2 should be in the list of friends
        }

        [TestMethod]
        public async Task GetFriendsAsync_ShouldReturnEmptyList_WhenNoFriendsExist()
        {
            // Arrange
            var userId = "1"; // user1 (no friends)

            // Act
            var friends = await _friendshipService.GetFriendsAsync(userId);

            // Assert
            Assert.AreEqual(0, friends.Count); // There should be no friends
        }
    }
}