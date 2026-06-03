using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLTTYKPH.Data;
using QLTTYKPH.Helpers;
using QLTTYKPH.ViewModels;

namespace QLTTYKPH.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;

        public AccountController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
                return View(model);
            }

            bool passwordOk = false;
            bool isBcrypt = user.Password.StartsWith("$2a$") || user.Password.StartsWith("$2b$");

            if (isBcrypt)
            {
                try { passwordOk = BCrypt.Net.BCrypt.Verify(model.Password, user.Password); }
                catch { passwordOk = false; }
            }
            else
            {
                passwordOk = user.Password == model.Password;
            }

            if (!passwordOk)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
                return View(model);
            }

            if (!isBcrypt)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                await _db.SaveChangesAsync();
            }

            SessionHelper.SetUser(HttpContext.Session, user);

            if (user.MustChangePassword)
            {
                TempData["Warning"] = "Bạn cần đổi mật khẩu trước khi tiếp tục.";
                return RedirectToAction("ChangePassword");
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            SessionHelper.Clear(HttpContext.Session);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login");

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp");
                return View();
            }

            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var user = await _db.Users.FindAsync(userId);
            if (user == null || !BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không đúng");
                return View();
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.MustChangePassword = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index", "Home");
        }
    }
}
