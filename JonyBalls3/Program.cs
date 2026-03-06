using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using JonyBalls3.Data;
using JonyBalls3.Models;
using JonyBalls3.Services;

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

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// НАШИ СЕРВИСЫ - ВСЕ ДОЛЖНЫ БЫТЬ ЗДЕСЬ!
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<ContractorService>();
builder.Services.AddScoped<CalculatorService>();
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
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}


// В методе Main или после builder.Build():
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated(); // Создаст базу если её нет
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
    
    // Проверяем, нужно ли добавить новые колонки
    try
    {
        // Пытаемся выполнить запрос к колонке Bio - если ошибка, значит колонки нет
        context.Database.ExecuteSqlRaw("SELECT Bio FROM AspNetUsers LIMIT 1");
    }
    catch
    {
        // Колонки нет - добавляем через SQL напрямую
        try { context.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN Bio TEXT DEFAULT ''"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN Location TEXT DEFAULT ''"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN BirthDate TEXT"); } catch { }
        try { context.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN Phone TEXT DEFAULT ''"); } catch { }
        
        Console.WriteLine("✅ База данных обновлена!");
    }
}
app.Run();