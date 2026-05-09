using System.ComponentModel.DataAnnotations;

namespace ADET_Group_12.Models;

public enum CustomerCategory
{
    Regular,
    SeniorCitizen,
    Pwd,
    Pregnant,
    Emergency
}

public enum QueueTicketStatus
{
    Scheduled,
    Waiting,
    Called,
    Completed,
    Cancelled
}

public sealed class ServiceOption
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string CounterName { get; init; }
    public int AverageMinutes { get; init; }
}

public sealed class QueueTicket
{
    public int Id { get; init; }
    public required string Number { get; init; }
    public required string CustomerName { get; init; }
    public string? ContactNumber { get; init; }
    public required string ServiceCode { get; init; }
    public required string ServiceName { get; init; }
    public int ServiceMinutes { get; init; }
    public CustomerCategory Category { get; init; }
    public QueueTicketStatus Status { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public DateTime? CalledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CounterName { get; set; }

    public bool IsPriority => Category is CustomerCategory.SeniorCitizen
        or CustomerCategory.Pwd
        or CustomerCategory.Pregnant
        or CustomerCategory.Emergency;
}

public class TicketInput
{
    [Required]
    [StringLength(80)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(30)]
    public string? ContactNumber { get; set; }

    [Required]
    public string ServiceCode { get; set; } = "GEN";

    public CustomerCategory Category { get; set; } = CustomerCategory.Regular;
}

public sealed class AppointmentInput : TicketInput
{
    [Required]
    public DateTime AppointmentAt { get; set; }
}

public sealed class TicketDisplay
{
    public int Id { get; init; }
    public required string Number { get; init; }
    public required string CustomerName { get; init; }
    public string? ContactNumber { get; init; }
    public required string ServiceName { get; init; }
    public required string ServiceCode { get; init; }
    public required string CategoryLabel { get; init; }
    public bool IsPriority { get; init; }
    public required string StatusLabel { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public DateTime? CalledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? CounterName { get; init; }
    public int Position { get; init; }
    public int EstimatedWaitMinutes { get; init; }
}

public sealed class SmartQDashboardViewModel
{
    public required IReadOnlyList<ServiceOption> Services { get; init; }
    public required IReadOnlyList<TicketDisplay> WaitingTickets { get; init; }
    public required IReadOnlyList<TicketDisplay> ActiveTickets { get; init; }
    public required IReadOnlyList<TicketDisplay> Appointments { get; init; }
    public required IReadOnlyList<TicketDisplay> QueueHistory { get; init; }
    public TicketDisplay? NextTicket { get; init; }
    public int WaitingCount { get; init; }
    public int PriorityCount { get; init; }
    public int ActiveCount { get; init; }
    public int CompletedTodayCount { get; init; }
    public int AverageWaitMinutes { get; init; }
    public DateTime GeneratedAt { get; init; }
    public string? FlashMessage { get; init; }
    public string FlashTone { get; init; } = "info";
}

public static class SmartQLabels
{
    public static string ToDisplayName(this CustomerCategory category) => category switch
    {
        CustomerCategory.SeniorCitizen => "Senior citizen",
        CustomerCategory.Pwd => "PWD",
        CustomerCategory.Pregnant => "Pregnant",
        CustomerCategory.Emergency => "Emergency",
        _ => "Regular"
    };

    public static string ToDisplayName(this QueueTicketStatus status) => status switch
    {
        QueueTicketStatus.Scheduled => "Scheduled",
        QueueTicketStatus.Waiting => "Waiting",
        QueueTicketStatus.Called => "Now serving",
        QueueTicketStatus.Completed => "Completed",
        QueueTicketStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };
}
