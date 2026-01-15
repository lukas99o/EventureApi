using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Vänskap_Api.Data;
using Vänskap_Api.Models;
using Vänskap_Api.Models.Dtos.Conversation;
using Vänskap_Api.Service;

namespace Vänskap_Api_Tests.UnitTests
{
    public class TestConversationService
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

        #region StartPrivateConversation Tests

        [Fact]
        public async Task StartPrivateConversation_NewConversation_CreatesConversation()
        {
            // Arrange
            using var context = CreateInMemoryContext("StartPrivate_New");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            user1.Friendships.Add(new Friendship { UserId = "user1", FriendId = "user2" });
            user2.Friendships.Add(new Friendship { UserId = "user2", FriendId = "user1" });

            await context.Users.AddRangeAsync(user1, user2);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.StartPrivateConversation("janesmith");

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Contain("John");
            result.Title.Should().Contain("Jane");
            result.ConversationParticipants.Should().HaveCount(2);
        }

        [Fact]
        public async Task StartPrivateConversation_ExistingConversation_ReturnsExisting()
        {
            // Arrange
            using var context = CreateInMemoryContext("StartPrivate_Existing");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            user1.Friendships.Add(new Friendship { UserId = "user1", FriendId = "user2" });
            user2.Friendships.Add(new Friendship { UserId = "user2", FriendId = "user1" });

            var existingConversation = new Conversation { Title = "Existing Chat" };
            existingConversation.ConversationParticipants.Add(new ConversationParticipant { UserId = "user1" });
            existingConversation.ConversationParticipants.Add(new ConversationParticipant { UserId = "user2" });

            await context.Users.AddRangeAsync(user1, user2);
            await context.Conversations.AddAsync(existingConversation);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.StartPrivateConversation("janesmith");

            // Assert
            result.Should().NotBeNull();
            result!.ConversationId.Should().Be(existingConversation.Id);
        }

        [Fact]
        public async Task StartPrivateConversation_NotFriends_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext("StartPrivate_NotFriends");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            await context.Users.AddRangeAsync(user1, user2);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.StartPrivateConversation("janesmith");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task StartPrivateConversation_UserNotFound_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext("StartPrivate_UserNotFound");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.StartPrivateConversation("nonexistent");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region SeeAllConversations Tests

        [Fact]
        public async Task SeeAllConversations_HasConversations_ReturnsAll()
        {
            // Arrange
            using var context = CreateInMemoryContext("SeeAll_HasConversations");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };

            var conv1 = new Conversation { Title = "Chat 1" };
            conv1.ConversationParticipants.Add(new ConversationParticipant { UserId = "user1" });

            var conv2 = new Conversation { Title = "Chat 2" };
            conv2.ConversationParticipants.Add(new ConversationParticipant { UserId = "user1" });

            var conv3 = new Conversation { Title = "Other Chat" };
            conv3.ConversationParticipants.Add(new ConversationParticipant { UserId = "user2" });

