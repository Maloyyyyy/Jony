namespace JonyBalls3.Models
{
    public class SimpleContractor
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Specialization { get; set; } = "";
        public string Phone { get; set; } = "";
        public decimal Rating { get; set; }
        public int ExperienceYears { get; set; }
        public decimal PricePerHour { get; set; }
        
        public static SimpleContractor[] GetSampleData()
        {
            return new[]
            {
                new SimpleContractor { Id = 1, Name = "ООО 'РемонтПрофи'", Specialization = "Капитальный ремонт", Phone = "+375 29 111-22-33", Rating = 4.8m, ExperienceYears = 10, PricePerHour = 2500 },
                new SimpleContractor { Id = 2, Name = "ИП Иванов А.В.", Specialization = "Косметический ремонт", Phone = "+375 29 222-33-44", Rating = 4.5m, ExperienceYears = 5, PricePerHour = 1800 },
                new SimpleContractor { Id = 3, Name = "Студия 'ДизайнИнтерьер'", Specialization = "Дизайнерский ремонт", Phone = "+375 29 333-44-55", Rating = 4.9m, ExperienceYears = 8, PricePerHour = 3500 }
            };
        }
    }
}
