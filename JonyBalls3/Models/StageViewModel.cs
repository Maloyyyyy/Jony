using System.ComponentModel.DataAnnotations;

namespace JonyBalls3.Models
{
    public class StageViewModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Название этапа обязательно")]
        [Display(Name = "Название этапа")]
        public string Name { get; set; }

        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Display(Name = "Порядок")]
        public int Order { get; set; } = 1;

        [Display(Name = "Бюджет этапа (руб.)")]
        [Range(0, 10000000)]
        public decimal Budget { get; set; }

        [Display(Name = "Потрачено (руб.)")]
        public decimal Spent { get; set; } = 0;

        [Display(Name = "Прогресс %")]
        [Range(0, 100)]
        public int Progress { get; set; } = 0;

        [Display(Name = "Статус")]
        public string Status { get; set; } = "Не начат";

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
    }
}
