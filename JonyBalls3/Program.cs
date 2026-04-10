using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using JonyBalls3.Data;
using JonyBalls3.Models;
using JonyBalls3.Services;
using JonyBalls3.Filters;

var builder = WebApplication.CreateBuilder(args);

// Добавление сервисов
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ContractorStatusFilter>();
});
builder.Services.AddRazorPages();
builder.Services.AddScoped<ContractorStatusFilter>();

// НАШИ СЕРВИСЫ
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<ContractorService>();
builder.Services.AddScoped<CalculatorService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<InvitationService>();

var app = builder.Build();

// Создание базы данных и таблиц при запуске
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
    
    // Создание ролей
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "User", "Contractor", "Admin" };
    
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Создание администратора по умолчанию если нет ни одного
    var userManager2 = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var adminEmail = "admin@jony.ru";
    var existingAdmin = await userManager2.FindByEmailAsync(adminEmail);
    if (existingAdmin == null)
    {
        var adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            NormalizedEmail = adminEmail.ToUpperInvariant(),
            NormalizedUserName = adminEmail.ToUpperInvariant(),
            FirstName = "Администратор",
            LastName = "Системный",
            EmailConfirmed = true,
            CreatedAt = DateTime.Now
        };
        var createResult = await userManager2.CreateAsync(adminUser, "Admin123!");
        if (createResult.Succeeded)
        {
            await userManager2.AddToRoleAsync(adminUser, "Admin");
            Console.WriteLine("✅ Создан администратор: admin@jony.ru / Admin123!");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Автоматическое обновление базы данных при запуске
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Проверяем и добавляем колонки пользователя
    try
    {
        context.Database.ExecuteSqlRaw("SELECT Bio FROM AspNetUsers LIMIT 1");
    }
    catch
    {
        try { context.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN Bio TEXT DEFAULT ''"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN Location TEXT DEFAULT ''"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN BirthDate TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN Phone TEXT DEFAULT ''"); } catch { }
        Console.WriteLine("✅ Колонки пользователя добавлены!");
    }

    // Создаём таблицу Notifications если её нет
    try
    {
        context.Database.ExecuteSqlRaw("SELECT Id FROM Notifications LIMIT 1");
    }
    catch
    {
        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Notifications (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId TEXT NOT NULL,
                    Title TEXT NOT NULL DEFAULT '',
                    Message TEXT NOT NULL DEFAULT '',
                    Type INTEGER NOT NULL DEFAULT 1,
                    Link TEXT,
                    IsRead INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                    ReadAt TEXT,
                    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_Notifications_UserId ON Notifications(UserId);
                CREATE INDEX IF NOT EXISTS IX_Notifications_IsRead ON Notifications(IsRead);
            ");
            Console.WriteLine("✅ Таблица Notifications создана!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Ошибка создания таблицы Notifications: {ex.Message}");
        }
    }
}

app.Run();
