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

builder.Services.AddMemoryCache();
builder.Services.AddSession();

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

// Инициализация базы данных
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try 
    {
        // 1. Создаем БД если нет
        context.Database.EnsureCreated();

        // 2. Проверяем и добавляем колонки в AspNetUsers (User)
        string[] userColumns = { "Bio", "Location", "BirthDate", "Phone", "AvatarUrl", "FirstName", "LastName", "CreatedAt" };
        foreach (var col in userColumns)
        {
            try { context.Database.ExecuteSqlRaw($"SELECT {col} FROM AspNetUsers LIMIT 1"); }
            catch {
                try { 
                    string type = col == "BirthDate" || col == "CreatedAt" ? "TEXT" : "TEXT DEFAULT ''";
                    context.Database.ExecuteSqlRaw($"ALTER TABLE AspNetUsers ADD COLUMN {col} {type}"); 
                    logger.LogInformation($"Column {col} added to AspNetUsers");
                } catch { }
            }
        }

        // 3. Проверяем и добавляем колонки в ContractorProfiles
        try { context.Database.ExecuteSqlRaw("SELECT AvatarUrl FROM ContractorProfiles LIMIT 1"); }
        catch {
            try { 
                context.Database.ExecuteSqlRaw("ALTER TABLE ContractorProfiles ADD COLUMN AvatarUrl TEXT"); 
                logger.LogInformation("Column AvatarUrl added to ContractorProfiles");
            } catch { }
        }

        // 4. Создаем таблицу Notifications если нет (ручная миграция)
        try { context.Database.ExecuteSqlRaw("SELECT Id FROM Notifications LIMIT 1"); }
        catch {
            try {
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
                logger.LogInformation("Table Notifications created manually");
            } catch { }
        }

        // 5. Сид ролей и админа
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        string[] roles = { "User", "Contractor", "Admin" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = "admin@jony.ru";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Администратор",
                LastName = "Системный",
                EmailConfirmed = true,
                CreatedAt = DateTime.Now,
                AvatarUrl = ""
            };
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Default admin created: admin@jony.ru / Admin123!");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database initialization");
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
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
