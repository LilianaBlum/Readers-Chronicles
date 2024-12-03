using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Models;
using ReadersChronicle.Services;
using ReadersChronicle.Data;
using ReadersChronicle.Settings;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace ReadersChronicleTest
{
    [TestClass]
    public class UserServiceTests
    {
        private ApplicationDbContext _context;
        private UserService _userService;
        private UserManager<User> _userManager;
        private SignInManager<User> _signInManager;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_LoginAsync")
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

            var mockJwtSettings = new JwtSettings { Key = "TestKey", Issuer = "TestIssuer", Audience = "TestAudience" };
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("aD$3j8Jg7%0zD4xT^#WqK3eRz9VmXcT5");
            mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

            _userService = new UserService(
                _context,
                Options.Create(mockJwtSettings),
                _userManager,
                mockConfiguration.Object,
                _signInManager
            );

            var passwordHasher = new PasswordHasher<User>();
            var testUser = new User
            {
                UserName = "existingUser",
                Email = "existing@example.com",
                PasswordHash = passwordHasher.HashPassword(null, "Password123"),
                SecurityQuestion = "MotherMaidenName",
                SecurityAnswerHash = passwordHasher.HashPassword(null, "MyMaidenName")
            };

            testUser.PasswordHash = passwordHasher.HashPassword(testUser, "Password123");
            _context.Users.Add(testUser);
            _context.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task IsUsernameUniqueAsync_ShouldReturnTrue_WhenUsernameIsUnique()
        {
            // Act
            var result = await _userService.IsUsernameUniqueAsync("newUser");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task IsUsernameUniqueAsync_ShouldReturnFalse_WhenUsernameExists()
        {
            // Act
            var result = await _userService.IsUsernameUniqueAsync("existingUser");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task IsEmailUniqueAsync_ShouldReturnTrue_WhenEmailIsUnique()
        {
            // Act
            var result = await _userService.IsEmailUniqueAsync("unique@example.com");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task IsEmailUniqueAsync_ShouldReturnFalse_WhenEmailExists()
        {
            // Act
            var result = await _userService.IsEmailUniqueAsync("existing@example.com");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task LoginAsync_ReturnsSuccess_WhenCredentialsAreValid()
        {
            // Arrange
            var loginViewModel = new LoginViewModel
            {
                UserName = "existingUser",
                Password = "Password123"
            };

            var mockSignInResult = SignInResult.Success;
            var mockSignInManager = Mock.Get(_signInManager);
            mockSignInManager
                .Setup(s => s.PasswordSignInAsync(It.IsAny<User>(), loginViewModel.Password, false, false))
                .ReturnsAsync(mockSignInResult);

            // Act
            var result = await _userService.LoginAsync(loginViewModel);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Message);
        }

        [TestMethod]
        public async Task LoginAsync_ReturnsFailure_WhenUserNotFound()
        {
            // Arrange
            var loginViewModel = new LoginViewModel
            {
                UserName = "nonExistentUser",
                Password = "Password123"
            };

            // Act
            var result = await _userService.LoginAsync(loginViewModel);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Invalid username or password.", result.Message);
        }

        [TestMethod]
        public async Task LoginAsync_ReturnsFailure_WhenPasswordIsInvalid()
        {
            // Arrange
            var loginViewModel = new LoginViewModel
            {
                UserName = "existingUser",
                Password = "WrongPassword"
            };

            var mockSignInResult = SignInResult.Failed;
            var mockSignInManager = Mock.Get(_signInManager);
            mockSignInManager
                .Setup(s => s.PasswordSignInAsync(It.IsAny<User>(), loginViewModel.Password, false, false))
                .ReturnsAsync(mockSignInResult);

            // Act
            var result = await _userService.LoginAsync(loginViewModel);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Invalid username or password.", result.Message);
        }

        [TestMethod]
        public async Task LogoutAsync_CallsSignOutAsync()
        {
            // Arrange
            var mockSignInManager = Mock.Get(_signInManager);
            mockSignInManager
                .Setup(s => s.SignOutAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _userService.LogoutAsync();

            // Assert
            mockSignInManager.Verify(s => s.SignOutAsync(), Times.Once, "SignOutAsync should be called exactly once.");
        }

        [TestMethod]
        public async Task RegisterAsync_ReturnsFailure_WhenUsernameIsNotUnique()
        {
            // Arrange
            _context.Users.Add(new User { 
                UserName = "existingUser", 
                Email = "test@example.com", 
                SecurityQuestion = "Favorite teacher",
                SecurityAnswerHash = "Mr. Smith"
            });
            _context.SaveChanges();

            var registerViewModel = new RegisterViewModel
            {
                UserName = "existingUser",
                Email = "newuser@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                SecurityQuestion = "Favorite teacher",
                SecurityAnswer = "Mr. Smith"
            };

            // Act
            var result = await _userService.RegisterAsync(registerViewModel);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Username is already taken.", result.Message);
        }

        [TestMethod]
        public async Task RegisterAsync_ReturnsFailure_WhenEmailIsNotUnique()
        {
            // Arrange
            _context.Users.Add(new User { UserName = "uniqueUser", Email = "test@example.com",SecurityQuestion = "Favorite teacher", SecurityAnswerHash = "Mr. Smith"
            });
            _context.SaveChanges();

            var registerViewModel = new RegisterViewModel
            {
                UserName = "newUser",
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                SecurityQuestion = "Favorite teacher",
                SecurityAnswer = "Mr. Smith"
            };

            // Act
            var result = await _userService.RegisterAsync(registerViewModel);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Email is already taken.", result.Message);
        }

        [TestMethod]
        public async Task RegisterAsync_ReturnsSuccess_WhenAllDataIsValid()
        {
            // Arrange
            var registerViewModel = new RegisterViewModel
            {
                UserName = "newUser",
                Email = "newuser@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                SecurityQuestion = "Favorite teacher",
                SecurityAnswer = "Mr. Smith"
            };

            var mockCreateResult = IdentityResult.Success;
            Mock.Get(_userManager)
                .Setup(um => um.CreateAsync(It.IsAny<User>(), registerViewModel.Password))
                .ReturnsAsync(mockCreateResult);

            Mock.Get(_signInManager)
                .Setup(sm => sm.SignInAsync(It.IsAny<User>(), false, null))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _userService.RegisterAsync(registerViewModel);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("Registration successful.", result.Message);
        }

        [TestMethod]
        public async Task RegisterAsync_ReturnsFailure_WhenUserCreationFails()
        {
            // Arrange
            var registerViewModel = new RegisterViewModel
            {
                UserName = "newUser",
                Email = "newuser@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                SecurityQuestion = "Favorite teacher",
                SecurityAnswer = "Mr. Smith"
            };

            var mockCreateResult = IdentityResult.Failed(new IdentityError { Description = "Password too weak" });
            Mock.Get(_userManager)
                .Setup(um => um.CreateAsync(It.IsAny<User>(), registerViewModel.Password))
                .ReturnsAsync(mockCreateResult);

            // Act
            var result = await _userService.RegisterAsync(registerViewModel);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Password too weak", result.Message);
        }

        [TestMethod]
        public async Task CreateUserProfile_CreatesProfile_WhenUserExists()
        {
            // Arrange
            var user1 = new User
            {
                UserName = "existingUser1",
                Email = "test@example.com",
                SecurityQuestion = "Favorite teacher",
                SecurityAnswerHash = "Mr. Smith"
            };
            _context.Users.Add(user1);
            await _context.SaveChangesAsync();

            var profilePicturePath = Path.Combine("wwwroot", "Common", "profile-picture.png");
            Directory.CreateDirectory(Path.GetDirectoryName(profilePicturePath));
            File.WriteAllBytes(profilePicturePath, new byte[] { 1, 2, 3 });

            // Act
            await _userService.CreateUserProfile(user1.UserName);

            // Assert
            var createdProfile = _context.Profiles.FirstOrDefault(p => p.UserID == user1.Id);
            Assert.IsNotNull(createdProfile, "Profile should be created for the user.");
            Assert.AreEqual("This is your profile bio!", createdProfile.Bio, "Default bio should be set.");
            Assert.AreEqual("image/png", createdProfile.ImageMimeType, "Default MIME type should be set.");
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, createdProfile.ImageData, "Profile image data should match the file.");
        }

        [TestMethod]
        public async Task CreateUserProfile_DoesNotCreateProfile_WhenUserDoesNotExist()
        {
            // Arrange
            var username = "nonExistentUser";

            // Act
            await _userService.CreateUserProfile(username);

            // Assert
            var createdProfile = _context.Profiles.FirstOrDefault();
            Assert.IsNull(createdProfile, "No profile should be created for a non-existent user.");
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task CreateUserProfile_ThrowsException_WhenProfilePictureFileIsMissing()
        {
            // Arrange
            var username = "existingUser";
            var user = new User { Id = "1", UserName = username,
                SecurityQuestion = "Favorite teacher",
                SecurityAnswerHash = "Mr. Smith"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Ensure the profile picture file does not exist
            var profilePicturePath = Path.Combine("wwwroot", "Common", "profile-picture.png");
            if (File.Exists(profilePicturePath))
            {
                File.Delete(profilePicturePath);
            }

            // Act
            await _userService.CreateUserProfile(username);
        }

        [TestMethod]
        public async Task GetSecurityQuestionAsync_ReturnsQuestion_WhenUserExists()
        {
            // Arrange
            var userName = "testUser";
            _context.Users.Add(new User
            {
                UserName = "testUser",
                Email = "test@example.com",
                SecurityQuestion = "What is your pet's name?",
                SecurityAnswerHash = "Bingo"
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetSecurityQuestionAsync(userName);

            // Assert
            Assert.AreEqual("What is your pet's name?", result, "The returned security question should match the user's stored question.");
        }

        [TestMethod]
        public async Task GetSecurityQuestionAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            // Arrange
            var userNameOrEmail = "nonExistentUser";

            // Act
            var result = await _userService.GetSecurityQuestionAsync(userNameOrEmail);

            // Assert
            Assert.IsNull(result, "The method should return null for a user that does not exist.");
        }

        [TestMethod]
        public async Task VerifySecurityAnswerAsync_ReturnsTrue_WhenAnswerIsCorrect()
        {
            // Arrange
            var userName = "testUser";
            var securityAnswer = "fluffy";
            var hashedAnswer = new PasswordHasher<User>().HashPassword(null, securityAnswer);

            _context.Users.Add(new User
            {
                UserName = userName,
                Email = "test@example.com",
                SecurityQuestion = "What is your pet's name?",
                SecurityAnswerHash = hashedAnswer
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.VerifySecurityAnswerAsync(userName, securityAnswer);

            // Assert
            Assert.IsTrue(result, "The method should return true for a correct security answer.");
        }

        [TestMethod]
        public async Task VerifySecurityAnswerAsync_ReturnsFalse_WhenAnswerIsIncorrect()
        {
            // Arrange
            var userName = "testUser";
            var correctAnswer = "fluffy";
            var wrongAnswer = "notFluffy";
            var hashedAnswer = new PasswordHasher<User>().HashPassword(null, correctAnswer);

            _context.Users.Add(new User
            {
                UserName = userName,
                Email = "test@example.com",
                SecurityQuestion = "What is your pet's name?",
                SecurityAnswerHash = hashedAnswer
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.VerifySecurityAnswerAsync(userName, wrongAnswer);

            // Assert
            Assert.IsFalse(result, "The method should return false for an incorrect security answer.");
        }

        [TestMethod]
        public async Task VerifySecurityAnswerAsync_ReturnsFalse_WhenUserDoesNotExist()
        {
            // Arrange
            var userNameOrEmail = "nonExistentUser";
            var securityAnswer = "someAnswer";

            // Act
            var result = await _userService.VerifySecurityAnswerAsync(userNameOrEmail, securityAnswer);

            // Assert
            Assert.IsFalse(result, "The method should return false for a nonexistent user.");
        }

        [TestMethod]
        public async Task ResetPasswordAsync_ReturnsTrue_WhenUserExistsAndPasswordIsReset()
        {
            // Arrange
            var userName = "testUser";
            var email = "test@example.com";
            var newPassword = "NewPassword123";

            var user = new User
            {
                UserName = userName,
                Email = email,
                PasswordHash = new PasswordHasher<User>().HashPassword(null, "OldPassword123"),
                SecurityQuestion = "What is your pet's name?",
                SecurityAnswerHash = "Bingo"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.ResetPasswordAsync(userName, newPassword);

            // Assert
            Assert.IsTrue(result, "The method should return true when the password is successfully reset.");

            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            Assert.IsNotNull(updatedUser, "The user should still exist in the database.");
            var passwordHasher = new PasswordHasher<User>();
            var verificationResult = passwordHasher.VerifyHashedPassword(null, updatedUser.PasswordHash, newPassword);
            Assert.AreEqual(PasswordVerificationResult.Success, verificationResult, "The password hash should match the new password.");
        }

        [TestMethod]
        public async Task ResetPasswordAsync_ReturnsFalse_WhenUserDoesNotExist()
        {
            // Arrange
            var userNameOrEmail = "nonExistentUser";
            var newPassword = "NewPassword123";

            // Act
            var result = await _userService.ResetPasswordAsync(userNameOrEmail, newPassword);

            // Assert
            Assert.IsFalse(result, "The method should return false when the user does not exist.");
        }

        [TestMethod]
        public async Task ResetPasswordAsync_ReturnsTrue_WhenUserExistsAndPasswordResetByEmail()
        {
            // Arrange
            var userName = "testUser";
            var email = "test@example.com";
            var newPassword = "NewPassword123";

            var user = new User
            {
                UserName = userName,
                Email = email,
                PasswordHash = new PasswordHasher<User>().HashPassword(null, "OldPassword123"),
                SecurityQuestion = "What is your pet's name?",
                SecurityAnswerHash = "Bingo"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.ResetPasswordAsync(email, newPassword);

            // Assert
            Assert.IsTrue(result, "The method should return true when the password is successfully reset by email.");

            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            Assert.IsNotNull(updatedUser, "The user should still exist in the database.");
            var passwordHasher = new PasswordHasher<User>();
            var verificationResult = passwordHasher.VerifyHashedPassword(null, updatedUser.PasswordHash, newPassword);
            Assert.AreEqual(PasswordVerificationResult.Success, verificationResult, "The password hash should match the new password.");
        }
    }
}
