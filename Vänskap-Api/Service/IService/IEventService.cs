using Vänskap_Api.Models.Dtos.Event;

namespace Vänskap_Api.Service.IService
{
    public interface IEventService
    {
        Task<(ReadEventDto?, string?)> CreateEvent(EventDto createEvent);
        Task<IEnumerable<ReadEventDto>> ReadAllPublicEvents(List<string?> interests, int? ageMin, int? ageMax);
        Task<IEnumerable<ReadEventDto>> GetAllFriendEvents();
        Task<ReadEventDto?> ReadEvent(int id);
        Task<(bool, string)> UpdateEvent(int id, EventDto updateEvent);
        Task<bool> JoinEvent(int id);
        Task<bool> LeaveEvent(int id);
        Task<bool> DeleteEvent(int id);
        Task<bool> HostDeleteEvent(int id); 
        Task<List<string>> GetInterests();
        Task<List<int>> EventPartcipantStatus();
        Task<IEnumerable<ReadEventDto>> GetMyCreatedEvents();
        Task<IEnumerable<ReadEventDto>> GetMyJoinedEvents();
        Task<IEnumerable<ReadEventDto>> GetUnjoinedEvents(List<string?> interests, int? ageMin, int? ageMax);
        //Task<bool> SendMessage(int id, string text);
        //Task<IEnumerable<EventReceiveMessageDto>> GetEventMessages(int id);
    }
}