            await context.Users.AddAsync(user1);
            await context.Conversations.AddRangeAsync(conv1, conv2, conv3);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.SeeAllConversations();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(c => c.Title == "Chat 1");
            result.Should().Contain(c => c.Title == "Chat 2");
            result.Should().NotContain(c => c.Title == "Other Chat");
        }

        [Fact]
        public async Task SeeAllConversations_NoConversations_ReturnsEmpty()
        {
            // Arrange
            using var context = CreateInMemoryContext("SeeAll_NoConversations");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.SeeAllConversations();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SeeAllConversations_WithMessages_IncludesMessages()
        {
            // Arrange
            using var context = CreateInMemoryContext("SeeAll_WithMessages");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };

            var conv = new Conversation { Title = "Chat with Messages" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "user1" });

            await context.Users.AddAsync(user1);
            await context.Conversations.AddAsync(conv);
            await context.SaveChangesAsync();

            conv.Messages.Add(new Message { Content = "Hello", SenderId = "user1", ConversationId = conv.Id });
            conv.Messages.Add(new Message { Content = "World", SenderId = "user1", ConversationId = conv.Id });
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.SeeAllConversations();

            // Assert
            result.Should().HaveCount(1);
            result.First().Messages.Should().HaveCount(2);
        }

        #endregion

        #region SeeConversation Tests

        [Fact]
        public async Task SeeConversation_ValidId_ReturnsConversation()
        {
            // Arrange
            using var context = CreateInMemoryContext("See_Valid");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };

            var conv = new Conversation { Title = "Test Chat" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "user1" });

            await context.Users.AddAsync(user1);
            await context.Conversations.AddAsync(conv);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.SeeConversation(conv.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Test Chat");
        }

        [Fact]
        public async Task SeeConversation_NotParticipant_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext("See_NotParticipant");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };

            var conv = new Conversation { Title = "Other Chat" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "user2" });

            await context.Users.AddAsync(user1);
            await context.Conversations.AddAsync(conv);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.SeeConversation(conv.Id);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task SeeConversation_InvalidId_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext("See_InvalidId");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.SeeConversation(999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region SendMessage Tests

        [Fact]
        public async Task SendMessage_ValidParticipant_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext("Send_Valid");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };

            var conv = new Conversation { Title = "Test Chat" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "user1" });

            await context.Users.AddAsync(user1);
            await context.Conversations.AddAsync(conv);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.SendMessage("Hello World", conv.Id);

            // Assert
            result.Should().BeTrue();
            var messages = await context.Messages.ToListAsync();
            messages.Should().HaveCount(1);
            messages.First().Content.Should().Be("Hello World");
            messages.First().SenderId.Should().Be("user1");
        }

        [Fact]
        public async Task SendMessage_NotParticipant_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("Send_NotParticipant");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };

            var conv = new Conversation { Title = "Other Chat" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "user2" });

            await context.Users.AddAsync(user1);
            await context.Conversations.AddAsync(conv);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.SendMessage("Hello", conv.Id);

            // Assert
            result.Should().BeFalse();
            var messages = await context.Messages.ToListAsync();
            messages.Should().BeEmpty();
        }

        #endregion

        #region EditConversationTitle Tests

        [Fact]
        public async Task EditConversationTitle_NotParticipant_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("EditTitle_NotParticipant");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };

            var conv = new Conversation { Title = "Other Chat" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "user2" });

            await context.Users.AddAsync(user1);
            await context.Conversations.AddAsync(conv);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.EditConversationTitle(conv.Id, "New Title");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task EditConversationTitle_InvalidId_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("EditTitle_InvalidId");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.EditConversationTitle(999, "New Title");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetConversationMessages Tests

        [Fact]
        public async Task GetConversationMessages_HasMessages_ReturnsMessages()
        {
            // Arrange
            using var context = CreateInMemoryContext("GetMessages_HasMessages");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };

            var conv = new Conversation { Title = "Chat" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "user1" });

            await context.Users.AddAsync(user1);
            await context.Conversations.AddAsync(conv);
            await context.SaveChangesAsync();

            await context.Messages.AddRangeAsync(
                new Message { Content = "Hello", SenderId = "user1", ConversationId = conv.Id },
                new Message { Content = "World", SenderId = "user1", ConversationId = conv.Id }
            );
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.GetConversationMessages(conv.Id);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetConversationMessages_NoMessages_ReturnsEmpty()
        {
            // Arrange
            using var context = CreateInMemoryContext("GetMessages_NoMessages");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };

            var conv = new Conversation { Title = "Empty Chat" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "user1" });

            await context.Users.AddAsync(user1);
            await context.Conversations.AddAsync(conv);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.GetConversationMessages(conv.Id);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region EditRoles Tests

        [Fact]
        public async Task EditRoles_NotParticipant_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("EditRoles_NotParticipant");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };

            var conv = new Conversation { Title = "Other Chat" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "user2" });

            await context.Users.AddAsync(user1);
            await context.Conversations.AddAsync(conv);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.EditRoles(conv.Id, new List<EditRoleDto>());

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region RemoveUrselfFromConversation Tests

        [Fact]
        public async Task RemoveYourselfFromConversation_ValidParticipant_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext("RemoveSelf_Valid");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            var conv = new Conversation { Title = "Group Chat" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "user1", Role = "Participant" });
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "user2", Role = "Host" });

            await context.Users.AddRangeAsync(user1, user2);
            await context.Conversations.AddAsync(conv);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.RemoveUrselfFromConversation(conv.Id, new List<string?>());

            // Assert
            result.Should().BeTrue();
            var participants = await context.ConversationParticipants
                .Where(cp => cp.ConversationId == conv.Id)
                .ToListAsync();
            participants.Should().NotContain(p => p.UserId == "user1");
        }

        [Fact]
        public async Task RemoveYourselfFromConversation_NotParticipant_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("RemoveSelf_NotParticipant");
            var mockAccessor = CreateMockHttpContextAccessor("user1");

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };

            var conv = new Conversation { Title = "Other Chat" };
            conv.ConversationParticipants.Add(new ConversationParticipant { UserId = "user2" });

            await context.Users.AddAsync(user1);
            await context.Conversations.AddAsync(conv);
            await context.SaveChangesAsync();

            var service = new ConversationService(context, mockAccessor.Object);

            // Act
            var result = await service.RemoveUrselfFromConversation(conv.Id, new List<string?>());

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
