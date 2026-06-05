using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLTTYKPH.Data;
using QLTTYKPH.Helpers;
using QLTTYKPH.Models;
using QLTTYKPH.ViewModels;

namespace QLTTYKPH.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly AppDbContext _db;

        public FeedbackController(AppDbContext db)
        {
            _db = db;
        }

        // Danh sách khảo sát đang mở (dành cho sinh viên)
        public async Task<IActionResult> OpenSurveys()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!SessionHelper.IsStudent(HttpContext.Session))
                return RedirectToAction("Index", "Home");

            int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var user = await _db.Users.FindAsync(userId);

            var now = DateTime.Now;
            var surveys = await _db.Surveys
                .Include(s => s.Category)
                .Include(s => s.Department)
                .Where(s => s.IsPublished && 
                            (s.StartDate == null || s.StartDate <= now) &&
                            (s.EndDate == null || s.EndDate >= now))
                .ToListAsync();

            // Lọc theo lớp nếu có
            if (user?.ClassId != null)
            {
                surveys = surveys.Where(s =>
                    s.ClassId == null || s.ClassId == user.ClassId
                ).ToList();
            }

            // Lấy danh sách khảo sát đã nộp
            var submittedIds = await _db.Feedbacks
                .Where(f => f.UserId == userId)
                .Select(f => f.SurveyId)
                .ToListAsync();

            ViewBag.SubmittedIds = submittedIds;
            return View(surveys);
        }

        // Form điền khảo sát
        [HttpGet]
        public async Task<IActionResult> TakeSurvey(int surveyId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!SessionHelper.IsStudent(HttpContext.Session))
                return RedirectToAction("Index", "Home");

            int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;

            // Kiểm tra đã nộp chưa
            if (await _db.Feedbacks.AnyAsync(f => f.UserId == userId && f.SurveyId == surveyId))
            {
                TempData["Error"] = "Bạn đã thực hiện khảo sát này rồi!";
                return RedirectToAction("OpenSurveys");
            }

            var now = DateTime.Now;
            var survey = await _db.Surveys
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.Id == surveyId && s.IsPublished &&
                                          (s.StartDate == null || s.StartDate <= now) &&
                                          (s.EndDate == null || s.EndDate >= now));

            if (survey == null)
            {
                TempData["Error"] = "Khảo sát không tồn tại hoặc đã đóng!";
                return RedirectToAction("OpenSurveys");
            }

            var vm = new SurveySubmitViewModel
            {
                SurveyId = survey.Id,
                SurveyTitle = survey.Title,
                SurveyDescription = survey.Description,
                Questions = survey.Questions.Select(q => new QuestionAnswerViewModel
                {
                    QuestionId = q.Id,
                    Order = q.Order,
                    Text = q.Text,
                    Type = q.Type,
                    AttachmentPath = q.AttachmentPath,
                    OptionList = string.IsNullOrWhiteSpace(q.Options)
                        ? new List<string>()
                        : q.Options.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToList()
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TakeSurvey(SurveySubmitViewModel vm)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;

            var feedback = new Feedback
            {
                UserId = userId,
                SurveyId = vm.SurveyId,
                Status = FeedbackStatus.New,
                SubmittedAt = DateTime.Now
            };
            _db.Feedbacks.Add(feedback);
            await _db.SaveChangesAsync();

            foreach (var qa in vm.Questions)
            {
                string answerText = qa.Type == QuestionType.MultipleChoice
                    ? string.Join(", ", qa.SelectedOptions)
                    : qa.AnswerText ?? string.Empty;

                _db.FeedbackAnswers.Add(new FeedbackAnswer
                {
                    FeedbackId = feedback.Id,
                    QuestionId = qa.QuestionId,
                    AnswerText = answerText
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Gửi phản hồi thành công! Cảm ơn bạn đã tham gia khảo sát.";
            return RedirectToAction("OpenSurveys");
        }

        // Lịch sử phản hồi của sinh viên
        public async Task<IActionResult> MyFeedbacks()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!SessionHelper.IsStudent(HttpContext.Session))
                return RedirectToAction("Index", "Home");

            int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var feedbacks = await _db.Feedbacks
                .Include(f => f.Survey)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync();
            return View(feedbacks);
        }

        // Xem chi tiết phản hồi (của sinh viên)
        public async Task<IActionResult> ViewMyFeedback(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var feedback = await _db.Feedbacks
                .Include(f => f.Survey)
                .Include(f => f.FeedbackAnswers)
                    .ThenInclude(fa => fa.Question)
                .Include(f => f.ProcessingRecords)
                    .ThenInclude(pr => pr.HandlerUser)
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (feedback == null) return NotFound();
            return View(feedback);
        }

        // Danh sách tất cả Khảo sát (Staff/Admin Overview)
        public async Task<IActionResult> All(string? search)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (SessionHelper.IsStudent(HttpContext.Session))
                return RedirectToAction("Index", "Home");

            int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var user = await _db.Users.FindAsync(userId);
            bool isAdmin = SessionHelper.IsAdmin(HttpContext.Session);

            var query = _db.Surveys
                .Include(s => s.Category)
                .Include(s => s.Department)
                .Include(s => s.Class)
                .Include(s => s.Feedbacks)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.Title.Contains(search));
            }

            if (!isAdmin && user != null)
            {
                // Nhân viên chỉ thấy Khảo sát của Phòng ban mình hoặc Lớp mình quản lý
                query = query.Where(s => 
                    (user.Department != null && s.Department != null && s.Department.Name == user.Department) ||
                    (user.ClassId != null && s.ClassId == user.ClassId)
                );
            }

            var surveys = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();

            var vmList = surveys.Select(s => new FeedbackSurveyListViewModel
            {
                SurveyId = s.Id,
                Title = s.Title,
                CategoryName = s.Category?.Name,
                DepartmentName = s.Department?.Name,
                ClassName = s.Class?.Name,
                IsPublished = s.IsPublished,
                TotalFeedbacks = s.Feedbacks.Count,
                NewFeedbacks = s.Feedbacks.Count(f => f.Status == FeedbackStatus.New),
                ProcessingFeedbacks = s.Feedbacks.Count(f => f.Status == FeedbackStatus.Processing),
                CompletedFeedbacks = s.Feedbacks.Count(f => f.Status == FeedbackStatus.Completed)
            }).ToList();

            ViewBag.Search = search;
            return View(vmList);
        }

        // Danh sách phản hồi của 1 khảo sát (Staff/Admin)
        public async Task<IActionResult> BySurvey(int surveyId, string? status)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (SessionHelper.IsStudent(HttpContext.Session))
                return RedirectToAction("Index", "Home");

            int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var user = await _db.Users.FindAsync(userId);
            bool isAdmin = SessionHelper.IsAdmin(HttpContext.Session);

            var survey = await _db.Surveys
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Id == surveyId);

            if (survey == null) return RedirectToAction("All");

            if (!isAdmin && user != null)
            {
                bool isDeptMatch = user.Department != null && survey.Department != null && survey.Department.Name == user.Department;
                bool isClassMatch = user.ClassId != null && survey.ClassId == user.ClassId;
                if (!isDeptMatch && !isClassMatch)
                {
                    TempData["Error"] = "Bạn không có quyền xem phản hồi của khảo sát này.";
                    return RedirectToAction("All");
                }
            }

            var query = _db.Feedbacks
                .Include(f => f.User)
                    .ThenInclude(u => u!.Class)
                .Include(f => f.Survey)
                .Where(f => f.SurveyId == surveyId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<FeedbackStatus>(status, out var statusEnum))
                query = query.Where(f => f.Status == statusEnum);

            ViewBag.Survey = survey;
            ViewBag.Status = status;

            return View(await query.OrderByDescending(f => f.SubmittedAt).ToListAsync());
        }

        // Xem chi tiết phản hồi (Staff/Admin)
        public async Task<IActionResult> Details(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (SessionHelper.IsStudent(HttpContext.Session))
                return RedirectToAction("Index", "Home");

            var feedback = await _db.Feedbacks
                .Include(f => f.User)
                .Include(f => f.Survey)
                .Include(f => f.FeedbackAnswers)
                    .ThenInclude(fa => fa.Question)
                .Include(f => f.ProcessingRecords)
                    .ThenInclude(pr => pr.HandlerUser)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (feedback == null) return NotFound();
            return View(feedback);
        }
    }
}
