using Famick.HomeManagement.Core.DTOs.Calendar;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Web.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Famick.HomeManagement.Web.Shared.Controllers.v1;

/// <summary>
/// API controller for managing calendar events with recurrence support
/// </summary>
[ApiController]
[Route("api/v1/calendar")]
[Authorize]
public class CalendarController : ApiControllerBase
{
    private readonly ICalendarEventService _calendarEventService;

    public CalendarController(
        ICalendarEventService calendarEventService,
        ITenantProvider tenantProvider,
        ILogger<CalendarController> logger)
        : base(tenantProvider, logger)
    {
        _calendarEventService = calendarEventService;
    }

    #region Calendar Events

    /// <summary>
    /// Gets expanded calendar occurrences within a date range
    /// </summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(List<CalendarOccurrenceDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] CalendarEventFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting calendar events for tenant {TenantId} from {Start} to {End}",
            TenantId, filter.StartDate, filter.EndDate);

        var occurrences = await _calendarEventService.GetCalendarEventsAsync(filter, cancellationToken);

        return ApiResponse(occurrences);
    }

    /// <summary>
    /// Gets a single calendar event by ID with full details
    /// </summary>
    [HttpGet("events/{id}")]
    [ProducesResponseType(typeof(CalendarEventDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetEvent(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting calendar event {EventId} for tenant {TenantId}", id, TenantId);

        var calendarEvent = await _calendarEventService.GetCalendarEventAsync(id, cancellationToken);

        if (calendarEvent == null)
        {
            return NotFoundResponse("Calendar event not found");
        }

        return ApiResponse(calendarEvent);
    }

    /// <summary>
    /// Creates a new calendar event with members
    /// </summary>
    [HttpPost("events")]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(typeof(CalendarEventDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CreateEvent(
        [FromBody] CreateCalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return UnauthorizedResponse("User context not available");
        }

        _logger.LogInformation("Creating calendar event '{Title}' for tenant {TenantId} by user {UserId}",
            request.Title, TenantId, userId.Value);

        var calendarEvent = await _calendarEventService.CreateCalendarEventAsync(
            request, userId.Value, cancellationToken);

        return CreatedAtAction(nameof(GetEvent), new { id = calendarEvent.Id }, calendarEvent);
    }

    /// <summary>
    /// Updates a calendar event with scope support for recurring events
    /// </summary>
    [HttpPut("events/{id}")]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(typeof(CalendarEventDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateEvent(
        Guid id,
        [FromBody] UpdateCalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating calendar event {EventId} for tenant {TenantId} with scope {EditScope}",
            id, TenantId, request.EditScope);

        var calendarEvent = await _calendarEventService.UpdateCalendarEventAsync(id, request, cancellationToken);

        return ApiResponse(calendarEvent);
    }

    /// <summary>
    /// Deletes a calendar event with scope support for recurring events
    /// </summary>
    [HttpDelete("events/{id}")]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteEvent(
        Guid id,
        [FromBody] DeleteCalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting calendar event {EventId} for tenant {TenantId} with scope {EditScope}",
            id, TenantId, request.EditScope);

        await _calendarEventService.DeleteCalendarEventAsync(id, request, cancellationToken);

        return NoContent();
    }

    #endregion

    #region Dashboard

    /// <summary>
    /// Gets upcoming events for the dashboard widget
    /// </summary>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(List<CalendarOccurrenceDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetUpcomingEvents(
        [FromQuery] int days = 7,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting upcoming events for tenant {TenantId}, days={Days}, userId={UserId}",
            TenantId, days, userId);

        var occurrences = await _calendarEventService.GetUpcomingEventsAsync(days, userId, cancellationToken);

        return ApiResponse(occurrences);
    }

    #endregion

    private Guid? GetCurrentUserId()
    {
        return _tenantProvider.UserId;
    }
}
