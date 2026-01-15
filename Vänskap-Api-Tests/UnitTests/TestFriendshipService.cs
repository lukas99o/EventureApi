using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Vänskap_Api.Data;
using Vänskap_Api.Models;
using Vänskap_Api.Service;

namespace Vänskap_Api_Tests.UnitTests
{
    public class TestFriendshipService
    {
        private static ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ApplicationDbContext(options);
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

        #region SendFriendRequest Tests

        [Fact]
        public async Task SendFriendRequest_ValidUser_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext("SendFriendRequest_Valid");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            await context.Users.AddRangeAsync(user1, user2);
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.SendFriendRequest("janesmith");

            // Assert
            result.Should().BeTrue();
            var friendRequest = await context.FriendRequests.FirstOrDefaultAsync();
            friendRequest.Should().NotBeNull();
            friendRequest!.SenderId.Should().Be("user1");
            friendRequest.ReceiverId.Should().Be("user2");
        }

        [Fact]
        public async Task SendFriendRequest_UserNotFound_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("SendFriendRequest_NotFound");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.SendFriendRequest("nonexistent");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SendFriendRequest_AlreadySent_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("SendFriendRequest_AlreadySent");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            await context.Users.AddRangeAsync(user1, user2);
            await context.FriendRequests.AddAsync(new FriendRequest { SenderId = "user1", ReceiverId = "user2" });
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.SendFriendRequest("janesmith");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SendFriendRequest_AlreadyFriends_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("SendFriendRequest_AlreadyFriends");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            user2.Friendships.Add(new Friendship { UserId = "user2", FriendId = "user1" });

            await context.Users.AddRangeAsync(user1, user2);
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.SendFriendRequest("janesmith");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region AcceptFriendRequest Tests

        [Fact]
        public async Task AcceptFriendRequest_ValidRequest_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext("AcceptFriendRequest_Valid");
            var mockAccessor = CreateMockHttpContextAccessor("user2");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            await context.Users.AddRangeAsync(user1, user2);
            await context.FriendRequests.AddAsync(new FriendRequest { SenderId = "user1", ReceiverId = "user2" });
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.AcceptFriendRequest("johndoe");

            // Assert
            result.Should().BeTrue();
            
            var friendships = await context.Friendships.ToListAsync();
            friendships.Should().HaveCount(2);
            friendships.Should().Contain(f => f.UserId == "user1" && f.FriendId == "user2");
            friendships.Should().Contain(f => f.UserId == "user2" && f.FriendId == "user1");

