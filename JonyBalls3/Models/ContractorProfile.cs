using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JonyBalls3.Models
{
    public class ContractorProfile
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        
        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string Specialization { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string? Description { get; set; }
        
        [Range(0, 50)]
        public int ExperienceYears { get; set; }
        
        [Phone]
        public string? Phone { get; set; }
        
        [Url]
        public string? Website { get; set; }
        
        [Range(0, 10000)]
        public decimal HourlyRate { get; set; }
        
        [Range(0, 5)]
        public double Rating { get; set; } = 0;
        
        public int ReviewsCount { get; set; } = 0;
        
        public ContractorStatus Status { get; set; } = ContractorStatus.Available;
        
        public bool IsVerified { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Навигационные свойства
        public virtual ICollection<PortfolioItem> Portfolio { get; set; } = new List<PortfolioItem>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }
    
    public enum ContractorStatus
    {
        [Display(Name = "Свободен")]
        Available = 1,
        
        [Display(Name = "Занят")]
        Busy = 2,
        
        [Display(Name = "Недоступен")]
        Unavailable = 3
    }
}