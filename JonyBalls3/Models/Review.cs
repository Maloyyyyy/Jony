using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JonyBalls3.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ContractorId { get; set; }
        
        [ForeignKey("ContractorId")]
        public virtual ContractorProfile Contractor { get; set; } = null!;
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        public int? ProjectId { get; set; }
        
        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        
        [Required]
        [StringLength(1000)]
        public string Comment { get; set; } = "";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
