using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Vänskap_Api;
using Vänskap_Api.Data;
using Vänskap_Api.Models;
using Vänskap_Api.Models.Dtos.Conversation;

namespace Vänskap_Api_Tests.IntegrationTests
{
    public class ConversationControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ConversationControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            Environment.SetEnvironmentVariable("BaseUrl", "http://localhost:5173");
            _factory = factory;
            _client = _factory.CreateClient();
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        #region StartPrivateConversation Tests

        [Fact]
        public async Task StartPrivateConversation_ValidFriend_ReturnsOk()
        {
            // The factory seeds a friendship with "frienduser"
            // Act
            var response = await _client.PostAsync("/api/Conversation/start-private-conversation/frienduser", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var conversation = await response.Content.ReadFromJsonAsync<SeeConversationDto>();
            conversation.Should().NotBeNull();
        }

        [Fact]
        public async Task StartPrivateConversation_NotFriends_ReturnsBadRequest()
        {
            // "otheruser" is not a friend of the test user
            // Act
            var response = await _client.PostAsync("/api/Conversation/start-private-conversation/otheruser", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task StartPrivateConversation_UserNotFound_ReturnsBadRequest()
        {
            // Act
            var response = await _client.PostAsync("/api/Conversation/start-private-conversation/nonexistentuser", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region SeeAllConversations Tests

        [Fact]
        public async Task SeeAllConversations_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/Conversation");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var conversations = await response.Content.ReadFromJsonAsync<List<SeeConversationDto>>();
            conversations.Should().NotBeNull();
        }

        #endregion

        #region SeeConversation Tests

        [Fact]
        public async Task SeeConversation_NotParticipant_ReturnsBadRequest()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var conv = new Conversation { Title = "Other Conversation" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "other-user-id" });
            context.Conversations.Add(conv);
            await context.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync($"/api/Conversation/{conv.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SeeConversation_InvalidId_ReturnsBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/Conversation/99999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region GetConversationMessages Tests

        [Fact]
        public async Task GetConversationMessages_ValidId_ReturnsOk()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var conv = new Conversation { Title = "Messages Conversation" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = _factory.TestUserId });
            context.Conversations.Add(conv);
            await context.SaveChangesAsync();

            context.Messages.Add(new Message { Content = "Hello", SenderId = _factory.TestUserId, ConversationId = conv.Id });
            await context.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync($"/api/Conversation/get-conversation-messages/{conv.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region SendMessage Tests

        [Fact]
        public async Task SendMessage_NotParticipant_ReturnsBadRequest()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var conv = new Conversation { Title = "Not My Message Conversation" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "other-user-id" });
            context.Conversations.Add(conv);
            await context.SaveChangesAsync();

            // Act
            var response = await _client.PostAsync($"/api/Conversation/messages?content=Hello&id={conv.Id}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion
    }
}
