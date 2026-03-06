using System.ComponentModel.DataAnnotations;

namespace JonyBalls3.Models
{
    public class UserProfileViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имя обязательно")]
        [Display(Name = "Имя")]
        [StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Фамилия обязательна")]
        [Display(Name = "Фамилия")]
        [StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = "";

        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Display(Name = "Телефон")]
        [Phone]
        public string? Phone { get; set; }

        [Display(Name = "О себе")]
        [StringLength(500)]
        public string? Bio { get; set; }

        [Display(Name = "Город")]
        [StringLength(100)]
        public string? Location { get; set; }

        [Display(Name = "Дата рождения")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "Аватар")]
        public string? AvatarUrl { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }
}
