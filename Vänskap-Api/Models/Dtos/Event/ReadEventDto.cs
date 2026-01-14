using System.ComponentModel.DataAnnotations;

namespace Vänskap_Api.Models.Dtos.Event
{
    public class ReadEventDto
    {
        public int EventId { get; set; }
        public required string UserId { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? Location { get; set; }
        public int? AgeRangeMin { get; set; }
        public int? AgeRangeMax { get; set; }
        public List<string>? Interests { get; set; }
        public List<EventParticipantDto>? EventParticipants { get; set; }
        public List<EventMessageDto>? EventMessages { get; set; }
        public bool IsPublic { get; set; }
        public string? Img { get; set; }
        public int ConversationId { get; set; }
    }
}
