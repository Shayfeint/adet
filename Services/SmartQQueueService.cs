using ADET_Group_12.Models;

namespace ADET_Group_12.Services;

public sealed class SmartQQueueService
{
    private readonly object _sync = new();
    private readonly List<ServiceOption> _services =
    [
        new() { Code = "GEN", Name = "General assistance", CounterName = "Counter 1", AverageMinutes = 8 },
        new() { Code = "PAY", Name = "Payments and claims", CounterName = "Counter 2", AverageMinutes = 10 },
        new() { Code = "DOC", Name = "Documents and verification", CounterName = "Counter 3", AverageMinutes = 12 },
        new() { Code = "MED", Name = "Health consultation", CounterName = "Room A", AverageMinutes = 15 }
    ];

    private readonly List<QueueTicket> _tickets = [];
    private int _nextId = 1000;

    public SmartQQueueService()
    {
        SeedTickets();
    }

    public SmartQDashboardViewModel GetDashboard(string? flashMessage = null, string? flashTone = null)
    {
        lock (_sync)
        {
            var now = DateTime.Now;
            ActivateDueAppointments(now);

            var waitingTickets = OrderWaitingTickets(now).ToList();
            var waitingDisplays = BuildWaitingDisplays(waitingTickets, now);
            var activeDisplays = _tickets
                .Where(ticket => ticket.Status == QueueTicketStatus.Called)
                .OrderBy(ticket => ticket.CalledAt)
                .Select(ticket => BuildDisplay(ticket, 0, RemainingMinutes(ticket, now)))
                .ToList();
            var appointmentDisplays = _tickets
                .Where(ticket => ticket.Status == QueueTicketStatus.Scheduled)
                .OrderBy(ticket => ticket.ScheduledAt)
                .Select(ticket =>
                {
                    var minutesUntilAppointment = ticket.ScheduledAt.HasValue
                        ? Math.Max(0, (int)Math.Ceiling((ticket.ScheduledAt.Value - now).TotalMinutes))
                        : 0;

                    return BuildDisplay(ticket, 0, minutesUntilAppointment);
                })
                .ToList();
            var historyDisplays = _tickets
                .Where(ticket => ticket.Status is QueueTicketStatus.Completed or QueueTicketStatus.Cancelled)
                .OrderByDescending(ticket => ticket.CompletedAt ?? ticket.CreatedAt)
                .Take(10)
                .Select(ticket => BuildDisplay(ticket, 0, 0))
                .ToList();

            return new SmartQDashboardViewModel
            {
                Services = _services,
                WaitingTickets = waitingDisplays,
                ActiveTickets = activeDisplays,
                Appointments = appointmentDisplays,
                QueueHistory = historyDisplays,
                NextTicket = waitingDisplays.FirstOrDefault(),
                WaitingCount = waitingDisplays.Count,
                PriorityCount = waitingDisplays.Count(ticket => ticket.IsPriority),
                ActiveCount = activeDisplays.Count,
                CompletedTodayCount = _tickets.Count(ticket =>
                    ticket.Status == QueueTicketStatus.Completed &&
                    ticket.CompletedAt.HasValue &&
                    ticket.CompletedAt.Value.Date == now.Date),
                AverageWaitMinutes = waitingDisplays.Count == 0
                    ? 0
                    : (int)Math.Round(waitingDisplays.Average(ticket => ticket.EstimatedWaitMinutes)),
                GeneratedAt = now,
                FlashMessage = flashMessage,
                FlashTone = string.IsNullOrWhiteSpace(flashTone) ? "info" : flashTone
            };
        }
    }

    public QueueTicket CreateTicket(TicketInput input)
    {
        lock (_sync)
        {
            var service = FindService(input.ServiceCode);
            var ticket = NewTicket(input, service, DateTime.Now, QueueTicketStatus.Waiting, scheduledAt: null);
            _tickets.Add(ticket);

            return ticket;
        }
    }

