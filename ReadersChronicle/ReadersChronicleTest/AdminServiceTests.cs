using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;
using ReadersChronicle.Services;

namespace ReadersChronicleTest
{
    [TestClass]
    public class AdminServiceTests
    {
        private ApplicationDbContext _context;
        private AdminService _adminService;

        [TestInitialize]
        public void Setup()
        {
            // Initialize the in-memory database and the AdminService
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_AdminService")
                .Options;

            _context = new ApplicationDbContext(options);
            _adminService = new AdminService(_context);

            var passwordHasher = new PasswordHasher<User>();
            // Add test users to the database
            var testUser1 = new User
            {
                UserName = "user1",
                Email = "user1@example.com",
                Id = "1",
                UserType = "user",
                PasswordHash = passwordHasher.HashPassword(null, "Password123"),
                isBlocked = false,
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
                isBlocked = true,
                SecurityQuestion = "MotherMaidenName",
                SecurityAnswerHash = passwordHasher.HashPassword(null, "MyMaidenName")
            };

            var testUser3 = new User
            {
                UserName = "admin",
                Email = "admin@example.com",
                Id = "3",
                UserType = "admin",
                PasswordHash = passwordHasher.HashPassword(null, "Password123"),
                isBlocked = false,
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

        // Test for GetAllUsers
        [TestMethod]
        public async Task GetAllUsers_ShouldReturnAllNonAdminUsers()
        {
            // Act
            var users = await _adminService.GetAllUsers();

            // Assert
            Assert.AreEqual(2, users.Count); // Should return 2 users (user1 and user2)
            Assert.IsFalse(users.Any(u => u.UserType == "admin")); // Should not return the admin user
        }

        // Test for SearchUsersAsync
        [TestMethod]
        public async Task SearchUsersAsync_ShouldReturnUsers_WhenUsernameMatches()
        {
            // Act
            var users = await _adminService.SearchUsersAsync("user");

            // Assert
            Assert.AreEqual(2, users.Count); // Should return user1 and user2
            Assert.IsTrue(users.All(u => u.UserName.Contains("user"))); // All returned usernames should contain "user"
        }

        [TestMethod]
        public async Task SearchUsersAsync_ShouldReturnEmpty_WhenNoUsernameMatches()
        {
            // Act
            var users = await _adminService.SearchUsersAsync("nonexistent");

            // Assert
            Assert.AreEqual(0, users.Count); // No user should match the search term "nonexistent"
        }

        // Test for ChangeIsBlockedForUser
        [TestMethod]
        public async Task ChangeIsBlockedForUser_ShouldToggleBlockStatus_WhenUserExists()
        {
            // Arrange
            var username = "user1";
            var initialStatus = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username)).isBlocked;

            // Act
            var result = await _adminService.ChangeIsBlockedForUser(username);
            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

            // Assert
            Assert.IsTrue(result); // Method should return true
            Assert.AreNotEqual(initialStatus, updatedUser.isBlocked); // Block status should be toggled
        }

        [TestMethod]
        public async Task ChangeIsBlockedForUser_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            // Act
            var result = await _adminService.ChangeIsBlockedForUser("nonexistent");

            // Assert
            Assert.IsFalse(result); // Method should return false if the user does not exist
        }
    }
}
