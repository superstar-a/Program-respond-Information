using Microsoft.EntityFrameworkCore;
using QLTTYKPH.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Categories.Any())
    {
        db.Categories.AddRange(
            new QLTTYKPH.Models.Category { Name = "Khảo sát môn học" },
            new QLTTYKPH.Models.Category { Name = "Khảo sát cơ sở vật chất" },
            new QLTTYKPH.Models.Category { Name = "Khảo sát dịch vụ sinh viên" }
        );
        db.SaveChanges();
    }

    if (!db.Departments.Any())
    {
        db.Departments.AddRange(
            new QLTTYKPH.Models.Department { Name = "Phòng Đào tạo" },
            new QLTTYKPH.Models.Department { Name = "Phòng Khảo thí" },
            new QLTTYKPH.Models.Department { Name = "Phòng Quản trị" }
        );
        db.SaveChanges();
    }

    if (!db.Users.Any())
    {
        db.Users.AddRange(
            new QLTTYKPH.Models.User
            {
                Username = "admin",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FullName = "Quản trị viên",
                Role = QLTTYKPH.Models.UserRole.Admin
            },
            new QLTTYKPH.Models.User
            {
                Username = "staff01",
                Password = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                FullName = "Nguyễn Văn A",
                Role = QLTTYKPH.Models.UserRole.Staff
            },
            new QLTTYKPH.Models.User
            {
                Username = "sv001",
                Password = BCrypt.Net.BCrypt.HashPassword("Student@123"),
                FullName = "Trần Thị B",
                Role = QLTTYKPH.Models.UserRole.Student,
                Class = "CNTT01"
            }
        );
        db.SaveChanges();
    }
}

app.Run();
