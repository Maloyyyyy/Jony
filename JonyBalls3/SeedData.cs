using Microsoft.EntityFrameworkCore;
using JonyBalls3.Data;
using JonyBalls3.Models;
using Microsoft.AspNetCore.Identity;

namespace JonyBalls3
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());
            
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Создаем роли если их нет
            string[] roles = { "User", "Contractor", "Admin" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Создаем тестовых подрядчиков если их нет
            if (!context.ContractorProfiles.Any())
            {
                // Подрядчик 1
                var user1 = new User
                {
                    UserName = "ivanov@mail.ru",
                    Email = "ivanov@mail.ru",
                    FirstName = "Иван",
                    LastName = "Иванов",
                    CreatedAt = DateTime.Now
                };
                await userManager.CreateAsync(user1, "Test123!");
                await userManager.AddToRoleAsync(user1, "Contractor");

                var contractor1 = new ContractorProfile
                {
                    UserId = user1.Id,
                    CompanyName = "ИП Иванов А.В.",
                    Specialization = "Отделочные работы, плитка, обои",
                    Description = "Профессиональный отделочник с 8-летним опытом. Работаю с квартирами и домами. Гарантия качества.",
                    ExperienceYears = 8,
                    Phone = "+7 (999) 111-22-33",
                    HourlyRate = 1800,
                    Rating = 4.8,
                    ReviewsCount = 15,
                    Status = ContractorStatus.Available,
                    IsVerified = true,
                    CreatedAt = DateTime.Now
                };
                context.ContractorProfiles.Add(contractor1);

                // Подрядчик 2
                var user2 = new User
                {
                    UserName = "petrov@mail.ru",
                    Email = "petrov@mail.ru",
                    FirstName = "Петр",
                    LastName = "Петров",
                    CreatedAt = DateTime.Now
                };
                await userManager.CreateAsync(user2, "Test123!");
                await userManager.AddToRoleAsync(user2, "Contractor");

                var contractor2 = new ContractorProfile
                {
                    UserId = user2.Id,
                    CompanyName = "ООО 'РемонтСтрой'",
                    Specialization = "Сантехника, электрика, отопление",
                    Description = "Команда профессионалов. Любые виды сантехнических и электромонтажных работ. Выезд по городу.",
                    ExperienceYears = 12,
                    Phone = "+7 (999) 222-33-44",
                    HourlyRate = 2200,
                    Rating = 4.9,
                    ReviewsCount = 27,
                    Status = ContractorStatus.Busy,
                    IsVerified = true,
                    CreatedAt = DateTime.Now
                };
                context.ContractorProfiles.Add(contractor2);

                // Подрядчик 3
                var user3 = new User
                {
                    UserName = "sidorov@mail.ru",
                    Email = "sidorov@mail.ru",
                    FirstName = "Сидор",
                    LastName = "Сидоров",
                    CreatedAt = DateTime.Now
                };
                await userManager.CreateAsync(user3, "Test123!");
                await userManager.AddToRoleAsync(user3, "Contractor");

                var contractor3 = new ContractorProfile
                {
                    UserId = user3.Id,
                    CompanyName = "ИП Сидоров",
                    Specialization = "Дизайн интерьеров, отделка премиум",
                    Description = "Дизайнер-проектировщик. Разработка дизайн-проектов, авторский надзор. Индивидуальный подход.",
                    ExperienceYears = 15,
                    Phone = "+7 (999) 333-44-55",
                    HourlyRate = 3000,
                    Rating = 5.0,
                    ReviewsCount = 9,
                    Status = ContractorStatus.Available,
                    IsVerified = true,
                    CreatedAt = DateTime.Now
                };
                context.ContractorProfiles.Add(contractor3);

                await context.SaveChangesAsync();
            }

            // Создаем тестовые проекты если их нет
            if (!context.Projects.Any())
            {
                var user = await userManager.FindByEmailAsync("ivanov@mail.ru");
                if (user != null)
                {
                    var project = new Project
                    {
                        UserId = user.Id,
                        Name = "Ремонт ванной комнаты",
                        Description = "Требуется сделать ремонт в ванной 5м²: замена плитки, сантехники, электрики",
                        Area = 5,
                        RepairType = RepairType.Capital,
                        Budget = 250000,
                        Status = ProjectStatus.Planning,
                        CreatedAt = DateTime.Now,
                        Progress = 0,
                        Spent = 0
                    };
                    context.Projects.Add(project);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}