    public QueueTicket BookAppointment(AppointmentInput input)
    {
        lock (_sync)
        {
            if (input.AppointmentAt < DateTime.Now.AddMinutes(5))
            {
                throw new InvalidOperationException("Choose an appointment at least 5 minutes from now.");
            }

            var service = FindService(input.ServiceCode);
            var ticket = NewTicket(input, service, DateTime.Now, QueueTicketStatus.Scheduled, input.AppointmentAt);
            _tickets.Add(ticket);

            return ticket;
        }
    }

    public QueueTicket? CallNext(string? serviceCode)
    {
        lock (_sync)
        {
            var now = DateTime.Now;
            ActivateDueAppointments(now);

            var nextTicket = OrderWaitingTickets(now)
                .FirstOrDefault(ticket =>
                    string.IsNullOrWhiteSpace(serviceCode) ||
                    string.Equals(ticket.ServiceCode, serviceCode, StringComparison.OrdinalIgnoreCase));

            if (nextTicket is null)
            {
                return null;
            }

            nextTicket.Status = QueueTicketStatus.Called;
            nextTicket.CalledAt = now;
            nextTicket.CounterName = FindService(nextTicket.ServiceCode).CounterName;

            return nextTicket;
        }
    }

    public bool CompleteTicket(int id)
    {
        lock (_sync)
        {
            var ticket = _tickets.FirstOrDefault(item => item.Id == id);
            if (ticket is null || ticket.Status != QueueTicketStatus.Called)
            {
                return false;
            }

            ticket.Status = QueueTicketStatus.Completed;
            ticket.CompletedAt = DateTime.Now;

            return true;
        }
    }

    public bool CancelTicket(int id)
    {
        lock (_sync)
        {
            var ticket = _tickets.FirstOrDefault(item => item.Id == id);
            if (ticket is null || ticket.Status is QueueTicketStatus.Completed or QueueTicketStatus.Cancelled)
            {
                return false;
            }

            ticket.Status = QueueTicketStatus.Cancelled;
            ticket.CompletedAt = DateTime.Now;

            return true;
        }
    }

    private QueueTicket NewTicket(
        TicketInput input,
        ServiceOption service,
        DateTime createdAt,
        QueueTicketStatus status,
        DateTime? scheduledAt)
    {
        if (string.IsNullOrWhiteSpace(input.CustomerName))
        {
            throw new InvalidOperationException("Customer name is required.");
        }

        var id = ++_nextId;
        return new QueueTicket
        {
            Id = id,
            Number = $"{service.Code}-{id}",
            CustomerName = input.CustomerName.Trim(),
            ContactNumber = string.IsNullOrWhiteSpace(input.ContactNumber) ? null : input.ContactNumber.Trim(),
            ServiceCode = service.Code,
            ServiceName = service.Name,
            ServiceMinutes = service.AverageMinutes,
            Category = input.Category,
            Status = status,
            CreatedAt = createdAt,
            ScheduledAt = scheduledAt
        };
    }

    private ServiceOption FindService(string? code)
    {
        var service = _services.FirstOrDefault(item =>
            string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));

