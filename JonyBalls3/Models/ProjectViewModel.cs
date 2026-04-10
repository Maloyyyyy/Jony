using System.ComponentModel.DataAnnotations;

namespace JonyBalls3.Models
{
    public class ProjectViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название проекта обязательно")]
        [Display(Name = "Название проекта")]
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        public string? Name { get; set; }

        [Display(Name = "Описание")]
        [StringLength(2000, ErrorMessage = "Описание не должно превышать 2000 символов")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Площадь обязательна")]
        [Display(Name = "Площадь (м²)")]
        [Range(1, 10000, ErrorMessage = "Площадь должна быть от 1 до 10000 м²")]
        public decimal Area { get; set; }

        [Required(ErrorMessage = "Тип ремонта обязателен")]
        [Display(Name = "Тип ремонта")]
        public string? RepairType { get; set; }

        [Required(ErrorMessage = "Бюджет обязателен")]
        [Display(Name = "Бюджет (руб.)")]
        [Range(100, 100000000, ErrorMessage = "Бюджет должен быть от 100 до 100 000 000 руб.")]
        public decimal Budget { get; set; }

        [Display(Name = "Дата начала")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Дата окончания")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Статус")]
        public string Status { get; set; } = "Планирование";

        // Для расчета из калькулятора
        public bool FromCalculator { get; set; }
        public decimal? CalculatedTotal { get; set; }
    }
}
