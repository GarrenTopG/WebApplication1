using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class NotificationController : Controller
    {
        private readonly NotificationService _notificationService;

        public NotificationController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET: /Notification/List
        public async Task<IActionResult> List()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);
            return PartialView("_NotificationList", notifications);
        }

        // Mark a notification as read
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return RedirectToAction("List");
        }
    }
}
