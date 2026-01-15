using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Vänskap_Api;
using Vänskap_Api.Data;
using Vänskap_Api.Models;
using Vänskap_Api.Models.Dtos.Friend;

namespace Vänskap_Api_Tests.IntegrationTests
{
    public class FriendshipControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public FriendshipControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            Environment.SetEnvironmentVariable("BaseUrl", "http://localhost:5173");
            _factory = factory;
            _client = _factory.CreateClient();
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        #region SendFriendRequest Tests

        [Fact]
        public async Task SendFriendRequest_UserNotFound_ReturnsBadRequest()
        {
            // Act
            var response = await _client.PostAsync("/api/Friendship/send-friend-request/nonexistentuser", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region SeeFriendList Tests

        [Fact]
        public async Task SeeFriendList_ReturnsOkWithFriends()
        {
            // Act
            var response = await _client.GetAsync("/api/Friendship/friends");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var friends = await response.Content.ReadFromJsonAsync<List<GetFriendsDto>>();
            friends.Should().NotBeNull();
        }

        #endregion

        #region SeeFriendRequests Tests

        [Fact]
        public async Task SeeFriendRequests_ReturnsOkWithRequests()
        {
            // Act
            var response = await _client.GetAsync("/api/Friendship/friend-requests");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var requests = await response.Content.ReadFromJsonAsync<GetFriendRequestsDto>();
            requests.Should().NotBeNull();
            requests!.IncomingUsernames.Should().NotBeNull();
            requests.OutgoingUsernames.Should().NotBeNull();
        }

        #endregion

        #region AcceptFriendRequest Tests

        [Fact]
        public async Task AcceptFriendRequest_ValidRequest_ReturnsOk()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var senderUser = new ApplicationUser
            {
                Id = $"sender-{Guid.NewGuid()}",
                UserName = $"sender{Guid.NewGuid():N}",
                FirstName = "Sender",
                LastName = "User",
                Email = $"sender{Guid.NewGuid():N}@test.com",
                DateOfBirth = new DateOnly(1993, 1, 1)
            };
            context.Users.Add(senderUser);

            var friendRequest = new FriendRequest
            {
                SenderId = senderUser.Id,
                ReceiverId = _factory.TestUserId
            };
            context.FriendRequests.Add(friendRequest);
            await context.SaveChangesAsync();

            // Act
            var response = await _client.PostAsync($"/api/Friendship/accept-friend-request/{senderUser.UserName}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify friendship was created
            var friendships = await context.Friendships
                .Where(f => (f.UserId == _factory.TestUserId && f.FriendId == senderUser.Id) ||
                           (f.UserId == senderUser.Id && f.FriendId == _factory.TestUserId))
                .ToListAsync();
            friendships.Should().HaveCount(2);
        }

        [Fact]
        public async Task AcceptFriendRequest_NoRequest_ReturnsBadRequest()
        {
            // Act
            var response = await _client.PostAsync("/api/Friendship/accept-friend-request/nonexistentuser", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region DeclineFriendRequest Tests

        [Fact]
        public async Task DeclineFriendRequest_ValidRequest_ReturnsOk()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var senderUser = new ApplicationUser
            {
                Id = $"decline-sender-{Guid.NewGuid()}",
                UserName = $"declinesender{Guid.NewGuid():N}",
                FirstName = "Decline",
                LastName = "Sender",
                Email = $"decline{Guid.NewGuid():N}@test.com",
                DateOfBirth = new DateOnly(1993, 1, 1)
            };
            context.Users.Add(senderUser);

            var friendRequest = new FriendRequest
            {
                SenderId = senderUser.Id,
                ReceiverId = _factory.TestUserId
            };
            context.FriendRequests.Add(friendRequest);
            await context.SaveChangesAsync();

            // Act
            var response = await _client.DeleteAsync($"/api/Friendship/decline-friend-request/{senderUser.UserName}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify friend request was removed
            var remainingRequest = await context.FriendRequests
                .FirstOrDefaultAsync(fr => fr.SenderId == senderUser.Id && fr.ReceiverId == _factory.TestUserId);
            remainingRequest.Should().BeNull();
        }

        #endregion

        #region RemoveFriend Tests

        [Fact]
        public async Task RemoveFriend_NotFriends_ReturnsBadRequest()
        {
            // Act
            var response = await _client.DeleteAsync("/api/Friendship/removefriend/nonexistentfriend");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region RemoveFriendRequest Tests

        [Fact]
        public async Task RemoveFriendRequest_ValidRequest_ReturnsOk()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var receiverUser = new ApplicationUser
            {
                Id = $"receiver-{Guid.NewGuid()}",
                UserName = $"receiver{Guid.NewGuid():N}",
                FirstName = "Receiver",
                LastName = "User",
                Email = $"receiver{Guid.NewGuid():N}@test.com",
                DateOfBirth = new DateOnly(1993, 1, 1)
            };
            context.Users.Add(receiverUser);

            var friendRequest = new FriendRequest
            {
                SenderId = _factory.TestUserId,
                ReceiverId = receiverUser.Id
            };
            context.FriendRequests.Add(friendRequest);
            await context.SaveChangesAsync();

            // Act
            var response = await _client.DeleteAsync($"/api/Friendship/regret-friend-request/{receiverUser.UserName}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion
    }
}
