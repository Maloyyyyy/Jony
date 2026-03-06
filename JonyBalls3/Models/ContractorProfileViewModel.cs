using System.ComponentModel.DataAnnotations;

namespace JonyBalls3.Models
{
    public class ContractorProfileViewModel
    {
        [Required(ErrorMessage = "Название компании обязательно")]
        [Display(Name = "Название компании / ИП")]
        public string CompanyName { get; set; } = "";

        [Required(ErrorMessage = "Специализация обязательна")]
        [Display(Name = "Специализация")]
        public string Specialization { get; set; } = "";

        [Display(Name = "Описание деятельности")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Опыт работы (лет)")]
        [Range(0, 50)]
        public int ExperienceYears { get; set; }

        [Display(Name = "Контактный телефон")]
        [Phone]
        public string? Phone { get; set; }

        [Display(Name = "Сайт")]
        [Url]
        public string? Website { get; set; }

        [Display(Name = "Часовой тариф (руб.)")]
        [Range(0, 10000)]
        public decimal HourlyRate { get; set; }

        public List<IFormFile>? PortfolioFiles { get; set; }
    }
}