            var friendRequest = await context.FriendRequests.FirstOrDefaultAsync();
            friendRequest.Should().BeNull();
        }

        [Fact]
        public async Task AcceptFriendRequest_RequestNotFound_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("AcceptFriendRequest_NotFound");
            var mockAccessor = CreateMockHttpContextAccessor("user2");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            await context.Users.AddRangeAsync(user1, user2);
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.AcceptFriendRequest("johndoe");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region DeclineFriendRequest Tests

        [Fact]
        public async Task DeclineFriendRequest_ValidRequest_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext("DeclineFriendRequest_Valid");
            var mockAccessor = CreateMockHttpContextAccessor("user2");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            await context.Users.AddRangeAsync(user1, user2);
            await context.FriendRequests.AddAsync(new FriendRequest { SenderId = "user1", ReceiverId = "user2" });
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.DeclineFriendRequest("johndoe");

            // Assert
            result.Should().BeTrue();
            var friendRequest = await context.FriendRequests.FirstOrDefaultAsync();
            friendRequest.Should().BeNull();
        }

        [Fact]
        public async Task DeclineFriendRequest_RequestNotFound_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("DeclineFriendRequest_NotFound");
            var mockAccessor = CreateMockHttpContextAccessor("user2");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            await context.Users.AddRangeAsync(user1, user2);
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.DeclineFriendRequest("johndoe");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region RemoveFriend Tests

        [Fact]
        public async Task RemoveFriend_ValidFriend_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext("RemoveFriend_Valid");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            await context.Users.AddRangeAsync(user1, user2);
            await context.Friendships.AddRangeAsync(
                new Friendship { UserId = "user1", FriendId = "user2" },
                new Friendship { UserId = "user2", FriendId = "user1" }
            );
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.RemoveFriend("janesmith");

            // Assert
            result.Should().BeTrue();
            var friendships = await context.Friendships.ToListAsync();
            friendships.Should().BeEmpty();
        }

        [Fact]
        public async Task RemoveFriend_FriendNotFound_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("RemoveFriend_NotFound");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.RemoveFriend("nonexistent");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveFriend_NotFriends_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("RemoveFriend_NotFriends");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            await context.Users.AddRangeAsync(user1, user2);
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.RemoveFriend("janesmith");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region SeeFriendList Tests

        [Fact]
        public async Task SeeFriendList_HasFriends_ReturnsFriendList()
        {
            // Arrange
            using var context = CreateInMemoryContext("SeeFriendList_HasFriends");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith", DateOfBirth = new DateOnly(1990, 1, 1) };
            var user3 = new ApplicationUser { Id = "user3", FirstName = "Bob", LastName = "Jones", UserName = "bobjones", DateOfBirth = new DateOnly(1985, 5, 15) };

            user1.Friendships.Add(new Friendship { UserId = "user1", FriendId = "user2" });
            user1.Friendships.Add(new Friendship { UserId = "user1", FriendId = "user3" });

            await context.Users.AddRangeAsync(user1, user2, user3);
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.SeeFriendList();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(f => f.Username == "janesmith");
            result.Should().Contain(f => f.Username == "bobjones");
        }

        [Fact]
        public async Task SeeFriendList_NoFriends_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext("SeeFriendList_NoFriends");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.SeeFriendList();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region SeeFriendRequests Tests

        [Fact]
        public async Task SeeFriendRequests_HasRequests_ReturnsBothIncomingAndOutgoing()
        {
            // Arrange
            using var context = CreateInMemoryContext("SeeFriendRequests_HasRequests");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };
            var user3 = new ApplicationUser { Id = "user3", FirstName = "Bob", LastName = "Jones", UserName = "bobjones" };

            await context.Users.AddRangeAsync(user1, user2, user3);
            await context.FriendRequests.AddRangeAsync(
                new FriendRequest { SenderId = "user2", ReceiverId = "user1" }, // incoming
                new FriendRequest { SenderId = "user1", ReceiverId = "user3" }  // outgoing
            );
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.SeeFriendRequests();

            // Assert
            result.Should().NotBeNull();
            result.IncomingUsernames.Should().Contain("janesmith");
            result.OutgoingUsernames.Should().Contain("bobjones");
        }

        [Fact]
        public async Task SeeFriendRequests_NoRequests_ReturnsEmptyLists()
        {
            // Arrange
            using var context = CreateInMemoryContext("SeeFriendRequests_NoRequests");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.SeeFriendRequests();

            // Assert
            result.Should().NotBeNull();
            result.IncomingUsernames.Should().BeEmpty();
            result.OutgoingUsernames.Should().BeEmpty();
        }

        #endregion

        #region RemoveFriendRequest Tests

        [Fact]
        public async Task RemoveFriendRequest_ValidRequest_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext("RemoveFriendRequest_Valid");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            await context.Users.AddRangeAsync(user1, user2);
            await context.FriendRequests.AddAsync(new FriendRequest { SenderId = "user1", ReceiverId = "user2" });
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.RemoveFriendRequest("janesmith");

            // Assert
            result.Should().BeTrue();
            var friendRequest = await context.FriendRequests.FirstOrDefaultAsync();
            friendRequest.Should().BeNull();
        }

        [Fact]
        public async Task RemoveFriendRequest_RequestNotFound_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("RemoveFriendRequest_NotFound");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            await context.Users.AddRangeAsync(user1, user2);
            await context.SaveChangesAsync();

            var service = new FriendshipService(context, mockAccessor.Object);

            // Act
            var result = await service.RemoveFriendRequest("janesmith");

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
