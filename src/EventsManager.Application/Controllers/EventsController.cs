using Amazon.S3;
using Amazon.S3.Model;
using EventsManager.Application.Config;
using EventsManager.Application.ExtensionManager;
using EventsManager.Application.Models;
using EventsManager.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EventsManager.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{

    private readonly IAmazonS3 _s3Client;
    private readonly IEventAttendeeRepository _repository;
    private readonly BucketConfig _bucketConfig;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IAmazonS3 s3Client, IEventAttendeeRepository repository, BucketConfig bucketConfig, ILogger<EventsController> logger)
    {
        _s3Client = s3Client;
        _repository = repository;
        _bucketConfig = bucketConfig;
        _logger = logger;
    }


    /// <summary>
    /// GET /api/events: Lists all events stored in the S3 bucket with the specified prefix.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListEvents()
    {
        _logger.LogInformation("Listing events from bucket: {BucketName} with prefix: {Prefix}", _bucketConfig.BucketName, _bucketConfig.Prefix);
        var request = new ListObjectsV2Request
        {
            BucketName = _bucketConfig.BucketName,
            Prefix = _bucketConfig.Prefix
        };

        var response = await _s3Client.ListObjectsV2Async(request);
        var eventIds = response.S3Objects?
            .Select(obj => Path.GetFileNameWithoutExtension(obj.Key))
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        return Ok(eventIds);
    }

    /// <summary>
    ///  GET /api/events/{id}: Retrieves a specific event by its ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(string id)
    {
        var key = $"{_bucketConfig.Prefix}/{id}.json";
        try
        {
            var response = await _s3Client.GetObjectAsync(_bucketConfig.BucketName, key);
            using var reader = new StreamReader(response.ResponseStream);
            var content = await reader.ReadToEndAsync();
            return Ok(JsonSerializer.Deserialize<object>(content));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound($"Event '{id}' not found.");
        }
    }

    /// <summary>
    ///  GET /api/events/eventId/attendees: Retrieves a list of attendees for a specific event by its ID.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("{eventId}/attendees")]
    public async Task<IActionResult> GetAttendeesToEvent(string eventId)
    {
        var key = $"{_bucketConfig.Prefix}/{eventId}.json";
        try
        {
            var attendees = await _repository.QueryByEventIdAsync(eventId);

            return Ok(attendees);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound($"Event '{eventId}' not found.");
        }
    }

    /// <summary>
    ///  POST /api/events/{eventId}/attendees: Adds an attendee to a specific event by IDs.
    /// </summary>
    [Authorize]
    [HttpPost("{eventId}/attendees")]
    public async Task<IActionResult> CreateAttendeeToEvent(string eventId, string userId)
    {
        if (!this.CanUserAccessWitProvidedData(userId))
        {
            return Unauthorized("You cannot access this data.");
        }

        await _repository.SaveAsync(new EventAttendee
        {
            EventId = eventId,
            UserId = userId,
            RegistrationDate = DateTime.UtcNow.ToString("o")
        });

        return Accepted();
    }

    /// <summary>
    ///  DELETE /api/events/{eventId}/attendees: Removes all attendees from a specific event by its ID.
    /// </summary>
    [Authorize("Admin")]
    [HttpDelete("{eventId}/attendees")]
    public async Task<IActionResult> RemoveAttendeesFromEvent(string eventId)
    {
        if (!this.CanUserAccessWitProvidedData(string.Empty))
        {
            return Unauthorized("You cannot access this data.");
        }

        var attendeesToRemove = await _repository.QueryByEventIdAsync(eventId);
        await _repository.DeleteAsync(attendeesToRemove);
        return Accepted();
    }

    /// <summary>
    /// DELETE /api/events/{eventId}/attendees/{userId}: Removes a specific attendee from an event by event ID and user ID.
    /// </summary>
    [Authorize]
    [HttpDelete("{eventId}/attendees/{userId}")]
    public async Task<IActionResult> RemoveAttendeeFromEvent(string eventId, string userId)
    {
        if (!this.CanUserAccessWitProvidedData(userId))
        {
            return Unauthorized("You cannot access this data.");
        }

        await _repository.DeleteByPairIdsAsync(eventId, userId);
        return Accepted();
    }

    /// <summary>
    /// POST /api/events: Creates a new event with a unique ID and stores it in the S3 bucket.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] Event eventData)
    {
        var eventId = Guid.NewGuid().ToString();
        eventData.EventId = eventId;
        var json = JsonSerializer.Serialize(eventData);
        var key = $"{_bucketConfig.Prefix}/{eventId}.json";

        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketConfig.BucketName,
            Key = key,
            ContentBody = json
        });

        return CreatedAtAction(nameof(GetEvent), new { id = eventId }, new { id = eventId });
    }

    /// <summary>
    /// PUT /api/events/{id}: Updates an existing event by its ID.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(string id, [FromBody] Event eventData)
    {
        var json = JsonSerializer.Serialize(eventData);
        var key = $"{_bucketConfig.Prefix}/{id}.json";

        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketConfig.BucketName,
            Key = key,
            ContentBody = json
        });

        return Ok();
    }

    /// <summary>
    /// DELETE /api/events/{id}: Deletes an event by its ID and removes all associated attendees.
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(string id)
    {
        var key = $"{_bucketConfig.Prefix}/{id}.json";

        await _s3Client.DeleteObjectAsync(_bucketConfig.BucketName, key);

        var attendeesToRemove = await _repository.QueryByEventIdAsync(id);
        await _repository.DeleteAsync(attendeesToRemove);
        return NoContent();
    }
}