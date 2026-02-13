using Famick.HomeManagement.Core.DTOs.Calendar;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Domain.Enums;
using Famick.HomeManagement.Infrastructure.Data;
using Ical.Net;
using Ical.Net.DataTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

public class CalendarAvailabilityService : ICalendarAvailabilityService
{
    private readonly HomeManagementDbContext _context;
    private readonly ILogger<CalendarAvailabilityService> _logger;

    public CalendarAvailabilityService(
        HomeManagementDbContext context,
        ILogger<CalendarAvailabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<FreeBusyDto>> GetFreeBusyAsync(
        List<Guid> userIds,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting free/busy for {UserCount} user(s) from {Start} to {End}",
            userIds.Count, startDate, endDate);

        var results = new List<FreeBusyDto>();

        foreach (var userId in userIds)
        {
            var busySlots = await GetBusySlotsForUserAsync(userId, startDate, endDate, cancellationToken);
            var merged = MergeOverlappingSlots(busySlots);

            // Get user display name
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            var displayName = user != null
                ? $"{user.FirstName} {user.LastName}".Trim()
                : "Unknown";

            results.Add(new FreeBusyDto
            {
                UserId = userId,
                UserDisplayName = displayName,
                BusySlots = merged
            });
        }

        return results;
    }

    public async Task<List<AvailableSlotDto>> FindAvailableSlotsAsync(
        FindSlotsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Finding available slots for {UserCount} user(s), duration={Duration}min, from {Start} to {End}",
            request.UserIds.Count, request.DurationMinutes, request.StartDate, request.EndDate);

        // Get merged busy slots for ALL users combined
        var allBusySlots = new List<TimeSlotDto>();

        foreach (var userId in request.UserIds)
        {
            var userSlots = await GetBusySlotsForUserAsync(
                userId, request.StartDate, request.EndDate, cancellationToken);
            allBusySlots.AddRange(userSlots);
        }

        var mergedBusy = MergeOverlappingSlots(allBusySlots);
        var duration = TimeSpan.FromMinutes(request.DurationMinutes);

        // Find gaps between busy slots that fit the requested duration
        var availableSlots = new List<AvailableSlotDto>();
        var searchStart = request.StartDate;

        // Add a sentinel at the end
        var busyWithSentinel = mergedBusy
            .OrderBy(s => s.StartTimeUtc)
            .Append(new TimeSlotDto { StartTimeUtc = request.EndDate, EndTimeUtc = request.EndDate })
            .ToList();

        foreach (var busy in busyWithSentinel)
        {
            if (availableSlots.Count >= request.MaxResults)
                break;

            var gapStart = searchStart;
            var gapEnd = busy.StartTimeUtc;

            // Find valid slots within this gap
            FindSlotsInGap(gapStart, gapEnd, duration, request, availableSlots);

            // Move past this busy period
            if (busy.EndTimeUtc > searchStart)
            {
                searchStart = busy.EndTimeUtc;
            }
        }

