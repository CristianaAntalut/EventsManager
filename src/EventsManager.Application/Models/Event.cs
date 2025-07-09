using System.Text.Json.Serialization;

namespace EventsManager.Application.Models;

public class Event
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string EventId { get; set; }
    public string Title { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
