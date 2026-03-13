using System.ComponentModel.DataAnnotations;

namespace JonyBalls3.Models
{
    public class StagePhotoViewModel
    {
        public int StageId { get; set; }

        [Required]
        [Display(Name = "Фото")]
        public IFormFile Image { get; set; }

        [Display(Name = "Описание")]
        public string Description { get; set; }
    }
}
