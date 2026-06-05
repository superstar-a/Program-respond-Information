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
        private readonly IWebHostEnvironment _env;

        public SurveyController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private IActionResult RequireStaffOrAdmin()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (SessionHelper.IsStudent(HttpContext.Session))
                return RedirectToAction("Index", "Home");
            return null!;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId, int? departmentId, int? classId, bool? isPublished)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            bool isAdmin = SessionHelper.IsAdmin(HttpContext.Session);
            int? sessionDeptId = HttpContext.Session.GetInt32("DepartmentId");

            var query = _db.Surveys
                .Include(s => s.Category)
                .Include(s => s.Department)
                .Include(s => s.Class)
                .AsQueryable();

            if (!isAdmin && sessionDeptId.HasValue)
            {
                query = query.Where(s => s.DepartmentId == sessionDeptId.Value);
                departmentId = sessionDeptId.Value;
            }

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.Title.Contains(search));
            if (categoryId.HasValue)
                query = query.Where(s => s.CategoryId == categoryId);
            if (departmentId.HasValue)
                query = query.Where(s => s.DepartmentId == departmentId);
            if (classId.HasValue)
                query = query.Where(s => s.ClassId == classId);
            if (isPublished.HasValue)
                query = query.Where(s => s.IsPublished == isPublished);

            ViewBag.Categories = await _db.Categories.ToListAsync();
            ViewBag.Departments = await _db.Departments.ToListAsync();
            ViewBag.Classes = await GetClassListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.DepartmentId = departmentId;
            ViewBag.ClassId = classId;
            ViewBag.IsPublished = isPublished;

            return View(await query.OrderByDescending(s => s.Id).ToListAsync());
        }

        private async Task<List<Class>> GetClassListAsync()
        {
            return await _db.Classes
                .OrderBy(c => c.Name)
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
        public async Task<IActionResult> Create(Survey model, List<IFormFile> attachments)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Thời gian kết thúc phải sau thời gian mở.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.Categories.ToListAsync();
                ViewBag.Departments = await _db.Departments.ToListAsync();
                ViewBag.Classes = await _db.Classes.ToListAsync();
                return View(model);
            }

            try
            {
                if (attachments != null && attachments.Count > 0)
                {
                    model.AttachmentPath = await FileHelper.UploadMultipleFilesAsync(attachments, "surveys", _env.WebRootPath);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("AttachmentPath", ex.Message);
                ViewBag.Categories = await _db.Categories.ToListAsync();
                ViewBag.Departments = await _db.Departments.ToListAsync();
                ViewBag.Classes = await GetClassListAsync();
                return View(model);
            }

            model.IsPublished = false;
            model.CreatedAt = DateTime.Now;
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
        public async Task<IActionResult> Edit(int id, Survey model, List<IFormFile> attachments)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            if (id != model.Id) return NotFound();

            if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Thời gian kết thúc phải sau thời gian mở.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.Categories.ToListAsync();
                ViewBag.Departments = await _db.Departments.ToListAsync();
                ViewBag.Classes = await GetClassListAsync();
                return View(model);
            }

            var survey = await _db.Surveys.FindAsync(id);
            if (survey == null) return NotFound();

            try
            {
                if (attachments != null && attachments.Count > 0)
                {
                    survey.AttachmentPath = await FileHelper.UploadMultipleFilesAsync(attachments, "surveys", _env.WebRootPath);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("AttachmentPath", ex.Message);
                ViewBag.Categories = await _db.Categories.ToListAsync();
                ViewBag.Departments = await _db.Departments.ToListAsync();
                ViewBag.Classes = await GetClassListAsync();
                return View(model);
            }

            survey.Title = model.Title;
            survey.Description = model.Description;
            survey.ClassId = model.ClassId;
            survey.CategoryId = model.CategoryId;
            survey.DepartmentId = model.DepartmentId;
            survey.StartDate = model.StartDate;
            survey.EndDate = model.EndDate;
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
            var referer = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("Index");
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
            var referer = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("Index");
        }

        // ==================== QUESTIONS ====================
        public async Task<IActionResult> Questions(int surveyId)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var survey = await _db.Surveys
                .Include(s => s.Questions.OrderBy(q => q.Order))
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
        public async Task<IActionResult> AddQuestion(Question model, List<IFormFile> attachments)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            if (!ModelState.IsValid)
            {
                var survey = await _db.Surveys.FindAsync(model.SurveyId);
                ViewBag.SurveyTitle = survey?.Title;
                return View(model);
            }

            try
            {
                if (attachments != null && attachments.Count > 0)
                {
                    model.AttachmentPath = await FileHelper.UploadMultipleFilesAsync(attachments, "questions", _env.WebRootPath);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("AttachmentPath", ex.Message);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveQuestionUp(int id)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var question = await _db.Questions.FindAsync(id);
            if (question == null) return NotFound();

            var questions = await _db.Questions
                .Where(q => q.SurveyId == question.SurveyId)
                .OrderBy(q => q.Order)
                .ToListAsync();

            var currentIndex = questions.FindIndex(q => q.Id == id);
            if (currentIndex > 0)
            {
                var prevQuestion = questions[currentIndex - 1];
                int tempOrder = question.Order;
                question.Order = prevQuestion.Order;
                prevQuestion.Order = tempOrder;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Questions", new { surveyId = question.SurveyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveQuestionDown(int id)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var question = await _db.Questions.FindAsync(id);
            if (question == null) return NotFound();

            var questions = await _db.Questions
                .Where(q => q.SurveyId == question.SurveyId)
                .OrderBy(q => q.Order)
                .ToListAsync();

            var currentIndex = questions.FindIndex(q => q.Id == id);
            if (currentIndex < questions.Count - 1)
            {
                var nextQuestion = questions[currentIndex + 1];
                int tempOrder = question.Order;
                question.Order = nextQuestion.Order;
                nextQuestion.Order = tempOrder;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Questions", new { surveyId = question.SurveyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResequenceQuestions(int surveyId)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var questions = await _db.Questions
                .Where(q => q.SurveyId == surveyId)
                .OrderBy(q => q.Order)
                .ToListAsync();

            for (int i = 0; i < questions.Count; i++)
            {
                questions[i].Order = i + 1;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã sắp xếp lại số thứ tự câu hỏi!";
            return RedirectToAction("Questions", new { surveyId });
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
        public async Task<IActionResult> EditQuestion(int id, Question model, List<IFormFile> attachments)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var question = await _db.Questions.FindAsync(id);
            if (question == null) return NotFound();

            if (!ModelState.IsValid)
            {
                var survey = await _db.Surveys.FindAsync(question.SurveyId);
                ViewBag.SurveyTitle = survey?.Title;
                return View(model);
            }

            try
            {
                if (attachments != null && attachments.Count > 0)
                {
                    question.AttachmentPath = await FileHelper.UploadMultipleFilesAsync(attachments, "questions", _env.WebRootPath);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("AttachmentPath", ex.Message);
                var survey = await _db.Surveys.FindAsync(question.SurveyId);
                ViewBag.SurveyTitle = survey?.Title;
                return View(model);
            }

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
