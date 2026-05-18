using System.Diagnostics;
using ADET_Group_12.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ADET_Group_12.Models;

namespace ADET_Group_12.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly SmartQQueueService _queueService;

    public HomeController(SmartQQueueService queueService)
    {
        _queueService = queueService;
    }

    public IActionResult Index()
    {
        return View(_queueService.GetDashboard(
            TempData["FlashMessage"] as string,
            TempData["FlashTone"] as string));
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = SmartQRoles.Customer)]
    [ValidateAntiForgeryToken]
    public IActionResult CreateTicket(TicketInput input)
    {
        try
        {
            var ticket = _queueService.CreateTicket(input);
            Flash($"Ticket {ticket.Number} is ready for {ticket.CustomerName}.", "success");
        }
        catch (InvalidOperationException exception)
        {
            Flash(exception.Message, "danger");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = SmartQRoles.Customer)]
    [ValidateAntiForgeryToken]
    public IActionResult BookAppointment(AppointmentInput input)
    {
        try
        {
            var ticket = _queueService.BookAppointment(input);
            Flash($"Appointment {ticket.Number} is booked for {ticket.ScheduledAt:g}.", "success");
        }
        catch (InvalidOperationException exception)
        {
            Flash(exception.Message, "danger");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = SmartQRoles.ServiceProvider)]
    [ValidateAntiForgeryToken]
    public IActionResult CallNext(string? serviceCode)
    {
        var ticket = _queueService.CallNext(serviceCode);
        Flash(
            ticket is null
                ? "No waiting tickets match that service."
                : $"{ticket.Number} is now serving at {ticket.CounterName}.",
            ticket is null ? "warning" : "success");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = SmartQRoles.ServiceProvider)]
    [ValidateAntiForgeryToken]
    public IActionResult CompleteTicket(int id)
    {
        Flash(
            _queueService.CompleteTicket(id)
                ? "Ticket completed."
                : "That ticket is no longer active.",
            "info");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = SmartQRoles.ServiceProvider)]
    [ValidateAntiForgeryToken]
    public IActionResult CancelTicket(int id)
    {
        Flash(
            _queueService.CancelTicket(id)
                ? "Ticket cancelled."
                : "That ticket cannot be cancelled.",
            "info");

        return RedirectToAction(nameof(Index));
    }

    private void Flash(string message, string tone)
    {
        TempData["FlashMessage"] = message;
        TempData["FlashTone"] = tone;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
