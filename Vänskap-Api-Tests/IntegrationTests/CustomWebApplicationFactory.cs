using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Vänskap_Api.Data;
using Vänskap_Api.Models;

namespace Vänskap_Api_Tests.IntegrationTests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        public string TestUserId { get; set; } = "test-user-id";
        public string TestUserName { get; set; } = "testuser";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Ta bort original DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Lägg till InMemory DbContext
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                // Konfigurera test-auth
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "TestScheme";
                    options.DefaultChallengeScheme = "TestScheme";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

                services.Configure<TestAuthOptions>(options =>
                {
                    options.UserId = TestUserId;
                    options.UserName = TestUserName;
                });

                // Seed databasen efter host byggts
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
                SeedTestData(db, TestUserId, TestUserName);
            });
        }

        private static void SeedTestData(ApplicationDbContext context, string testUserId, string testUserName)
        {
            if (context.Users.Any()) return; // Avoid double seeding

            var testUser = new ApplicationUser
            {
                Id = testUserId,
                UserName = testUserName,
                FirstName = "Test",
                LastName = "User",
                Email = "test@test.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };

            var otherUser = new ApplicationUser
            {
                Id = "other-user-id",
                UserName = "otheruser",
                FirstName = "Other",
                LastName = "User",
                Email = "other@test.com",
                DateOfBirth = new DateOnly(1985, 5, 15)
            };

            var friendUser = new ApplicationUser
            {
                Id = "friend-user-id",
                UserName = "frienduser",
                FirstName = "Friend",
                LastName = "User",
                Email = "friend@test.com",
                DateOfBirth = new DateOnly(1992, 3, 20)
            };

            context.Users.AddRange(testUser, otherUser, friendUser);

            context.Friendships.AddRange(
                new Friendship { UserId = testUserId, FriendId = "friend-user-id" },
                new Friendship { UserId = "friend-user-id", FriendId = testUserId }
            );

            context.Interests.AddRange(
                new Interest { Name = "Sports" },
                new Interest { Name = "Music" },
                new Interest { Name = "Art" },
                new Interest { Name = "Technology" }
            );

            context.SaveChanges();
        }
    }

    public class TestAuthOptions
    {
        public string UserId { get; set; } = "test-user-id";
        public string UserName { get; set; } = "testuser";
    }

    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly TestAuthOptions _options;

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptions<TestAuthOptions> testAuthOptions)
            : base(options, logger, encoder)
        {
            _options = testAuthOptions.Value;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, _options.UserId),
                new Claim(ClaimTypes.Name, _options.UserName),
                new Claim(ClaimTypes.Role, "User")
            };

            var identity = new ClaimsIdentity(claims, "TestScheme");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestScheme");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