        return service ?? throw new InvalidOperationException("Select a valid service.");
    }

    private void ActivateDueAppointments(DateTime now)
    {
        foreach (var ticket in _tickets.Where(ticket =>
            ticket.Status == QueueTicketStatus.Scheduled &&
            ticket.ScheduledAt <= now.AddMinutes(15)))
        {
            ticket.Status = QueueTicketStatus.Waiting;
        }
    }

    private IEnumerable<QueueTicket> OrderWaitingTickets(DateTime now)
    {
        return _tickets
            .Where(ticket => ticket.Status == QueueTicketStatus.Waiting)
            .OrderBy(ticket => PriorityRank(ticket))
            .ThenBy(ticket => ticket.ScheduledAt ?? ticket.CreatedAt)
            .ThenBy(ticket => ticket.Id);
    }

    private static int PriorityRank(QueueTicket ticket)
    {
        if (ticket.Category == CustomerCategory.Emergency)
        {
            return 0;
        }

        if (ticket.IsPriority)
        {
            return 1;
        }

        if (ticket.ScheduledAt.HasValue)
        {
            return 2;
        }

        return 3;
    }

    private List<TicketDisplay> BuildWaitingDisplays(IReadOnlyList<QueueTicket> waitingTickets, DateTime now)
    {
        var currentWaitMinutes = _tickets
            .Where(ticket => ticket.Status == QueueTicketStatus.Called)
            .Sum(ticket => RemainingMinutes(ticket, now));

        var displays = new List<TicketDisplay>();
        for (var index = 0; index < waitingTickets.Count; index++)
        {
            var ticket = waitingTickets[index];
            displays.Add(BuildDisplay(ticket, index + 1, currentWaitMinutes));
            currentWaitMinutes += ticket.ServiceMinutes;
        }

        return displays;
    }

    private static int RemainingMinutes(QueueTicket ticket, DateTime now)
    {
        if (!ticket.CalledAt.HasValue)
        {
            return ticket.ServiceMinutes;
        }

        var elapsedMinutes = (int)Math.Floor((now - ticket.CalledAt.Value).TotalMinutes);
        return Math.Max(1, ticket.ServiceMinutes - elapsedMinutes);
    }

    private static TicketDisplay BuildDisplay(QueueTicket ticket, int position, int estimatedWaitMinutes)
    {
        return new TicketDisplay
        {
            Id = ticket.Id,
            Number = ticket.Number,
            CustomerName = ticket.CustomerName,
            ContactNumber = ticket.ContactNumber,
            ServiceName = ticket.ServiceName,
            ServiceCode = ticket.ServiceCode,
            CategoryLabel = ticket.Category.ToDisplayName(),
            IsPriority = ticket.IsPriority,
            StatusLabel = ticket.Status.ToDisplayName(),
            CreatedAt = ticket.CreatedAt,
            ScheduledAt = ticket.ScheduledAt,
            CalledAt = ticket.CalledAt,
            CompletedAt = ticket.CompletedAt,
            CounterName = ticket.CounterName,
            Position = position,
            EstimatedWaitMinutes = estimatedWaitMinutes
        };
    }

    private void SeedTickets()
    {
        var now = DateTime.Now;

        _tickets.Add(NewTicket(
            new TicketInput { CustomerName = "Maria Santos", ServiceCode = "GEN", Category = CustomerCategory.SeniorCitizen },
            FindService("GEN"),
            now.AddMinutes(-26),
            QueueTicketStatus.Called,
            scheduledAt: null));
        _tickets[^1].CalledAt = now.AddMinutes(-4);
        _tickets[^1].CounterName = "Counter 1";

        _tickets.Add(NewTicket(
            new TicketInput { CustomerName = "Paolo Reyes", ServiceCode = "PAY", Category = CustomerCategory.Regular },
            FindService("PAY"),
            now.AddMinutes(-20),
            QueueTicketStatus.Waiting,
            scheduledAt: null));

        _tickets.Add(NewTicket(
            new TicketInput { CustomerName = "Ana Cruz", ServiceCode = "DOC", Category = CustomerCategory.Pwd },
            FindService("DOC"),
            now.AddMinutes(-15),
            QueueTicketStatus.Waiting,
            scheduledAt: null));

        _tickets.Add(NewTicket(
            new AppointmentInput
            {
                CustomerName = "Luis Garcia",
                ServiceCode = "MED",
                Category = CustomerCategory.Regular,
                AppointmentAt = now.AddMinutes(45)
            },
            FindService("MED"),
            now.AddMinutes(-60),
            QueueTicketStatus.Scheduled,
            now.AddMinutes(45)));

        var completedTicket = NewTicket(
            new TicketInput { CustomerName = "Nina Flores", ServiceCode = "DOC", Category = CustomerCategory.Regular },
            FindService("DOC"),
            now.AddHours(-2),
            QueueTicketStatus.Completed,
            scheduledAt: null);
        completedTicket.CalledAt = now.AddHours(-2).AddMinutes(10);
        completedTicket.CompletedAt = now.AddHours(-2).AddMinutes(21);
        completedTicket.CounterName = "Counter 3";
        _tickets.Add(completedTicket);
    }
}
