using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JonyBalls3.Models
{
    public class PortfolioItem
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ContractorId { get; set; }
        
        [ForeignKey("ContractorId")]
        public virtual ContractorProfile Contractor { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";
        
        [StringLength(1000)]
        public string Description { get; set; } = "";
        
        public string ImageUrl { get; set; } = "";
        
        public DateTime CompletedDate { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
