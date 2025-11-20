using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;



        public NotificationService(
    ApplicationDbContext context,
    UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task AddNotificationAsync(string userId, string message)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(message))
                return;

            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        // Get first Coordinator user ID (or list if needed)
        public async Task<string?> GetCoordinatorUserIdAsync()
        {
            var coordinators = await _userManager.GetUsersInRoleAsync("Coordinator");
            return coordinators.FirstOrDefault()?.Id;
        }

        // Get first HR user ID (or list if needed)
        public async Task<string?> GetHRUserIdAsync()
        {
            var hrUsers = await _userManager.GetUsersInRoleAsync("HR");
            return hrUsers.FirstOrDefault()?.Id;
        }
    }
}
