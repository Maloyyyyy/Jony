namespace JonyBalls3.Models
{
    public class SimpleProject
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Area { get; set; }
        public decimal Budget { get; set; }
        public string Status { get; set; } = "Planning";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public static string[] RepairTypes = new[] { "Косметический", "Капитальный", "Дизайнерский" };
        public static string[] Statuses = new[] { "Планирование", "Активный", "Приостановлен", "Завершен" };
    }
}
