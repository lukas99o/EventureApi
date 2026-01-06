using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Vänskap_Api.Models
{
    public class ApplicationUser : IdentityUser
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? About { get; set; }
        public ICollection<UserInterest>? UserInterests { get; set; } = new List<UserInterest>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
        public ICollection<EventParticipant> EventParticipations { get; set; } = new List<EventParticipant>();
        public ICollection<ConversationParticipant> ConversationParticipations { get; set; } = new List<ConversationParticipant>();
        public ICollection<Friendship> Friendships { get; set; } = new List<Friendship>();
        public ICollection<MessageReadAt> ReadMessages { get; set; } = new List<MessageReadAt>();
        public string? ProfilePicturePath { get; set; }
        public DateTime? LastProfilePictureUpload { get; set; }
        public int ProfilePictureUploadCountToday { get; set; } = 0;
    }
}
