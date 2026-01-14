using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vänskap_Api.Models
{
    public class Event
    {
        [Key]
        public int Id { get; set; }

        public required string Title { get; set; }
        public string? Description { get; set; }       
        public string? Location { get; set; }
        public int? AgeRangeMin { get; set; }
        public int? AgeRangeMax { get; set; }
        public bool IsPublic { get; set; } 
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime EndTime { get; set; } = DateTime.UtcNow.AddDays(1);
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Img { get; set; }

        public ICollection<EventInterest> EventInterests { get; set; } = new List<EventInterest>();
        public ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();

        [ForeignKey("CreatedByUser")]
        public required string CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public DateTime? LastImageUpdate { get; set; }
        public int ImageUpdateCountToday { get; set; } = 0;
    }
}
