using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLTTYKPH.Data;
using QLTTYKPH.Helpers;
using QLTTYKPH.Models;

namespace QLTTYKPH.Controllers
{
    public class SurveyController : Controller
    {
        private readonly AppDbContext _db;

        public SurveyController(AppDbContext db)
        {
            _db = db;
        }

        private IActionResult RequireStaffOrAdmin()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (SessionHelper.IsStudent(HttpContext.Session))
                return RedirectToAction("Index", "Home");
            return null!;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId, int? departmentId, bool? isPublished)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var query = _db.Surveys
                .Include(s => s.Category)
                .Include(s => s.Department)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.Title.Contains(search));
            if (categoryId.HasValue)
                query = query.Where(s => s.CategoryId == categoryId);
            if (departmentId.HasValue)
                query = query.Where(s => s.DepartmentId == departmentId);
            if (isPublished.HasValue)
                query = query.Where(s => s.IsPublished == isPublished);

            ViewBag.Categories = await _db.Categories.ToListAsync();
            ViewBag.Departments = await _db.Departments.ToListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.DepartmentId = departmentId;
            ViewBag.IsPublished = isPublished;

            return View(await query.OrderByDescending(s => s.Id).ToListAsync());
        }

        private async Task<List<string>> GetClassListAsync()
        {
            return await _db.Users
                .Where(u => !string.IsNullOrEmpty(u.Class))
                .Select(u => u.Class!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;
            ViewBag.Categories = await _db.Categories.ToListAsync();
            ViewBag.Departments = await _db.Departments.ToListAsync();
            ViewBag.Classes = await GetClassListAsync();
            return View(new Survey());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Survey model)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            ModelState.Remove("ClassTarget");
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.Categories.ToListAsync();
                ViewBag.Departments = await _db.Departments.ToListAsync();
                ViewBag.Classes = await GetClassListAsync();
                return View(model);
            }

            model.IsPublished = false;
            model.CreatedAt = DateTime.Now;
            model.ClassTarget = model.ClassTarget ?? string.Empty;
            _db.Surveys.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Tạo khảo sát thành công!";
            return RedirectToAction("Questions", new { surveyId = model.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var survey = await _db.Surveys.FindAsync(id);
            if (survey == null) return NotFound();
            ViewBag.Categories = await _db.Categories.ToListAsync();
            ViewBag.Departments = await _db.Departments.ToListAsync();
            ViewBag.Classes = await GetClassListAsync();
            return View(survey);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Survey model)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var survey = await _db.Surveys.FindAsync(id);
            if (survey == null) return NotFound();

            ModelState.Remove("ClassTarget");
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.Categories.ToListAsync();
                ViewBag.Departments = await _db.Departments.ToListAsync();
                ViewBag.Classes = await GetClassListAsync();
                return View(model);
            }

            survey.Title = model.Title;
            survey.Description = model.Description;
            survey.ClassTarget = model.ClassTarget ?? string.Empty;
            survey.CategoryId = model.CategoryId;
            survey.DepartmentId = model.DepartmentId;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật khảo sát thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var survey = await _db.Surveys.FindAsync(id);
            if (survey == null) return NotFound();
            _db.Surveys.Remove(survey);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Xóa khảo sát thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var survey = await _db.Surveys.FindAsync(id);
            if (survey == null) return NotFound();
            survey.IsPublished = !survey.IsPublished;
            if (survey.IsPublished)
            {
                survey.PublishedAt = DateTime.Now;
                survey.ClosedAt = null;
            }
            else
            {
                survey.ClosedAt = DateTime.Now;
            }
            await _db.SaveChangesAsync();
            TempData["Success"] = survey.IsPublished ? "Khảo sát đã được xuất bản!" : "Khảo sát đã đóng!";
            return RedirectToAction("Index");
        }

        // ==================== QUESTIONS ====================
        public async Task<IActionResult> Questions(int surveyId)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var survey = await _db.Surveys
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.Id == surveyId);
            if (survey == null) return NotFound();
            return View(survey);
        }

        [HttpGet]
        public async Task<IActionResult> AddQuestion(int surveyId)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var survey = await _db.Surveys.FindAsync(surveyId);
            if (survey == null) return NotFound();
            ViewBag.SurveyTitle = survey.Title;
            return View(new Question { SurveyId = surveyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(Question model)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            if (!ModelState.IsValid)
            {
                var survey = await _db.Surveys.FindAsync(model.SurveyId);
                ViewBag.SurveyTitle = survey?.Title;
                return View(model);
            }

            model.Order = await _db.Questions.CountAsync(q => q.SurveyId == model.SurveyId) + 1;
            _db.Questions.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Thêm câu hỏi thành công!";
            return RedirectToAction("Questions", new { surveyId = model.SurveyId });
        }

        [HttpGet]
        public async Task<IActionResult> EditQuestion(int id)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var question = await _db.Questions.Include(q => q.Survey).FirstOrDefaultAsync(q => q.Id == id);
            if (question == null) return NotFound();
            ViewBag.SurveyTitle = question.Survey?.Title;
            return View(question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(int id, Question model)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var question = await _db.Questions.FindAsync(id);
            if (question == null) return NotFound();
            question.Text = model.Text;
            question.Type = model.Type;
            question.Options = model.Options;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật câu hỏi thành công!";
            return RedirectToAction("Questions", new { surveyId = question.SurveyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var question = await _db.Questions.FindAsync(id);
            if (question == null) return NotFound();
            int surveyId = question.SurveyId;
            _db.Questions.Remove(question);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Xóa câu hỏi thành công!";
            return RedirectToAction("Questions", new { surveyId });
        }

        // ==================== DETAILS ====================
        public async Task<IActionResult> Details(int id)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var survey = await _db.Surveys
                .Include(s => s.Category)
                .Include(s => s.Department)
                .Include(s => s.Questions)
                .Include(s => s.Feedbacks)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (survey == null) return NotFound();
            return View(survey);
        }
    }
}
