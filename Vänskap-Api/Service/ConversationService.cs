using Microsoft.EntityFrameworkCore;
using Vänskap_Api.Models;
using Vänskap_Api.Service.IService;
using System.Security.Claims;
using Vänskap_Api.Data;
using Vänskap_Api.Models.Dtos.Conversation;
using System.Runtime.InteropServices;

namespace Vänskap_Api.Service
{
    public class ConversationService : IConversationService 
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _contextAccessor;
        public string UserId => _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new ArgumentNullException(nameof(UserId));

        public ConversationService(ApplicationDbContext context, IHttpContextAccessor contextAccessor)
        {
            _context = context;
            _contextAccessor = contextAccessor;
        }

        public async Task<SeeConversationDto?> StartPrivateConversation(string username)
        {
            var user = await _context.Users
                .Include(f => f.Friendships)
                .ThenInclude(f => f.Friend)
                .SingleOrDefaultAsync(u => u.Id == UserId);

            var friend = await _context.Users
                .Include(f => f.Friendships)
                .ThenInclude(f => f.Friend)
                .SingleOrDefaultAsync(u => u.UserName == username);

            if (user == null || friend == null)
                return null;

            var areFriends = await _context.Friendships.AnyAsync(fs =>
                (fs.UserId == user.Id && fs.FriendId == friend.Id) ||
                (fs.UserId == friend.Id && fs.FriendId == user.Id)
            );

            if (!areFriends)
                return null;

            var existingConversation = await _context.Conversations
                .Include(c => c.ConversationParticipants)
                .Include(c => c.Messages)
                .ThenInclude(m => m.Sender)
                .SingleOrDefaultAsync(c =>
                    c.ConversationParticipants.Count == 2 &&
                    c.ConversationParticipants.Any(cp => cp.UserId == user.Id) &&
                    c.ConversationParticipants.Any(cp => cp.UserId == friend.Id) &&
                    c.EventId == null
                );

            if (existingConversation != null)
            {
                return new SeeConversationDto
                {
                    ConversationId = existingConversation.Id,
                    Title = existingConversation.Title,
                    Messages = existingConversation.Messages.Select(m => new ConversationMessageDto
                    {
                        MessageId = m.Id,
                        Content = m.Content,
                        SenderId = m.SenderId,
                        CreatedAt = m.CreatedAt,
                        SenderName = m.Sender != null ? m.Sender.UserName : "[Unknown user]"
                    }),
                    CreatedAt = existingConversation.CreatedAt,
                    ConversationParticipants = existingConversation.ConversationParticipants.Select(cp => new ConversationParticipantDto
                    {
                        UserId = cp.UserId,
                        ConversationId = cp.ConversationId
                    })
                };
            }

            var conversation = new Conversation
            {
                Title = $"{user.FirstName}, {friend.FirstName}"
            };

            conversation.ConversationParticipants.Add(new ConversationParticipant { UserId = user.Id });
            conversation.ConversationParticipants.Add(new ConversationParticipant { UserId = friend.Id });

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return new SeeConversationDto
            {
                ConversationId = conversation.Id,
                Title = conversation.Title,
                Messages = Array.Empty<ConversationMessageDto>(),
                CreatedAt = conversation.CreatedAt,
                ConversationParticipants = conversation.ConversationParticipants.Select(cp => new ConversationParticipantDto
                {
                    UserId = cp.UserId,
                    ConversationId = cp.ConversationId
                })
            };
        }

        public async Task<IEnumerable<SeeConversationDto>> SeeAllConversations()
        {
            var userConversations = await _context.Conversations
                .Where(c => c.ConversationParticipants.Any(cp => cp.UserId == UserId))
                .Include(c => c.Messages)
                    .ThenInclude(m => m.Sender)
                .Include(c => c.ConversationParticipants)
                .ToListAsync();

            var conversationDtos = userConversations.Select(conversation => new SeeConversationDto
            {
                ConversationId = conversation.Id,
                Title = conversation.Title,
                Messages = conversation.Messages.Select(m => new ConversationMessageDto
                {
                    MessageId = m.Id,
                    Content = m.Content,
                    SenderId = m.SenderId,
                    CreatedAt = m.CreatedAt,
                    SenderName = m.Sender != null ? m.Sender.UserName : "[Unknown user]"
                }),
                CreatedAt = conversation.CreatedAt,
                ConversationParticipants = conversation.ConversationParticipants.Select(cp => new ConversationParticipantDto
                {
                    UserId = cp.UserId,
                    ConversationId = cp.ConversationId
                })
            });

            return conversationDtos;
        }

        public async Task<SeeConversationDto?> SeeConversation(int id)
        {
            var conversation = await _context.Conversations
                .Where(c => c.ConversationParticipants.Any(cp => cp.UserId == UserId) && c.Id == id)
                .Include(c => c.Messages)
                .SingleOrDefaultAsync(c => c.Id == id);

            if (conversation == null) return null;

            var seeConversation = new SeeConversationDto
            {
                ConversationId = conversation.Id,
                Title = conversation.Title,
                Messages = conversation.Messages.Select(m => new ConversationMessageDto
                {
                    MessageId = m.Id,
                    Content = m.Content,
                    SenderId = m.SenderId,
                    CreatedAt = m.CreatedAt,
                    SenderName = m.Sender != null ? m.Sender.UserName : "[Unknown user]"
                }),
                CreatedAt = conversation.CreatedAt,
                ConversationParticipants = conversation.ConversationParticipants.Select(cp => new ConversationParticipantDto
                {
                    UserId = cp.UserId,
                    ConversationId = cp.ConversationId
                })
            };

            return seeConversation;
        }

