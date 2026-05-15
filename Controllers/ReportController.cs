using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLTTYKPH.Data;
using QLTTYKPH.Helpers;
using QLTTYKPH.Models;
using QLTTYKPH.ViewModels;

namespace QLTTYKPH.Controllers
{
    public class ReportController : Controller
    {
        private readonly AppDbContext _db;

        public ReportController(AppDbContext db)
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

        public async Task<IActionResult> Index()
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var vm = new ReportViewModel
            {
                TotalFeedbacks = await _db.Feedbacks.CountAsync(),
                NewFeedbacks = await _db.Feedbacks.CountAsync(f => f.Status == FeedbackStatus.New),
                ProcessingFeedbacks = await _db.Feedbacks.CountAsync(f => f.Status == FeedbackStatus.Processing),
                CompletedFeedbacks = await _db.Feedbacks.CountAsync(f => f.Status == FeedbackStatus.Completed),
                SurveyStats = await _db.Surveys
                    .Select(s => new SurveyStatItem
                    {
                        SurveyId = s.Id,
                        SurveyTitle = s.Title,
                        FeedbackCount = s.Feedbacks.Count,
                        CompletedCount = s.Feedbacks.Count(f => f.Status == FeedbackStatus.Completed)
                    })
                    .OrderByDescending(x => x.FeedbackCount)
                    .ToListAsync(),
                CategoryStats = await _db.Categories
                    .Select(c => new CategoryStatItem
                    {
                        CategoryId = c.Id,
                        CategoryName = c.Name,
                        FeedbackCount = c.Surveys.SelectMany(s => s.Feedbacks).Count()
                    })
                    .OrderByDescending(x => x.FeedbackCount)
                    .ToListAsync()
            };

            return View(vm);
        }

        // Chi tiết phản hồi theo khảo sát
        public async Task<IActionResult> SurveyDetail(int surveyId)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var survey = await _db.Surveys
                .Include(s => s.Questions)
                .Include(s => s.Feedbacks)
                    .ThenInclude(f => f.FeedbackAnswers)
                .Include(s => s.Category)
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Id == surveyId);

            if (survey == null) return NotFound();
            return View(survey);
        }

        // Xuất Excel tổng hợp
        public async Task<IActionResult> ExportExcel(int? surveyId)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            using var wb = new XLWorkbook();

            if (surveyId.HasValue)
            {
                var survey = await _db.Surveys
                    .Include(s => s.Questions)
                    .Include(s => s.Feedbacks)
                        .ThenInclude(f => f.User)
                    .Include(s => s.Feedbacks)
                        .ThenInclude(f => f.FeedbackAnswers)
                    .FirstOrDefaultAsync(s => s.Id == surveyId);

                if (survey == null) return NotFound();

                var ws = wb.Worksheets.Add("Phản hồi");
                int col = 1;
                ws.Cell(1, col++).Value = "STT";
                ws.Cell(1, col++).Value = "Họ tên";
                ws.Cell(1, col++).Value = "Lớp";
                ws.Cell(1, col++).Value = "Thời gian gửi";
                ws.Cell(1, col++).Value = "Trạng thái";

                foreach (var q in survey.Questions)
                    ws.Cell(1, col++).Value = q.Text;

                ws.Row(1).Style.Font.Bold = true;
                ws.Row(1).Style.Fill.BackgroundColor = XLColor.LightBlue;

                int row = 2;
                int stt = 1;
                foreach (var fb in survey.Feedbacks.OrderByDescending(f => f.SubmittedAt))
                {
                    col = 1;
                    ws.Cell(row, col++).Value = stt++;
                    ws.Cell(row, col++).Value = fb.User?.FullName ?? "";
                    ws.Cell(row, col++).Value = fb.User?.Class ?? "";
                    ws.Cell(row, col++).Value = fb.SubmittedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
                    ws.Cell(row, col++).Value = fb.Status switch
                    {
                        FeedbackStatus.New => "Mới",
                        FeedbackStatus.Processing => "Đang xử lý",
                        FeedbackStatus.Completed => "Hoàn thành",
                        _ => ""
                    };

                    foreach (var q in survey.Questions)
                    {
                        var ans = fb.FeedbackAnswers.FirstOrDefault(fa => fa.QuestionId == q.Id);
                        ws.Cell(row, col++).Value = ans?.AnswerText ?? "";
                    }
                    row++;
                }

                ws.Columns().AdjustToContents();
            }
            else
            {
                // Tổng hợp tất cả
                var ws = wb.Worksheets.Add("Tổng hợp");
                ws.Cell(1, 1).Value = "Khảo sát";
                ws.Cell(1, 2).Value = "Tổng phản hồi";
                ws.Cell(1, 3).Value = "Mới";
                ws.Cell(1, 4).Value = "Đang xử lý";
                ws.Cell(1, 5).Value = "Hoàn thành";
                ws.Row(1).Style.Font.Bold = true;
                ws.Row(1).Style.Fill.BackgroundColor = XLColor.LightBlue;

                var surveys = await _db.Surveys.Include(s => s.Feedbacks).ToListAsync();
                int row = 2;
                foreach (var s in surveys)
                {
                    ws.Cell(row, 1).Value = s.Title;
                    ws.Cell(row, 2).Value = s.Feedbacks.Count;
                    ws.Cell(row, 3).Value = s.Feedbacks.Count(f => f.Status == FeedbackStatus.New);
                    ws.Cell(row, 4).Value = s.Feedbacks.Count(f => f.Status == FeedbackStatus.Processing);
                    ws.Cell(row, 5).Value = s.Feedbacks.Count(f => f.Status == FeedbackStatus.Completed);
                    row++;
                }
                ws.Columns().AdjustToContents();

                var ws2 = wb.Worksheets.Add("Theo danh mục");
                ws2.Cell(1, 1).Value = "Danh mục";
                ws2.Cell(1, 2).Value = "Số phản hồi";
                ws2.Row(1).Style.Font.Bold = true;
                ws2.Row(1).Style.Fill.BackgroundColor = XLColor.LightGreen;
                var cats = await _db.Categories
                    .Include(c => c.Surveys)
                        .ThenInclude(s => s.Feedbacks)
                    .ToListAsync();
                row = 2;
                foreach (var c in cats)
                {
                    ws2.Cell(row, 1).Value = c.Name;
                    ws2.Cell(row, 2).Value = c.Surveys.SelectMany(s => s.Feedbacks).Count();
                    row++;
                }
                ws2.Columns().AdjustToContents();
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Seek(0, SeekOrigin.Begin);
            string fileName = surveyId.HasValue
                ? $"PhanHoi_KhaoSat_{surveyId}_{DateTime.Now:yyyyMMdd}.xlsx"
                : $"BaoCao_TongHop_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