        return availableSlots.Take(request.MaxResults).ToList();
    }

    #region Private Methods

    private async Task<List<TimeSlotDto>> GetBusySlotsForUserAsync(
        Guid userId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var busySlots = new List<TimeSlotDto>();

        // 1. Get Famick calendar events where user is "Involved" (not "Aware")
        var calendarEvents = await _context.CalendarEvents
            .Include(e => e.Members)
            .Include(e => e.Exceptions)
            .Where(e => e.Members.Any(m => m.UserId == userId && m.ParticipationType == ParticipationType.Involved))
            .Where(e =>
                (string.IsNullOrEmpty(e.RecurrenceRule) && e.StartTimeUtc < endDate && e.EndTimeUtc > startDate) ||
                (!string.IsNullOrEmpty(e.RecurrenceRule) && e.StartTimeUtc < endDate &&
                 (!e.RecurrenceEndDate.HasValue || e.RecurrenceEndDate.Value > startDate)))
            .ToListAsync(cancellationToken);

        foreach (var evt in calendarEvents)
        {
            if (string.IsNullOrEmpty(evt.RecurrenceRule))
            {
                // Non-recurring event
                busySlots.Add(new TimeSlotDto
                {
                    StartTimeUtc = evt.StartTimeUtc,
                    EndTimeUtc = evt.EndTimeUtc,
                    Title = evt.Title
                });
            }
            else
            {
                // Expand recurring event
                var exceptions = evt.Exceptions.ToDictionary(ex => ex.OriginalStartTimeUtc, ex => ex);
                var eventDuration = evt.EndTimeUtc - evt.StartTimeUtc;

                var calendar = new Calendar();
                var icalEvent = new Ical.Net.CalendarComponents.CalendarEvent
                {
                    DtStart = new CalDateTime(evt.StartTimeUtc, "UTC"),
                    DtEnd = new CalDateTime(evt.EndTimeUtc, "UTC")
                };
                icalEvent.RecurrenceRules.Add(new RecurrencePattern(evt.RecurrenceRule));
                calendar.Events.Add(icalEvent);

                var occurrences = icalEvent.GetOccurrences(
                    new CalDateTime(startDate, "UTC"),
                    new CalDateTime(endDate, "UTC"));

                foreach (var occurrence in occurrences)
                {
                    var occStart = occurrence.Period.StartTime.AsUtc;

                    if (evt.RecurrenceEndDate.HasValue && occStart > evt.RecurrenceEndDate.Value)
                        break;

                    if (exceptions.TryGetValue(occStart, out var exception))
                    {
                        if (exception.IsDeleted) continue;

                        busySlots.Add(new TimeSlotDto
                        {
                            StartTimeUtc = exception.OverrideStartTimeUtc ?? occStart,
                            EndTimeUtc = exception.OverrideEndTimeUtc ?? (exception.OverrideStartTimeUtc ?? occStart).Add(eventDuration),
                            Title = exception.OverrideTitle ?? evt.Title
                        });
                    }
                    else
                    {
                        busySlots.Add(new TimeSlotDto
                        {
                            StartTimeUtc = occStart,
                            EndTimeUtc = occStart.Add(eventDuration),
                            Title = evt.Title
                        });
                    }
                }
            }
        }

        // 2. Get external calendar events (always count as busy)
        var externalEvents = await _context.ExternalCalendarEvents
            .Include(e => e.Subscription)
            .Where(e => e.Subscription!.IsActive)
            .Where(e => e.Subscription!.UserId == userId)
            .Where(e => e.StartTimeUtc < endDate && e.EndTimeUtc > startDate)
            .ToListAsync(cancellationToken);

        foreach (var ext in externalEvents)
        {
            busySlots.Add(new TimeSlotDto
            {
                StartTimeUtc = ext.StartTimeUtc,
                EndTimeUtc = ext.EndTimeUtc,
                Title = ext.Title
            });
        }

        return busySlots;
    }

    private static List<TimeSlotDto> MergeOverlappingSlots(List<TimeSlotDto> slots)
    {
        if (slots.Count == 0) return new List<TimeSlotDto>();

        var sorted = slots.OrderBy(s => s.StartTimeUtc).ToList();
        var merged = new List<TimeSlotDto> { sorted[0] };

        for (var i = 1; i < sorted.Count; i++)
        {
            var current = sorted[i];
            var last = merged[^1];

            if (current.StartTimeUtc <= last.EndTimeUtc)
            {
                // Overlapping or adjacent - extend the end time
                if (current.EndTimeUtc > last.EndTimeUtc)
                {
                    last.EndTimeUtc = current.EndTimeUtc;
                }
            }
            else
            {
                merged.Add(current);
            }
        }

        return merged;
    }

    private static void FindSlotsInGap(
        DateTime gapStart,
        DateTime gapEnd,
        TimeSpan duration,
        FindSlotsRequest request,
        List<AvailableSlotDto> results)
    {
        if (gapEnd <= gapStart) return;
        if (results.Count >= request.MaxResults) return;

        var candidate = gapStart;

        while (candidate.Add(duration) <= gapEnd && results.Count < request.MaxResults)
        {
            // Apply preferred hours filter if specified
            if (request.PreferredStartHourUtc.HasValue && candidate.Hour < request.PreferredStartHourUtc.Value)
            {
                // Skip to preferred start hour
                candidate = candidate.Date.AddHours(request.PreferredStartHourUtc.Value);
                if (candidate < gapStart) candidate = candidate.AddDays(1);
                continue;
            }

            if (request.PreferredEndHourUtc.HasValue && candidate.Hour >= request.PreferredEndHourUtc.Value)
            {
                // Skip to next day's preferred start hour
                var nextDay = candidate.Date.AddDays(1);
                candidate = request.PreferredStartHourUtc.HasValue
                    ? nextDay.AddHours(request.PreferredStartHourUtc.Value)
                    : nextDay;
                continue;
            }

            var slotEnd = candidate.Add(duration);

            // Ensure the slot end doesn't exceed preferred hours
            if (request.PreferredEndHourUtc.HasValue && slotEnd.Hour > request.PreferredEndHourUtc.Value
                && slotEnd.Date == candidate.Date)
            {
                // Slot would end after preferred hours, skip to next day
                var nextDay = candidate.Date.AddDays(1);
                candidate = request.PreferredStartHourUtc.HasValue
                    ? nextDay.AddHours(request.PreferredStartHourUtc.Value)
                    : nextDay;
                continue;
            }

            if (slotEnd <= gapEnd)
            {
                results.Add(new AvailableSlotDto
                {
                    StartTimeUtc = candidate,
                    EndTimeUtc = slotEnd
                });
            }

            // Move forward by the duration to find non-overlapping slots
            candidate = slotEnd;
        }
    }

    #endregion
}
