using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JonyBalls3.Models
{
    public class StagePhoto
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int StageId { get; set; }
        
        [ForeignKey("StageId")]
        public virtual ProjectStage Stage { get; set; }
        
        [Required]
        public string ImageUrl { get; set; } = "";
        
        public string Description { get; set; } = "";
        
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        
        [Required]
        public string UploadedById { get; set; }
        
        [ForeignKey("UploadedById")]
        public virtual User UploadedBy { get; set; }
    }
}
