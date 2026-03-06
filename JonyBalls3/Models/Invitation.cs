using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JonyBalls3.Models
{
    public class Invitation
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ProjectId { get; set; }
        
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }
        
        [Required]
        public int ContractorId { get; set; }
        
        [ForeignKey("ContractorId")]
        public virtual ContractorProfile Contractor { get; set; }
        
        [Required]
        public string Message { get; set; } = "";
        
        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
        
        public DateTime SentAt { get; set; } = DateTime.Now;
        public DateTime? RespondedAt { get; set; }
    }
    
    public enum InvitationStatus
    {
        Pending = 1,
        Accepted = 2,
        Rejected = 3,
        Cancelled = 4
    }
}
