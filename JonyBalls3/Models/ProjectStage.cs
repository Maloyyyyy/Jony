using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JonyBalls3.Models
{
    public class ProjectStage
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ProjectId { get; set; }
        
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = "";
        
        [StringLength(1000)]
        public string Description { get; set; } = "";
        
        public int Order { get; set; }
        
        [DataType(DataType.Currency)]
        public decimal Budget { get; set; }
        
        [DataType(DataType.Currency)]
        public decimal Spent { get; set; } = 0;
        
        [Range(0, 100)]
        public int Progress { get; set; } = 0;
        
        [Display(Name = "Плановая дата начала")]
        [DataType(DataType.Date)]
        public DateTime? PlannedStartDate { get; set; }
        
        [Display(Name = "Плановая дата окончания")]
        [DataType(DataType.Date)]
        public DateTime? PlannedEndDate { get; set; }
        
        [Display(Name = "Фактическая дата начала")]
        [DataType(DataType.Date)]
        public DateTime? ActualStartDate { get; set; }
        
        [Display(Name = "Фактическая дата окончания")]
        [DataType(DataType.Date)]
        public DateTime? ActualEndDate { get; set; }
        
        public StageStatus Status { get; set; } = StageStatus.NotStarted;
        
        public virtual ICollection<StagePhoto> Photos { get; set; } = new List<StagePhoto>();
    }
    
    public enum StageStatus
    {
        [Display(Name = "Не начат")]
        NotStarted = 1,
        
        [Display(Name = "В работе")]
        InProgress = 2,
        
        [Display(Name = "Завершен")]
        Completed = 3,
        
        [Display(Name = "Проблемы")]
        Issues = 4
    }
}