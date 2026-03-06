using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JonyBalls3.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        
        public int? ContractorId { get; set; }
        
        [ForeignKey("ContractorId")]
        public virtual ContractorProfile? Contractor { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [Range(1, 1000)]
        public decimal Area { get; set; }
        
        public RepairType RepairType { get; set; }
        
        [DataType(DataType.Currency)]
        public decimal Budget { get; set; }
        
        [DataType(DataType.Currency)]
        public decimal Spent { get; set; } = 0;
        
        [DataType(DataType.Currency)]
        public decimal Remaining => Budget - Spent;
        
        [Range(0, 100)]
        public int Progress { get; set; } = 0;
        
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
        
        public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        
        // Навигационные свойства
        public virtual ICollection<ProjectStage> Stages { get; set; } = new List<ProjectStage>();
        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    }
    
    public enum RepairType
    {
        [Display(Name = "Косметический")]
        Cosmetic = 1,
        
        [Display(Name = "Капитальный")]
        Capital = 2,
        
        [Display(Name = "Дизайнерский")]
        Design = 3
    }
    
    public enum ProjectStatus
    {
        [Display(Name = "Планирование")]
        Planning = 1,
        
        [Display(Name = "Активный")]
        Active = 2,
        
        [Display(Name = "Приостановлен")]
        Paused = 3,
        
        [Display(Name = "Завершен")]
        Completed = 4,
        
        [Display(Name = "Отменен")]
        Cancelled = 5
    }
}