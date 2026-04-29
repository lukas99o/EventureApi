using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Vänskap_Api.Data;
using Vänskap_Api.Models;
using Vänskap_Api.Models.Dtos.Event;
using Vänskap_Api.Service;

namespace Vänskap_Api_Tests.UnitTests
{
    public class TestEventService
    {
        private static ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static Mock<IHttpContextAccessor> CreateMockHttpContextAccessor(string userId, string? userName = null)
        {
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockHttpContext = new Mock<HttpContext>();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            if (userName != null)
            {
                claims.Add(new Claim(ClaimTypes.Name, userName));
            }
            var mockUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));

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

        #region CreateEvent Tests

        [Fact]
        public async Task CreateEvent_ValidEvent_ReturnsEvent()
        {
            // Arrange
            using var context = CreateInMemoryContext("CreateEvent_Valid");
            var mockAccessor = CreateMockHttpContextAccessor("user1", "johndoe");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            var eventDto = new EventDto
            {
                Title = "Test Event",
                Description = "Test Description",
                IsPublic = true,
                Location = "Test Location",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(2)
            };

            // Act
            var (result, error) = await service.CreateEvent(eventDto);

            // Assert
            error.Should().BeNull();
            result.Should().NotBeNull();
            result!.Title.Should().Be("Test Event");
            result.UserId.Should().Be("user1");
        }

        [Fact]
        public async Task CreateEvent_DailyLimitReached_ReturnsError()
        {
            // Arrange
            using var context = CreateInMemoryContext("CreateEvent_LimitReached");
            var mockAccessor = CreateMockHttpContextAccessor("user1", "johndoe");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);

            // Add 5 events created today
            for (int i = 0; i < 5; i++)
            {
                await context.Events.AddAsync(new Event
                {
                    Title = $"Event {i}",
                    CreatedByUserId = "user1",
                    CreatedAt = DateTime.UtcNow,
                    Conversation = new Conversation { Title = $"Chat for Event {i}" }
                });
            }
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            var eventDto = new EventDto
            {
                Title = "Test Event 6",
                IsPublic = true
            };

            // Act
            var (result, error) = await service.CreateEvent(eventDto);

