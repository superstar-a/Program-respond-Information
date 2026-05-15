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
        public IActionResult CreateUser()
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;
            return View(new User());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(User model)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            if (!ModelState.IsValid)
                return View(model);

            if (await _db.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                return View(model);
            }

            model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
            _db.Users.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Tạo tài khoản thành công!";
            return RedirectToAction("Users");
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, User model, string? newPassword)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Role = model.Role;
            user.Class = model.Class;
            user.Department = model.Department;

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
    }
}
