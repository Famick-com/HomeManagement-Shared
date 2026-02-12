using Famick.HomeManagement.Domain.Enums;

namespace Famick.HomeManagement.Core.DTOs.Calendar;

/// <summary>
/// DTO representing a member's participation in a calendar event.
/// </summary>
public class CalendarEventMemberDto
{
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public ParticipationType ParticipationType { get; set; }
}
