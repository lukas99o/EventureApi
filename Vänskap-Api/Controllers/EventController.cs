using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vänskap_Api.Data;
using Vänskap_Api.Models;
using Vänskap_Api.Models.Dtos.Event;
using Vänskap_Api.Service.IService;

namespace Vänskap_Api.Controllers
{
    [Authorize(Roles = "Admin,User")]
    [ApiController]
    [Route("api/[controller]")]
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private string UserId => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new ArgumentNullException(nameof(UserId));

        public EventController(IEventService eventService, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _eventService = eventService;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromForm] EventDto createEvent)
        {
            var evnt = await _eventService.CreateEvent(createEvent);

            if (evnt.Item2 == "Daily event limit reached")           
                return BadRequest(evnt.Item2);            
            else if (evnt.Item2 == "Invalid file type")            
                return BadRequest(evnt.Item2);
            else if (evnt.Item2 == "File too large")
                return BadRequest(evnt.Item2);
            else
                return CreatedAtAction(nameof(ReadEvent), new { id = evnt.Item1.EventId }, evnt.Item1);
        }

        [HttpPost("join/{id}")]
        public async Task<IActionResult> JoinEvent(int id)
        {
            var result = await _eventService.JoinEvent(id);
            if (!result) return BadRequest("Could not join event.");

            return Ok("Joined the event.");
        }

        [HttpPost("leave/{id}")]
        public async Task<IActionResult> LeaveEvent(int id)
        {
            var result = await _eventService.LeaveEvent(id);
            if (!result) return BadRequest("Could not leave event");

            return Ok("Left the event");
        }

        [HttpPost("publicevents")]
        public async Task<ActionResult<IEnumerable<ReadEventDto>>> ReadAllPublicEvents(ReadAllPublicEventsDto dto)
        {
            return Ok(await _eventService.ReadAllPublicEvents(dto.Interests!, dto.AgeMin, dto.AgeMax));
        }

        [HttpGet("friendsevents")]
        public async Task<ActionResult<IEnumerable<ReadEventDto>>> GetAllFriendEvents()
        {
            return Ok(await _eventService.GetAllFriendEvents());
        }

        [HttpGet("unjoinedevents")]
        public async Task<ActionResult<IEnumerable<ReadEventDto>>> ReadAllUnjoinedEvents(ReadAllPublicEventsDto dto)
        {
            return Ok(await _eventService.GetUnjoinedEvents(dto.Interests!, dto.AgeMin, dto.AgeMax));
        }

        [HttpGet("my-created-events")]
        public async Task<ActionResult<IEnumerable<ReadEventDto>>> GetMyCreatedEvents()
        {
            return Ok(await _eventService.GetMyCreatedEvents());
        }

        [HttpGet("my-joined-events")]
        public async Task<ActionResult<IEnumerable<ReadEventDto>>> GetMyJoinedEvents()
        {
            return Ok(await _eventService.GetMyJoinedEvents());
        }

        //[HttpGet("event-messages")]
        //public async Task<ActionResult<IEnumerable<EventReceiveMessageDto>>> GetEventMessages(int id)
        //{
        //    var messages = await _eventService.GetEventMessages(id);

        //    if (messages == null || !messages.Any())
        //    {
        //        return NotFound("No messages found for this event.");
        //    }

        //    return Ok(messages);
        //}

        [HttpGet("{id}")]
        public async Task<ActionResult<ReadEventDto>> ReadEvent(int id)
        {
            var evnt = await _eventService.ReadEvent(id);
            if (evnt == null) return NotFound();
            return Ok(evnt);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, EventDto updateEvent)
        {
            var success = await _eventService.UpdateEvent(id, updateEvent);

            if (!success.Item1 && success.Item2 == "Event not found") 
                return NotFound(success.Item2);
            else if (!success.Item1 && success.Item2 == "Invalid file type")
                return BadRequest(success.Item2);
            else if (!success.Item1 && success.Item2 == "File too large")
                return BadRequest(success.Item2);
            else if (!success.Item1 && success.Item2 == "You can only update the event image 3 times per day")
                return BadRequest(success.Item2);
            else
                return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var success = await _eventService.DeleteEvent(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpDelete("host-delete/{id}")]
        public async Task<IActionResult> HostDeleteEvent(int id)
        {
            var success = await _eventService.HostDeleteEvent(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpGet("interests")]
        public async Task<ActionResult<List<string>>> GetInterests()
        {
            return Ok(await _eventService.GetInterests());
        }

        [HttpGet("participant-status")]
        public async Task<ActionResult<List<int>>> GetParticipantStatus()
        {
            return Ok(await _eventService.EventPartcipantStatus());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("seed-eventdata")]
        public async Task<IActionResult> SeedEventData()
        {
            var conversations = new List<Conversation>
            {
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
                new() { Title = $"Chat för" },
            };

            await _context.AddRangeAsync(conversations);
            await _context.SaveChangesAsync();

            List<Event> events = new()
            {
                new() { Conversation = conversations[0], Title = "Cooking & Travel Night [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80", StartTime = new DateTime(2026, 3, 15, 18, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 3, 15, 21, 0, 0, DateTimeKind.Local).ToUniversalTime(), Description = "En kväll med matlagning och resor där vi lagar mat från olika länder och delar resetips." },
                new() { Conversation = conversations[1], Title = "Photography Workshop [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1506744038136-46273834b3fb?auto=format&fit=crop&w=800&q=80", StartTime = new DateTime(2026, 4, 10, 14, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 4, 10, 17, 0, 0, DateTimeKind.Local).ToUniversalTime(), Description = "Lär dig grunderna i fotografi och hur du tar bättre bilder med din kamera." },
                new() { Conversation = conversations[2], Title = "Fitness Bootcamp [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://plus.unsplash.com/premium_photo-1726086677610-56498e65eed4?q=80&w=1470&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D", StartTime = new DateTime(2026, 5, 5, 9, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 5, 5, 11, 0, 0, DateTimeKind.Local).ToUniversalTime(), Description = "Intensiv träningsläger för att förbättra styrka och kondition i grupp." },
                new() { Conversation = conversations[3], Title = "Morning Run [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1508609349937-5ec4ae374ebf?auto=format&fit=crop&w=800&q=80", StartTime = new DateTime(2026, 6, 1, 6, 30, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 6, 1, 7, 30, 0, DateTimeKind.Local).ToUniversalTime(), Description = "Energifylld morgonjogg i grupp för att starta dagen på bästa sätt." },
                new() { Conversation = conversations[4], Title = "Hiking Adventure [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1500534623283-312aade485b7?auto=format&fit=crop&w=800&q=80", StartTime = new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 7, 20, 13, 0, 0, DateTimeKind.Local).ToUniversalTime(), Description = "En dagstur i naturen med vandrare och nya bekantskaper." },
                new() { Conversation = conversations[5], Title = "Book Club [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1512820790803-83ca734da794?auto=format&fit=crop&w=800&q=80", StartTime = new DateTime(2026, 2, 25, 18, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 2, 25, 20, 0, 0, DateTimeKind.Local).ToUniversalTime(), Description = "Diskussion kring månadens bok med fika och trevligt sällskap." },
                new() { Conversation = conversations[6], Title = "Hiking Trip [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1470770903676-69b98201ea1c?auto=format&fit=crop&w=800&q=80", StartTime = new DateTime(2026, 8, 5, 7, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Local).ToUniversalTime() },
                new() { Conversation = conversations[7], Title = "Wine Tasting [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1631118473337-dc07ccf892d2?q=80&w=1374&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D", StartTime = new DateTime(2026, 9, 10, 19, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 9, 10, 22, 0, 0, DateTimeKind.Local).ToUniversalTime(), Description = "Provsmakning av olika viner med expertguider och lättare tilltugg." },
                new() { Conversation = conversations[8], Title = "Sushi Workshop [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80", StartTime = new DateTime(2026, 10, 12, 16, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 10, 12, 19, 0, 0, DateTimeKind.Local).ToUniversalTime(), Description = "Lär dig rulla sushi och upptäck japanska smaker." },
                new() { Conversation = conversations[9], Title = "Beach Cleanup [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1644579156448-fd9346f7a333?w=400&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTJ8fHN0cmFuZHN0JUMzJUE0ZG5pbmd8ZW58MHx8MHx8fDA%3D", StartTime = new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Local).ToUniversalTime() },
                new() { Conversation = conversations[10], Title = "Stand-up Comedy [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://plus.unsplash.com/premium_photo-1705883064233-e56b05f42b07?w=400&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NXx8Y29tZWR5fGVufDB8fDB8fHww", StartTime = new DateTime(2026, 11, 5, 20, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 11, 5, 22, 0, 0, DateTimeKind.Local).ToUniversalTime(), Description = "En kväll fylld med skratt och humor från lokala komiker." },
                new() { Conversation = conversations[11], Title = "Coffee Meetup [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1509042239860-f550ce710b93?auto=format&fit=crop&w=800&q=80", StartTime = new DateTime(2026, 1, 20, 14, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 1, 20, 16, 0, 0, DateTimeKind.Local).ToUniversalTime(), Description = "En avslappnad träff för kaffeälskare att umgås och nätverka." },
                new() { Conversation = conversations[12], Title = "Art & Chill [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1494526585095-c41746248156?auto=format&fit=crop&w=800&q=80", StartTime = new DateTime(2026, 3, 10, 18, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 3, 10, 20, 0, 0, DateTimeKind.Local).ToUniversalTime() },
                new() { Conversation = conversations[13], Title = "Outdoor Yoga [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1506744038136-46273834b3fb?auto=format&fit=crop&w=800&q=80", StartTime = new DateTime(2026, 5, 20, 7, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 5, 20, 8, 0, 0, DateTimeKind.Local).ToUniversalTime() },
                new() { Conversation = conversations[14], Title = "Language Exchange [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1521737604893-d14cc237f11d?auto=format&fit=crop&w=800&q=80", StartTime = new DateTime(2026, 7, 1, 17, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 7, 1, 19, 0, 0, DateTimeKind.Local).ToUniversalTime() },
                new() { Conversation = conversations[15], Title = "Bike Ride [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1688136406066-4a7964d3db30?w=400&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTZ8fGN5a2xhfGVufDB8fDB8fHww", StartTime = new DateTime(2026, 8, 15, 9, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 8, 15, 11, 0, 0, DateTimeKind.Local).ToUniversalTime() },
                new() { Conversation = conversations[16], Title = "Movie Marathon [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://plus.unsplash.com/premium_photo-1661675440353-6a6019c95bc7?w=400&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MXx8bW92aWV8ZW58MHx8MHx8fDA%3D", StartTime = new DateTime(2026, 9, 25, 15, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 9, 25, 20, 0, 0, DateTimeKind.Local).ToUniversalTime() },
                new() { Conversation = conversations[17], Title = "Street Food Tour [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1603088549155-6ae9395b928f?w=400&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTJ8fHN0cmVldCUyMGZvb2R8ZW58MHx8MHx8fDA%3D", StartTime = new DateTime(2026, 10, 5, 13, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 10, 5, 16, 0, 0, DateTimeKind.Local).ToUniversalTime() },
                new() { Conversation = conversations[18], Title = "Photography Walk [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://plus.unsplash.com/premium_photo-1664301085009-470198636a73?w=400&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MXx8Zm90b2dyYWZpfGVufDB8fDB8fHww", StartTime = new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 11, 15, 12, 0, 0, DateTimeKind.Local).ToUniversalTime() },
                new() { Conversation = conversations[19], Title = "Midnight Picnic [Demo]", IsPublic = true, CreatedByUserId = UserId, Img = "https://images.unsplash.com/photo-1626450780751-d0d764738e86?w=400&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8cGljbmljJTIwbmlnaHR8ZW58MHx8MHx8fDA%3D", StartTime = new DateTime(2026, 12, 20, 22, 0, 0, DateTimeKind.Local).ToUniversalTime(), EndTime = new DateTime(2026, 12, 21, 1, 0, 0, DateTimeKind.Local).ToUniversalTime() }
            };

            for (int i = 0; i < events.Count; i++)
            {
                conversations[i].Title += $" {events[i].Title}";
            }

            await _context.AddRangeAsync(events);
            await _context.SaveChangesAsync();

            var conversationParticipants = conversations.Select(c => new ConversationParticipant
            {
                UserId = UserId,
                ConversationId = c.Id,
                Role = "Host"
            }).ToList();

            var eventParticipants = events.Select(e => new EventParticipant
            {
                UserId = UserId,
                EventId = e.Id,
                Role = "Host"
            }).ToList();

            var eventInterests = new List<EventInterest>
            {
                new() { InterestId = 1, EventId = events[0].Id },
                new() { InterestId = 2, EventId = events[1].Id },
                new() { InterestId = 3, EventId = events[1].Id },
                new() { InterestId = 4, EventId = events[2].Id },
                new() { InterestId = 5, EventId = events[3].Id },
                new() { InterestId = 6, EventId = events[4].Id }
            };

            await _context.AddRangeAsync(eventParticipants);
            await _context.AddRangeAsync(conversationParticipants);
            await _context.AddRangeAsync(eventInterests);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
