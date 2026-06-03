using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLTTYKPH.Data;
using QLTTYKPH.Helpers;
using QLTTYKPH.Models;

namespace QLTTYKPH.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        private IActionResult RequireAdmin()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!SessionHelper.IsAdmin(HttpContext.Session))
                return RedirectToAction("Index", "Home");
            return null!;
        }

        // ==================== USERS ====================
        public async Task<IActionResult> Users(string? search)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            var query = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Username.Contains(search));

            ViewBag.Search = search;
            return View(await query.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            ViewBag.Classes = await _db.Classes.ToListAsync();
            return View(new User());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(User model)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            if (!ModelState.IsValid)
            {
                ViewBag.Classes = await _db.Classes.ToListAsync();
                return View(model);
            }

            if (await _db.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                ViewBag.Classes = await _db.Classes.ToListAsync();
                return View(model);
            }

            model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
            _db.Users.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Tạo tài khoản thành công!";
            return RedirectToAction("Users");
        }

        [HttpGet]
        public IActionResult BulkCreateUsers()
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            ViewBag.Classes = _db.Classes.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkCreateUsers(string csvData, UserRole defaultRole, int? defaultClassId)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            if (string.IsNullOrWhiteSpace(csvData))
            {
                TempData["Error"] = "Vui lòng nhập dữ liệu!";
                ViewBag.Classes = _db.Classes.ToList();
                return View();
            }

            var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var createdCount = 0;
            var errorCount = 0;
            var errors = new List<string>();

            foreach (var line in lines)
            {
                var parts = line.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length < 2)
                {
                    errorCount++;
                    errors.Add($"Dòng không hợp lệ: {line}");
                    continue;
                }

                var username = parts[0];
                var fullName = parts[1];
                var password = parts.Length > 2 ? parts[2] : "123456"; // Mật khẩu mặc định

                if (await _db.Users.AnyAsync(u => u.Username == username))
                {
                    errorCount++;
                    errors.Add($"Username {username} đã tồn tại");
                    continue;
                }

                var user = new User
                {
                    Username = username,
                    Password = BCrypt.Net.BCrypt.HashPassword(password),
                    FullName = fullName,
                    Role = defaultRole,
                    ClassId = defaultClassId,
                    MustChangePassword = true
                };

                _db.Users.Add(user);
                createdCount++;
            }

            await _db.SaveChangesAsync();

            if (errorCount > 0)
            {
                TempData["Warning"] = $"Đã tạo {createdCount} tài khoản, {errorCount} lỗi: {string.Join("; ", errors.Take(5))}";
            }
            else
            {
                TempData["Success"] = $"Đã tạo thành công {createdCount} tài khoản!";
            }

            return RedirectToAction("Users");
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            ViewBag.Classes = await _db.Classes.ToListAsync();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, User model, string? newPassword, bool mustChangePassword)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Role = model.Role;
            user.ClassId = model.ClassId;
            user.Department = model.Department;
            user.MustChangePassword = mustChangePassword;

            if (!string.IsNullOrWhiteSpace(newPassword))
                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

            await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật tài khoản thành công!";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.Id == SessionHelper.GetUserId(HttpContext.Session))
            {
                TempData["Error"] = "Không thể xóa tài khoản đang đăng nhập!";
                return RedirectToAction("Users");
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Xóa tài khoản thành công!";
            return RedirectToAction("Users");
        }

        // ==================== CATEGORIES ====================
        public async Task<IActionResult> Categories()
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            return View(await _db.Categories.ToListAsync());
        }

        [HttpGet]
        public IActionResult CreateCategory()
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category model)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            if (!ModelState.IsValid) return View(model);
            _db.Categories.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Tạo danh mục thành công!";
            return RedirectToAction("Categories");
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            var cat = await _db.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, Category model)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            var cat = await _db.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            cat.Name = model.Name;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật danh mục thành công!";
            return RedirectToAction("Categories");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            var cat = await _db.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            _db.Categories.Remove(cat);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Xóa danh mục thành công!";
            return RedirectToAction("Categories");
        }

        // ==================== DEPARTMENTS ====================
        public async Task<IActionResult> Departments()
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            return View(await _db.Departments.ToListAsync());
        }

        [HttpGet]
        public IActionResult CreateDepartment()
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            return View(new Department());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartment(Department model)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            if (!ModelState.IsValid) return View(model);
            _db.Departments.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Tạo phòng ban thành công!";
            return RedirectToAction("Departments");
        }

        [HttpGet]
        public async Task<IActionResult> EditDepartment(int id)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            var dept = await _db.Departments.FindAsync(id);
            if (dept == null) return NotFound();
            return View(dept);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDepartment(int id, Department model)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            var dept = await _db.Departments.FindAsync(id);
            if (dept == null) return NotFound();
            dept.Name = model.Name;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật phòng ban thành công!";
            return RedirectToAction("Departments");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            var dept = await _db.Departments.FindAsync(id);
            if (dept == null) return NotFound();
            _db.Departments.Remove(dept);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Xóa phòng ban thành công!";
            return RedirectToAction("Departments");
        }

        // ==================== CLASSES ====================
        public async Task<IActionResult> Classes()
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            return View(await _db.Classes.ToListAsync());
        }

        [HttpGet]
        public IActionResult CreateClass()
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            return View(new Class());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass(Class model)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            if (!ModelState.IsValid) return View(model);
            _db.Classes.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Tạo lớp thành công!";
            return RedirectToAction("Classes");
        }

        [HttpGet]
        public async Task<IActionResult> EditClass(int id)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            var cls = await _db.Classes.FindAsync(id);
            if (cls == null) return NotFound();
            return View(cls);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClass(int id, Class model)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            var cls = await _db.Classes.FindAsync(id);
            if (cls == null) return NotFound();
            cls.Name = model.Name;
            cls.Description = model.Description;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật lớp thành công!";
            return RedirectToAction("Classes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            var cls = await _db.Classes.FindAsync(id);
            if (cls == null) return NotFound();
            _db.Classes.Remove(cls);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Xóa lớp thành công!";
            return RedirectToAction("Classes");
        }
    }
}