        public async Task<IEnumerable<ConversationMessageDto>> GetConversationMessages(int id)
        {
            var conversation = await _context.Conversations
                .Where(c => c.ConversationParticipants.Any(cp => cp.UserId == UserId) && c.Id == id)
                .Include(c => c.Messages)
                    .ThenInclude(m => m.Sender) 
                .SingleOrDefaultAsync(c => c.Id == id);

            if (conversation == null)
                return Enumerable.Empty<ConversationMessageDto>();

            var conversationMessagesDto = conversation.Messages
                .OrderBy(m => m.CreatedAt) 
                .Select(m => new ConversationMessageDto
                {
                    MessageId = m.Id,
                    Content = m.Content,
                    SenderId = m.SenderId,
                    CreatedAt = m.CreatedAt,
                    SenderName = m.Sender != null ? m.Sender.UserName : "[Unknown user]"
                });

            return conversationMessagesDto;
        }

        public async Task<bool> EditConversationTitle(int id, string title)
        {
            var conversation = await _context.Conversations
                .Where(c => c.ConversationParticipants.Any(cp => cp.UserId == UserId ) && c.Id == id)
                .Include(c => c.ConversationParticipants)
                .SingleOrDefaultAsync(c => c.Id == id);

            if (conversation == null) return false;

            var user = conversation.ConversationParticipants.SingleOrDefault(cp => cp.UserId == UserId);
            if (user == null) return false;

            if (user.Role != "Host") return false;

            conversation.Title = title;
            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> EditRoles(int id, List<EditRoleDto> namesAndRoles)
        {
            var conversation = await _context.Conversations
                .Where(c => c.ConversationParticipants.Any(cp => cp.UserId == UserId) && c.Id == id)
                .Include(c => c.ConversationParticipants)
                .ThenInclude(cp => cp.User)
                .SingleOrDefaultAsync();

            if (conversation == null) return false;

            var host = conversation.ConversationParticipants.SingleOrDefault(cp => cp.UserId == UserId);
            if (host == null || host.Role != "Host") return false;

            foreach (var dto in namesAndRoles)
            {
                if (dto.Role != "Host" || dto.Role != "Participant") continue;

                var participant = conversation.ConversationParticipants
                    .SingleOrDefault(cp => cp.User != null && cp.User.UserName == dto.UserName);

                if (participant != null)
                {
                    participant.Role = dto.Role;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveConversationParticipants(int id, List<string> userNames)
        {
            var conversation = await _context.Conversations
                .Where(c => c.ConversationParticipants.Any(cp => cp.UserId == UserId) && c.Id == id)
                .Include(c => c.ConversationParticipants)
                .ThenInclude(cp => cp.User != null ? cp.User.UserName : "[Unknown user]") 
                .SingleOrDefaultAsync(c => c.Id == id);

            if (conversation == null) return false;

            var participantsToRemove = conversation.ConversationParticipants
                .Where(cp => cp.User != null && userNames.Contains(cp.User.UserName!))
                .ToList();

            if (!participantsToRemove.Any()) return false;

            _context.RemoveRange(participantsToRemove);

            if (conversation.ConversationParticipants.Count() < 3) return false;

            await _context.SaveChangesAsync();
            return true;
        }

        // The second paramter is all ursers that the host would like to make host if the user is the last host left
        public async Task<bool> RemoveUrselfFromConversation(int id, List<string?> userNames)
        {
            var conversation = await _context.Conversations
                .Where(c => c.ConversationParticipants.Any(cp => cp.UserId == UserId) && c.Id == id)
                .Include(c => c.ConversationParticipants)
                .SingleOrDefaultAsync();

            if (conversation == null) return false;
            var user = await _context.ConversationParticipants.SingleOrDefaultAsync(u => u.UserId == UserId);
            if (user == null) return false;
            var hosts = conversation.ConversationParticipants.Where(cp => cp.Role == "Host").ToList();
            var participants = conversation.ConversationParticipants.Where(cp => cp.Role == "Participant").ToList();
            var newHosts = new List<ConversationParticipant>();

            foreach (var participant in participants)
            {
                if (userNames.Contains(participant.User?.UserName)) newHosts.Add(participant);
            }

            if (hosts.Count() == 1)
            {
                foreach (var participant in newHosts)
                {
                    participant.Role = "Host";
                }

                _context.ConversationParticipants.Remove(user);

                if (conversation.ConversationParticipants.Count() < 1)
                {
                    _context.Conversations.Remove(conversation);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            else
            {
                _context.ConversationParticipants.Remove(user);

                if (conversation.ConversationParticipants.Count() < 1)
                {
                    _context.Conversations.Remove(conversation);
                }

                await _context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> SendMessage(string content, int id)
        {
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == id && cp.UserId == UserId);

            if (!isParticipant) return false;

            var message = new Message() { Content = content, ConversationId = id, SenderId = UserId };
            await _context.AddAsync(message);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
