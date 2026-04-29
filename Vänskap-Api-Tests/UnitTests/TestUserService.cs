using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Vänskap_Api.Data;
using Vänskap_Api.Models;
using Vänskap_Api.Models.Dtos.User;

namespace Vänskap_Api_Tests.UnitTests
{
    public class TestUserService
    {
        private static Mock<UserManager<ApplicationUser>> CreateMockUserManager(List<ApplicationUser> users)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<ApplicationUser>>().Object,
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<ApplicationUser>>>().Object
            );

            mockUserManager.Setup(m => m.Users).Returns(users.AsQueryable());
            mockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => users.FirstOrDefault(u => u.Id == id));
            mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            return mockUserManager;
        }

        private static Mock<IHttpContextAccessor> CreateMockHttpContextAccessor(string userId)
        {
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "mock"));

            mockHttpContext.Setup(ctx => ctx.User).Returns(mockUser);
            mockHttpContextAccessor.Setup(accessor => accessor.HttpContext).Returns(mockHttpContext.Object);

            return mockHttpContextAccessor;
        }

        private static Mock<IWebHostEnvironment> CreateMockWebHostEnvironment()
        {
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
            return mockEnvironment;
        }

        #region GetUser Tests

        [Fact]
        public async Task GetUser_ValidUser_ReturnsUserDto()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = "user1",
                    UserName = "johndoe",
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateOnly(1990, 6, 15),
                    About = "Test about",
                    ProfilePicturePath = "/images/test.jpg"
                }
            };

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act
            var result = await service.GetUser("user1");

            // Assert
            result.Should().NotBeNull();
            result!.UserName.Should().Be("johndoe");
            result.FirstName.Should().Be("John");
            result.LastName.Should().Be("Doe");
            result.About.Should().Be("Test about");
            result.ProfilePicturePath.Should().Be("/images/test.jpg");
        }

        [Fact]
        public async Task GetUser_UserNotFound_ReturnsNull()
        {
            // Arrange
            var users = new List<ApplicationUser>();

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act
            var result = await service.GetUser("nonexistent");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUser_NoProfilePicture_ReturnsEmptyPath()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = "user1",
                    UserName = "johndoe",
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateOnly(1990, 6, 15),
                    ProfilePicturePath = null
                }
            };

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act
            var result = await service.GetUser("user1");

            // Assert
            result.Should().NotBeNull();
            result!.ProfilePicturePath.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUser_CalculatesAgeCorrectly()
        {
            // Arrange
            var today = DateTime.Now;
            var birthYear = today.Year - 30;
            var birthdayPassed = new DateOnly(birthYear, 1, 1);
            var birthdayNotPassed = new DateOnly(birthYear, 12, 31);

            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = "user1",
                    UserName = "user1",
                    FirstName = "Test",
                    LastName = "User",
                    DateOfBirth = birthdayPassed
                },
                new ApplicationUser
                {
                    Id = "user2",
                    UserName = "user2",
                    FirstName = "Test",
                    LastName = "User2",
                    DateOfBirth = birthdayNotPassed
                }
            };

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act
            var result1 = await service.GetUser("user1");
            var result2 = await service.GetUser("user2");

            // Assert
            result1.Should().NotBeNull();
            result1!.Age.Should().Be(30);

            result2.Should().NotBeNull();
            // If birthday hasn't passed yet this year, age should be 29
            if (today.DayOfYear < new DateTime(today.Year, 12, 31).DayOfYear)
                result2!.Age.Should().Be(29);
            else
                result2!.Age.Should().Be(30);
        }

        #endregion

        #region UpdateUserAbout Tests

        [Fact]
        public async Task UpdateUserAbout_ValidUser_ReturnsTrue()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = "user1",
                    UserName = "johndoe",
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateOnly(1990, 6, 15),
                    About = "Old about"
                }
            };

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act
            var result = await service.UpdateUserAbout("New about text");

            // Assert
            result.Should().BeTrue();
            mockUserManager.Verify(m => m.UpdateAsync(It.Is<ApplicationUser>(u => u.About == "New about text")), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAbout_UserNotFound_ThrowsException()
        {
            // Arrange
            var users = new List<ApplicationUser>();

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("nonexistent");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.UpdateUserAbout("New about text"));
        }

        [Fact]
        public async Task UpdateUserAbout_EmptyAbout_UpdatesSuccessfully()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = "user1",
                    UserName = "johndoe",
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateOnly(1990, 6, 15),
                    About = "Some text"
                }
            };

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act
            var result = await service.UpdateUserAbout("");

            // Assert
            result.Should().BeTrue();
            mockUserManager.Verify(m => m.UpdateAsync(It.Is<ApplicationUser>(u => u.About == "")), Times.Once);
        }

        #endregion

        #region UploadProfilePicture Tests

        [Fact]
        public async Task UploadProfilePicture_InvalidFileType_ThrowsException()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = "user1",
                    UserName = "johndoe",
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateOnly(1990, 6, 15)
                }
            };

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.gif");
            mockFile.Setup(f => f.Length).Returns(1024);

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => service.UploadProfilePictureAsync(mockFile.Object));
            exception.Message.Should().Be("Invalid file type");
        }

        [Fact]
        public async Task UploadProfilePicture_FileTooLarge_ThrowsException()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = "user1",
                    UserName = "johndoe",
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateOnly(1990, 6, 15)
                }
            };

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(5 * 1024 * 1024); // 5MB, exceeds 2MB limit

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => service.UploadProfilePictureAsync(mockFile.Object));
            exception.Message.Should().Be("File too large");
        }

        [Fact]
        public async Task UploadProfilePicture_UserNotFound_ThrowsException()
        {
            // Arrange
            var users = new List<ApplicationUser>();

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("nonexistent");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(1024);

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => service.UploadProfilePictureAsync(mockFile.Object));
            exception.Message.Should().Be("User not found");
        }

        [Fact]
        public async Task UploadProfilePicture_DailyLimitReached_ThrowsException()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = "user1",
                    UserName = "johndoe",
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateOnly(1990, 6, 15),
                    LastProfilePictureUpload = DateTime.UtcNow,
                    ProfilePictureUploadCountToday = 3
                }
            };

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(1024);

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => service.UploadProfilePictureAsync(mockFile.Object));
            exception.Message.Should().Be("You can only upload 3 profile pictures per day");
        }

        #endregion

        #region DeleteProfilePicture Tests

        [Fact]
        public async Task DeleteProfilePicture_UserNotFound_ThrowsException()
        {
            // Arrange
            var users = new List<ApplicationUser>();

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("nonexistent");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => service.DeleteProfilePictureAsync());
            exception.Message.Should().Be("User not found");
        }

        [Fact]
        public async Task DeleteProfilePicture_NoProfilePicture_ThrowsException()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = "user1",
                    UserName = "johndoe",
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateOnly(1990, 6, 15),
                    ProfilePicturePath = null
                }
            };

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => service.DeleteProfilePictureAsync());
            exception.Message.Should().Be("No profile picture to delete");
        }

        [Fact]
        public async Task DeleteProfilePicture_HasProfilePicture_ReturnsTrue()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), "images", "profiles");
            Directory.CreateDirectory(tempPath);
            var testFilePath = Path.Combine(tempPath, "testprofile.jpg");
            await File.WriteAllTextAsync(testFilePath, "test content");

            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = "user1",
                    UserName = "johndoe",
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateOnly(1990, 6, 15),
                    ProfilePicturePath = "/images/profiles/testprofile.jpg"
                }
            };

            var mockUserManager = CreateMockUserManager(users);
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var service = new UserService(mockUserManager.Object, mockEnvironment.Object, mockAccessor.Object);

            // Act
            var result = await service.DeleteProfilePictureAsync();

            // Assert
            result.Should().BeTrue();
            mockUserManager.Verify(m => m.UpdateAsync(It.Is<ApplicationUser>(u => u.ProfilePicturePath == null)), Times.Once);

            // Cleanup
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }

        #endregion
    }
}
