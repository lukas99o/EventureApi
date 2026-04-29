using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Vänskap_Api;
using Vänskap_Api.Data;
using Vänskap_Api.Models;
using Vänskap_Api.Models.Dtos.Event;

namespace Vänskap_Api_Tests.IntegrationTests
{
    public class EventControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public EventControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            Environment.SetEnvironmentVariable("BaseUrl", "http://localhost:5173");
            _factory = factory;
            _client = _factory.CreateClient();
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        #region CreateEvent Tests

        [Fact]
        public async Task CreateEvent_ValidEvent_ReturnsCreated()
        {
            // Arrange
            var formContent = new MultipartFormDataContent
            {
                { new StringContent("Test Event"), "Title" },
                { new StringContent("Test Description"), "Description" },
                { new StringContent("true"), "IsPublic" },
                { new StringContent("Test Location"), "Location" },
                { new StringContent(DateTime.UtcNow.AddDays(1).ToString("o")), "StartTime" },
                { new StringContent(DateTime.UtcNow.AddDays(2).ToString("o")), "EndTime" }
            };

            // Act
            var response = await _client.PostAsync("/api/Event", formContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var eventDto = await response.Content.ReadFromJsonAsync<ReadEventDto>();
            eventDto.Should().NotBeNull();
            eventDto!.Title.Should().Be("Test Event");
        }

        #endregion

        #region JoinEvent Tests

        [Fact]
        public async Task JoinEvent_PublicEvent_ReturnsOk()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var otherUser = new ApplicationUser
            {
                Id = $"event-creator-{Guid.NewGuid()}",
                UserName = $"eventcreator{Guid.NewGuid():N}",
                FirstName = "Event",
                LastName = "Creator",
                Email = $"event{Guid.NewGuid():N}@test.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };
            context.Users.Add(otherUser);

            var conversation = new Conversation { Title = "Event Chat" };
            var evnt = new Event
            {
                Title = "Joinable Event",
                CreatedByUserId = otherUser.Id,
                IsPublic = true,
                Conversation = conversation,
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(2)
            };
            evnt.EventParticipants.Add(new EventParticipant { UserId = otherUser.Id, Role = "Host" });
            context.Events.Add(evnt);
            await context.SaveChangesAsync();

            // Act
            var response = await _client.PostAsync($"/api/Event/join/{evnt.Id}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task JoinEvent_EventNotFound_ReturnsBadRequest()
        {
            // Act
            var response = await _client.PostAsync("/api/Event/join/99999", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region LeaveEvent Tests

        [Fact]
        public async Task LeaveEvent_Participant_ReturnsOk()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var otherUser = new ApplicationUser
            {
                Id = $"leave-creator-{Guid.NewGuid()}",
                UserName = $"leavecreator{Guid.NewGuid():N}",
                FirstName = "Leave",
                LastName = "Creator",
                Email = $"leave{Guid.NewGuid():N}@test.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };
            context.Users.Add(otherUser);

            var conversation = new Conversation { Title = "Leave Event Chat" };
            var evnt = new Event
            {
                Title = "Leavable Event",
                CreatedByUserId = otherUser.Id,
                IsPublic = true,
                Conversation = conversation,
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(2)
            };
            evnt.EventParticipants.Add(new EventParticipant { UserId = otherUser.Id, Role = "Host" });
            evnt.EventParticipants.Add(new EventParticipant { UserId = _factory.TestUserId, Role = "Participant" });
            context.Events.Add(evnt);
            await context.SaveChangesAsync();

            // Act
            var response = await _client.PostAsync($"/api/Event/leave/{evnt.Id}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LeaveEvent_NotParticipant_ReturnsBadRequest()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var otherUser = new ApplicationUser
            {
                Id = $"leave-other-{Guid.NewGuid()}",
                UserName = $"leaveother{Guid.NewGuid():N}",
                FirstName = "Other",
                LastName = "Creator",
                Email = $"leaveother{Guid.NewGuid():N}@test.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };
            context.Users.Add(otherUser);

            var conversation = new Conversation { Title = "Not Joined Chat" };
            var evnt = new Event
            {
                Title = "Not Joined Event",
                CreatedByUserId = otherUser.Id,
                IsPublic = true,
                Conversation = conversation,
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(2)
            };
            evnt.EventParticipants.Add(new EventParticipant { UserId = otherUser.Id, Role = "Host" });
            context.Events.Add(evnt);
            await context.SaveChangesAsync();

            // Act
            var response = await _client.PostAsync($"/api/Event/leave/{evnt.Id}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region ReadAllPublicEvents Tests

        [Fact]
        public async Task ReadAllPublicEvents_ReturnsOk()
        {
            // Arrange
            var dto = new ReadAllPublicEventsDto
            {
                Interests = new List<string?>(),
                AgeMin = null,
                AgeMax = null,
                Sort = "newest",
                Page = 1,
                PageSize = 10
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Event/publicevents", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region GetMyCreatedEvents Tests

        [Fact]
        public async Task GetMyCreatedEvents_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/Event/my-created-events");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var events = await response.Content.ReadFromJsonAsync<List<ReadEventDto>>();
            events.Should().NotBeNull();
        }

        #endregion

        #region GetMyJoinedEvents Tests

        [Fact]
        public async Task GetMyJoinedEvents_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/Event/my-joined-events");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var events = await response.Content.ReadFromJsonAsync<List<ReadEventDto>>();
            events.Should().NotBeNull();
        }

        #endregion

        #region GetAllFriendEvents Tests

        [Fact]
        public async Task GetAllFriendEvents_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/Event/friendsevents");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region ReadEvent Tests

        [Fact]
        public async Task ReadEvent_ValidId_ReturnsOk()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var conversation = new Conversation { Title = "Read Event Chat" };
            var evnt = new Event
            {
                Title = "Readable Event",
                CreatedByUserId = _factory.TestUserId,
                IsPublic = true,
                Conversation = conversation,
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(2)
            };
            evnt.EventParticipants.Add(new EventParticipant { UserId = _factory.TestUserId, Role = "Host" });
            context.Events.Add(evnt);
            await context.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync($"/api/Event/{evnt.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var eventDto = await response.Content.ReadFromJsonAsync<ReadEventDto>();
            eventDto.Should().NotBeNull();
            eventDto!.Title.Should().Be("Readable Event");
        }

        #endregion

        #region GetInterests Tests

        [Fact]
        public async Task GetInterests_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/Event/interests");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var interests = await response.Content.ReadFromJsonAsync<List<string>>();
            interests.Should().NotBeNull();
        }

        #endregion
    }
}
