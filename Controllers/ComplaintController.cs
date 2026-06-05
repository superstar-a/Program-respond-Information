using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLTTYKPH.Data;
using QLTTYKPH.Helpers;
using QLTTYKPH.Models;

namespace QLTTYKPH.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ComplaintController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET: Complaint
        public async Task<IActionResult> Index(string? search, string? status, int? departmentId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var role = SessionHelper.GetUserRole(HttpContext.Session);
            int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;

            var query = _db.Complaints
                .Include(c => c.User)
                    .ThenInclude(u => u!.Class)
                .Include(c => c.Department)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Title.Contains(search) || (c.User != null && c.User.FullName.Contains(search)));
            }

            if (role == "Student")
            {
                // Học sinh chỉ xem khiếu nại của chính mình
                query = query.Where(c => c.UserId == userId);
            }
            else if (role == "Staff")
            {
                // Nhân viên chỉ xem khiếu nại gửi tới phòng ban của mình
                var staffUser = await _db.Users.FindAsync(userId);
                if (staffUser == null || string.IsNullOrWhiteSpace(staffUser.Department))
                {
                    // Nếu nhân viên không thuộc phòng ban nào, trả về danh sách trống
                    query = query.Where(c => false);
                }
                else
                {
                    query = query.Where(c => c.Department != null && c.Department.Name.ToLower() == staffUser.Department.ToLower());
                    // Cố định filter bộ lọc phòng ban
                    var staffDept = await _db.Departments.FirstOrDefaultAsync(d => d.Name.ToLower() == staffUser.Department.ToLower());
                    if (staffDept != null)
                    {
                        departmentId = staffDept.Id;
                    }
                }
            }
            else if (role == "Admin")
            {
                // Admin xem toàn bộ, có thể lọc theo phòng ban và trạng thái
                if (departmentId.HasValue)
                {
                    query = query.Where(c => c.DepartmentId == departmentId.Value);
                }
                
                ViewBag.Departments = await _db.Departments.ToListAsync();
                ViewBag.DepartmentId = departmentId;
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ComplaintStatus>(status, out var statusEnum))
            {
                query = query.Where(c => c.Status == statusEnum);
            }
            ViewBag.Status = status;
            ViewBag.Search = search;

            var complaints = await query.OrderByDescending(c => c.SubmittedAt).ToListAsync();
            return View(complaints);
        }

        // GET: Complaint/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (!SessionHelper.IsStudent(HttpContext.Session))
                return RedirectToAction("Index");

            ViewBag.Departments = await _db.Departments.ToListAsync();
            return View(new Complaint());
        }

        // POST: Complaint/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Complaint model, List<IFormFile> attachments)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (!SessionHelper.IsStudent(HttpContext.Session))
                return RedirectToAction("Index");

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _db.Departments.ToListAsync();
                return View(model);
            }

            try
            {
                if (attachments != null && attachments.Count > 0)
                {
                    model.AttachmentPath = await FileHelper.UploadMultipleFilesAsync(attachments, "complaints", _env.WebRootPath);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Content", ex.Message);
                ViewBag.Departments = await _db.Departments.ToListAsync();
                return View(model);
            }

            int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            model.UserId = userId;
            model.SubmittedAt = DateTime.Now;
            model.Status = ComplaintStatus.New;
            model.ResolutionNote = null;
            model.ResolvedAt = null;
            model.ResolvedByUserId = null;

            _db.Complaints.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Gửi khiếu nại thành công!";
            return RedirectToAction("Index");
        }

        // GET: Complaint/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var role = SessionHelper.GetUserRole(HttpContext.Session);
            int currentUserId = SessionHelper.GetUserId(HttpContext.Session)!.Value;

            var complaint = await _db.Complaints
                .Include(c => c.User)
                    .ThenInclude(u => u!.Class)
                .Include(c => c.Department)
                .Include(c => c.ResolvedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null)
                return NotFound();

            // Phân quyền xem chi tiết
            if (role == "Student" && complaint.UserId != currentUserId)
            {
                return Forbid();
            }
            else if (role == "Staff")
            {
                var staffUser = await _db.Users.FindAsync(currentUserId);
                if (staffUser == null || string.IsNullOrWhiteSpace(staffUser.Department) ||
                    complaint.Department!.Name.ToLower() != staffUser.Department.ToLower())
                {
                    return Forbid();
                }
            }

            return View(complaint);
        }

        // POST: Complaint/Process/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(int id, string? resolutionNote, string newStatus, List<IFormFile> attachments)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var role = SessionHelper.GetUserRole(HttpContext.Session);
            if (role == "Student")
                return Forbid();

            int currentUserId = SessionHelper.GetUserId(HttpContext.Session)!.Value;

            var complaint = await _db.Complaints
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null)
                return NotFound();

            // Phân quyền xử lý đối với Staff
            if (role == "Staff")
            {
                var staffUser = await _db.Users.FindAsync(currentUserId);
                if (staffUser == null || string.IsNullOrWhiteSpace(staffUser.Department) ||
                    complaint.Department!.Name.ToLower() != staffUser.Department.ToLower())
                {
                    return Forbid();
                }
            }

            if (string.IsNullOrWhiteSpace(resolutionNote))
            {
                TempData["Error"] = "Vui lòng nhập nội dung phản hồi/giải quyết!";
                return RedirectToAction("Details", new { id });
            }

            try
            {
                if (attachments != null && attachments.Count > 0)
                {
                    complaint.ResolutionAttachmentPath = await FileHelper.UploadMultipleFilesAsync(attachments, "complaints", _env.WebRootPath);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi tải file minh chứng: " + ex.Message;
                return RedirectToAction("Details", new { id });
            }

            if (Enum.TryParse<ComplaintStatus>(newStatus, out var statusEnum))
            {
                complaint.Status = statusEnum;
                complaint.ResolutionNote = resolutionNote;

                if (statusEnum == ComplaintStatus.Resolved)
                {
                    complaint.ResolvedAt = DateTime.Now;
                    complaint.ResolvedByUserId = currentUserId;
                }
                else
                {
                    complaint.ResolvedAt = null;
                    complaint.ResolvedByUserId = null;
                }

                await _db.SaveChangesAsync();
                TempData["Success"] = "Cập nhật trạng thái và phản hồi khiếu nại thành công!";
            }
            else
            {
                TempData["Error"] = "Trạng thái mới không hợp lệ!";
            }

            return RedirectToAction("Details", new { id });
        }
    }
}