            // Assert
            result.Should().BeNull();
            error.Should().Be("Daily event limit reached");
        }

        #endregion

        #region JoinEvent Tests

        [Fact]
        public async Task JoinEvent_PublicEvent_ValidUser_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext("JoinEvent_Public_Valid");
            var mockAccessor = CreateMockHttpContextAccessor("user2");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith", DateOfBirth = new DateOnly(1990, 1, 1) };

            var conversation = new Conversation { Title = "Event Chat" };
            var evnt = new Event
            {
                Title = "Test Event",
                CreatedByUserId = "user1",
                IsPublic = true,
                Conversation = conversation
            };
            evnt.EventParticipants.Add(new EventParticipant { UserId = "user1", Role = "Host" });

            await context.Users.AddRangeAsync(user1, user2);
            await context.Events.AddAsync(evnt);
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.JoinEvent(evnt.Id);

            // Assert
            result.Should().BeTrue();
            var updatedEvent = await context.Events.Include(e => e.EventParticipants).FirstAsync(e => e.Id == evnt.Id);
            updatedEvent.EventParticipants.Should().HaveCount(2);
            updatedEvent.EventParticipants.Should().Contain(p => p.UserId == "user2");
        }

        [Fact]
        public async Task JoinEvent_EventNotFound_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("JoinEvent_NotFound");
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.JoinEvent(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task JoinEvent_AlreadyParticipant_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("JoinEvent_AlreadyParticipant");
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe", DateOfBirth = new DateOnly(1990, 1, 1) };

            var conversation = new Conversation { Title = "Event Chat" };
            var evnt = new Event
            {
                Title = "Test Event",
                CreatedByUserId = "user1",
                IsPublic = true,
                Conversation = conversation
            };
            evnt.EventParticipants.Add(new EventParticipant { UserId = "user1", Role = "Host" });

            await context.Users.AddAsync(user1);
            await context.Events.AddAsync(evnt);
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.JoinEvent(evnt.Id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task JoinEvent_PrivateEvent_NotFriends_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("JoinEvent_Private_NotFriends");
            var mockAccessor = CreateMockHttpContextAccessor("user2");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith", DateOfBirth = new DateOnly(1990, 1, 1) };

            var conversation = new Conversation { Title = "Event Chat" };
            var evnt = new Event
            {
                Title = "Test Event",
                CreatedByUserId = "user1",
                IsPublic = false,
                Conversation = conversation
            };
            evnt.EventParticipants.Add(new EventParticipant { UserId = "user1", Role = "Host" });

            await context.Users.AddRangeAsync(user1, user2);
            await context.Events.AddAsync(evnt);
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.JoinEvent(evnt.Id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task JoinEvent_PrivateEvent_Friends_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext("JoinEvent_Private_Friends");
            var mockAccessor = CreateMockHttpContextAccessor("user2");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith", DateOfBirth = new DateOnly(1990, 1, 1) };

            var conversation = new Conversation { Title = "Event Chat" };
            var evnt = new Event
            {
                Title = "Test Event",
                CreatedByUserId = "user1",
                IsPublic = false,
                Conversation = conversation
            };
            evnt.EventParticipants.Add(new EventParticipant { UserId = "user1", Role = "Host" });

            // Add friendship
            var friendship = new Friendship { UserId = "user1", FriendId = "user2" };

            await context.Users.AddRangeAsync(user1, user2);
            await context.Events.AddAsync(evnt);
            await context.Friendships.AddAsync(friendship);
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.JoinEvent(evnt.Id);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region LeaveEvent Tests

        [Fact]
        public async Task LeaveEvent_Participant_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext("LeaveEvent_Valid");
            var mockAccessor = CreateMockHttpContextAccessor("user2");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            var conversation = new Conversation { Title = "Event Chat" };
            var evnt = new Event
            {
                Title = "Test Event",
                CreatedByUserId = "user1",
                IsPublic = true,
                Conversation = conversation
            };
            evnt.EventParticipants.Add(new EventParticipant { UserId = "user1", Role = "Host" });
            evnt.EventParticipants.Add(new EventParticipant { UserId = "user2", Role = "Participant" });

            await context.Users.AddRangeAsync(user1, user2);
            await context.Events.AddAsync(evnt);
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.LeaveEvent(evnt.Id);

            // Assert
            result.Should().BeTrue();
            var updatedEvent = await context.Events.Include(e => e.EventParticipants).FirstAsync(e => e.Id == evnt.Id);
            updatedEvent.EventParticipants.Should().HaveCount(1);
            updatedEvent.EventParticipants.Should().NotContain(p => p.UserId == "user2");
        }

        [Fact]
        public async Task LeaveEvent_NotParticipant_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("LeaveEvent_NotParticipant");
            var mockAccessor = CreateMockHttpContextAccessor("user2");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            var conversation = new Conversation { Title = "Event Chat" };
            var evnt = new Event
            {
                Title = "Test Event",
                CreatedByUserId = "user1",
                IsPublic = true,
                Conversation = conversation
            };
            evnt.EventParticipants.Add(new EventParticipant { UserId = "user1", Role = "Host" });

            await context.Users.AddRangeAsync(user1, user2);
            await context.Events.AddAsync(evnt);
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.LeaveEvent(evnt.Id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task LeaveEvent_EventNotFound_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("LeaveEvent_NotFound");
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.LeaveEvent(999);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetMyCreatedEvents Tests

        [Fact]
        public async Task GetMyCreatedEvents_HasEvents_ReturnsEvents()
        {
            // Arrange
            using var context = CreateInMemoryContext("GetMyCreatedEvents_HasEvents");
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };

            await context.Users.AddAsync(user1);
            await context.Events.AddRangeAsync(
                new Event { Title = "Event 1", CreatedByUserId = "user1", Conversation = new Conversation { Title = "Chat 1" } },
                new Event { Title = "Event 2", CreatedByUserId = "user1", Conversation = new Conversation { Title = "Chat 2" } },
                new Event { Title = "Other Event", CreatedByUserId = "user2", Conversation = new Conversation { Title = "Chat 3" } }
            );
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.GetMyCreatedEvents();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(e => e.Title == "Event 1");
            result.Should().Contain(e => e.Title == "Event 2");
            result.Should().NotContain(e => e.Title == "Other Event");
        }

        [Fact]
        public async Task GetMyCreatedEvents_NoEvents_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext("GetMyCreatedEvents_NoEvents");
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.GetMyCreatedEvents();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetMyJoinedEvents Tests

        [Fact]
        public async Task GetMyJoinedEvents_HasJoinedEvents_ReturnsEvents()
        {
            // Arrange
            using var context = CreateInMemoryContext("GetMyJoinedEvents_HasEvents");
            var mockAccessor = CreateMockHttpContextAccessor("user2");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            var event1 = new Event { Title = "Event 1", CreatedByUserId = "user1", Conversation = new Conversation { Title = "Chat 1" } };
            event1.EventParticipants.Add(new EventParticipant { UserId = "user2", Role = "Participant" });

            var event2 = new Event { Title = "Event 2", CreatedByUserId = "user2", Conversation = new Conversation { Title = "Chat 2" } };
            event2.EventParticipants.Add(new EventParticipant { UserId = "user2", Role = "Host" });

            await context.Users.AddRangeAsync(user1, user2);
            await context.Events.AddRangeAsync(event1, event2);
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.GetMyJoinedEvents();

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(e => e.Title == "Event 1");
            result.Should().NotContain(e => e.Title == "Event 2"); // Created by user, not joined
        }

        [Fact]
        public async Task GetMyJoinedEvents_NoJoinedEvents_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext("GetMyJoinedEvents_NoEvents");
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            await context.Users.AddAsync(user1);
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.GetMyJoinedEvents();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region HostDeleteEvent Tests

        [Fact]
        public async Task HostDeleteEvent_OwnEvent_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext("HostDeleteEvent_Own");
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };

            var conversation = new Conversation { Title = "Event Chat" };
            var evnt = new Event
            {
                Title = "Test Event",
                CreatedByUserId = "user1",
                Conversation = conversation
            };
            evnt.EventParticipants.Add(new EventParticipant { UserId = "user1", Role = "Host" });

            await context.Users.AddAsync(user1);
            await context.Events.AddAsync(evnt);
            await context.SaveChangesAsync();

            var eventId = evnt.Id;
            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.HostDeleteEvent(eventId);

            // Assert
            result.Should().BeTrue();
            var deletedEvent = await context.Events.FindAsync(eventId);
            deletedEvent.Should().BeNull();
        }

        [Fact]
        public async Task HostDeleteEvent_NotOwner_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext("HostDeleteEvent_NotOwner");
            var mockAccessor = CreateMockHttpContextAccessor("user2");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", UserName = "johndoe" };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", UserName = "janesmith" };

            var conversation = new Conversation { Title = "Event Chat" };
            var evnt = new Event
            {
                Title = "Test Event",
                CreatedByUserId = "user1",
                Conversation = conversation
            };

            await context.Users.AddRangeAsync(user1, user2);
            await context.Events.AddAsync(evnt);
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.HostDeleteEvent(evnt.Id);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetInterests Tests

        [Fact]
        public async Task GetInterests_HasInterests_ReturnsOrderedList()
        {
            // Arrange
            using var context = CreateInMemoryContext("GetInterests_HasInterests");
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            await context.Interests.AddRangeAsync(
                new Interest { Name = "Sports" },
                new Interest { Name = "Music" },
                new Interest { Name = "Art" }
            );
            await context.SaveChangesAsync();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.GetInterests();

            // Assert
            result.Should().HaveCount(3);
            result.Should().BeInAscendingOrder();
            result.Should().ContainInOrder("Art", "Music", "Sports");
        }

        [Fact]
        public async Task GetInterests_NoInterests_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext("GetInterests_NoInterests");
            var mockAccessor = CreateMockHttpContextAccessor("user1");
            var mockEnvironment = CreateMockWebHostEnvironment();

            var service = new EventService(context, mockAccessor.Object, mockEnvironment.Object);

            // Act
            var result = await service.GetInterests();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion
    }
}
