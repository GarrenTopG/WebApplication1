using System;

namespace WebApplication1.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; }           // Who will receive the notification
        public string Message { get; set; }          // Notification text
        public bool IsRead { get; set; } = false;    // Has the user read it?
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

