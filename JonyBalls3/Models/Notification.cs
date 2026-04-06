using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JonyBalls3.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Message { get; set; } = "";

        public NotificationType Type { get; set; } = NotificationType.Info;

        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReadAt { get; set; }
    }

    public enum NotificationType
    {
        Info = 1,
        Success = 2,
        Warning = 3,
        Danger = 4,
        Chat = 5,
        Invitation = 6,
        Project = 7
    }
}
