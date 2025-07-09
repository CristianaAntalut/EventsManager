using EventsManager.Application.Models;

namespace EventsManager.Application.Services;

public interface IEventAttendeeRepository
{
    Task<EventAttendee> GetAsync(string eventId, string userId);
    Task SaveAsync(EventAttendee attendee);
    Task DeleteAsync(List<EventAttendee> attendees);
    Task DeleteByPairIdsAsync(string eventId, string userId);
    Task<List<EventAttendee>> QueryByEventIdAsync(string eventId);
    Task<List<EventAttendee>> QueryByUserIdAsync(string userId);
}