using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Extensions.NETCore.Setup;
using EventsManager.Application.Models;

namespace EventsManager.Application.Services;
public class EventAttendeeRepository : IEventAttendeeRepository
{
    private const string InvertedIndex = "UserId-EventId-index";
    private readonly IDynamoDBContext _context;
    private readonly string _tableName;

    public EventAttendeeRepository(IAmazonDynamoDB client, IDynamoDBContext context, AWSOptions awsOptions)
    {
        _context = context;
        var region = awsOptions.Region.SystemName;
        _tableName = $"EventAttendees-{region}";
    }

    public async Task<EventAttendee> GetAsync(string eventId, string userId)
    {
        var operationConfig = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName
        };

        return await _context.LoadAsync<EventAttendee>(eventId, userId, operationConfig);
    }

    public async Task SaveAsync(EventAttendee attendee)
    {
        var operationConfig = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName
        };
        await _context.SaveAsync(attendee, operationConfig);
    }
    public async Task DeleteAsync(List<EventAttendee> attendees)
    {
        var operationConfig = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName
        };
        foreach(var attendee in attendees)
        {
            await _context.DeleteAsync<EventAttendee>(attendee.EventId,attendee.UserId, operationConfig);
        }
    }

    public async Task DeleteByPairIdsAsync(string eventId, string userId)
    {
        var operationConfig = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName
        };
        await _context.DeleteAsync<EventAttendee>(eventId, userId, operationConfig);
    }

    public async Task<List<EventAttendee>> QueryByEventIdAsync(string eventId)
    {
        var operationConfig = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName
        };
        var conditions = _context.QueryAsync<EventAttendee>(eventId, operationConfig);
        return await conditions.GetRemainingAsync();
    }

    public async Task<List<EventAttendee>> QueryByUserIdAsync(string userId)
    {
        var operationConfig = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName,
            IndexName = InvertedIndex
        };
        var conditions = _context.QueryAsync<EventAttendee>(userId, operationConfig);
        return await conditions.GetRemainingAsync();
    }
}
