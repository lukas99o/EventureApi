using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using Vänskap_Api.Models;

namespace Vänskap_Api.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Event> Events { get; set; }
        public DbSet<EventParticipant> EventParticipants { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<Interest> Interests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Conversation>()
                .HasOne(c => c.Event)          
                .WithOne(e => e.Conversation)  
                .HasForeignKey<Conversation>(c => c.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Friendship>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Friendship>()
                .HasOne(f => f.Friend)
                .WithMany()
                .HasForeignKey(f => f.FriendId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FriendRequest>()
                .HasOne(f => f.Sender)
                .WithMany()
                .HasForeignKey(f => f.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FriendRequest>()
                .HasOne(f => f.Receiver)
                .WithMany()
                .HasForeignKey(f => f.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<EventParticipant>()
                .HasOne(ep => ep.User)
                .WithMany(u => u.EventParticipations)
                .HasForeignKey(ep => ep.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<EventParticipant>()
                .HasOne(ep => ep.Event)
                .WithMany(e => e.EventParticipants)
                .HasForeignKey(ep => ep.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserInterest
            builder.Entity<UserInterest>()
                .HasKey(ui => new { ui.UserId, ui.InterestId });

            builder.Entity<UserInterest>()
                .HasOne(ui => ui.User)
                .WithMany(u => u.UserInterests)
                .HasForeignKey(ui => ui.UserId);

            builder.Entity<UserInterest>()
                .HasOne(ui => ui.Interest)
                .WithMany()
                .HasForeignKey(ui => ui.InterestId);

            // EventInterest
            builder.Entity<EventInterest>()
                .HasKey(ei => new { ei.EventId, ei.InterestId });

            builder.Entity<EventInterest>()
                .HasOne(ei => ei.Event)
                .WithMany(e => e.EventInterests)
                .HasForeignKey(ei => ei.EventId);

            builder.Entity<EventInterest>()
                .HasOne(ei => ei.Interest)
                .WithMany()
                .HasForeignKey(ei => ei.InterestId);

            builder.Entity<Interest>().HasData(
                new Interest { Id = 1, Name = "Cooking" },
                new Interest { Id = 2, Name = "Travel" },
                new Interest { Id = 3, Name = "Photography" },
                new Interest { Id = 4, Name = "Fitness" },
                new Interest { Id = 5, Name = "Running" },
                new Interest { Id = 6, Name = "Hiking" },
                new Interest { Id = 7, Name = "Cycling" },
                new Interest { Id = 8, Name = "Swimming" },
                new Interest { Id = 9, Name = "Yoga" },
                new Interest { Id = 10, Name = "Music" },
                new Interest { Id = 11, Name = "Dance" },
                new Interest { Id = 12, Name = "Painting" },
                new Interest { Id = 13, Name = "Drawing" },
                new Interest { Id = 14, Name = "Writing" },
                new Interest { Id = 15, Name = "Reading Books" },
                new Interest { Id = 16, Name = "Playing Guitar" },
                new Interest { Id = 17, Name = "Playing Piano" },
                new Interest { Id = 18, Name = "Programming" },
                new Interest { Id = 19, Name = "Gardening" },
                new Interest { Id = 20, Name = "Fishing" },
                new Interest { Id = 21, Name = "Hunting" },
                new Interest { Id = 22, Name = "Baking" },
                new Interest { Id = 23, Name = "Fashion" },
                new Interest { Id = 24, Name = "Interior Design" },
                new Interest { Id = 25, Name = "Movies" },
                new Interest { Id = 26, Name = "TV Series" },
                new Interest { Id = 27, Name = "Podcasts" },
                new Interest { Id = 28, Name = "Cars" },
                new Interest { Id = 29, Name = "Motorcycles" },
                new Interest { Id = 30, Name = "Animals" },
                new Interest { Id = 31, Name = "Dogs" },
                new Interest { Id = 32, Name = "Cats" },
                new Interest { Id = 33, Name = "Volunteering" },
                new Interest { Id = 34, Name = "Stocks" },
                new Interest { Id = 35, Name = "Investing" },
                new Interest { Id = 36, Name = "Economics" },
                new Interest { Id = 37, Name = "History" },
                new Interest { Id = 38, Name = "Psychology" },
                new Interest { Id = 39, Name = "Philosophy" },
                new Interest { Id = 40, Name = "Astronomy" },
                new Interest { Id = 41, Name = "Science" },
                new Interest { Id = 42, Name = "Politics" },
                new Interest { Id = 43, Name = "Environmental Issues" },
                new Interest { Id = 44, Name = "Debate" },
                new Interest { Id = 45, Name = "Self-Development" },
                new Interest { Id = 46, Name = "Meditation" },
                new Interest { Id = 47, Name = "Mindfulness" },
                new Interest { Id = 48, Name = "Skiing" },
                new Interest { Id = 49, Name = "Snowboarding" },
                new Interest { Id = 50, Name = "Sailing" },
                new Interest { Id = 51, Name = "Surfing" },
                new Interest { Id = 52, Name = "Golf" },
                new Interest { Id = 53, Name = "Football" },
                new Interest { Id = 54, Name = "Basketball" },
                new Interest { Id = 55, Name = "Tennis" },
                new Interest { Id = 56, Name = "Padel" },
                new Interest { Id = 57, Name = "Baseball" },
                new Interest { Id = 58, Name = "Esports" },
                new Interest { Id = 59, Name = "Board Games" },
                new Interest { Id = 60, Name = "Chess" },
                new Interest { Id = 61, Name = "Card Games" },
                new Interest { Id = 62, Name = "Role-Playing Games" },
                new Interest { Id = 63, Name = "Camping" },
                new Interest { Id = 64, Name = "Road Trips" },
                new Interest { Id = 65, Name = "Backpacking" },
                new Interest { Id = 66, Name = "Languages" },
                new Interest { Id = 67, Name = "Culture" },
                new Interest { Id = 68, Name = "Food Culture" },
                new Interest { Id = 69, Name = "Brewing Beer" },
                new Interest { Id = 70, Name = "Wine Tasting" },
                new Interest { Id = 71, Name = "Cocktails" },
                new Interest { Id = 72, Name = "Coffee" },
                new Interest { Id = 73, Name = "Technology" },
                new Interest { Id = 74, Name = "AI" },
                new Interest { Id = 75, Name = "Game Development" },
                new Interest { Id = 76, Name = "Web Development" },
                new Interest { Id = 77, Name = "Mobile Apps" },
                new Interest { Id = 78, Name = "Entrepreneurship" },
                new Interest { Id = 79, Name = "Startups" },
                new Interest { Id = 80, Name = "Marketing" },
                new Interest { Id = 81, Name = "Social Media" },
                new Interest { Id = 82, Name = "YouTube" },
                new Interest { Id = 83, Name = "Streaming" },
                new Interest { Id = 84, Name = "Stand-up Comedy" },
                new Interest { Id = 85, Name = "Improvisation" },
                new Interest { Id = 86, Name = "Acting" },
                new Interest { Id = 87, Name = "Theatre" },
                new Interest { Id = 88, Name = "Art" },
                new Interest { Id = 89, Name = "Museums" },
                new Interest { Id = 90, Name = "Architecture" },
                new Interest { Id = 91, Name = "Fashion Photography" },
                new Interest { Id = 92, Name = "Vintage" },
                new Interest { Id = 93, Name = "Antiques" },
                new Interest { Id = 94, Name = "Flea Markets" },
                new Interest { Id = 95, Name = "Minimalism" },
                new Interest { Id = 96, Name = "Zero Waste" },
                new Interest { Id = 97, Name = "DIY Projects" },
                new Interest { Id = 98, Name = "Woodworking" },
                new Interest { Id = 99, Name = "Ceramics" },
                new Interest { Id = 100, Name = "Origami" }
            );
        }
    }
}
