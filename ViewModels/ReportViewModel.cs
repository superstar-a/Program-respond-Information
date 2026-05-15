namespace QLTTYKPH.ViewModels
{
    public class ReportViewModel
    {
        public int TotalFeedbacks { get; set; }
        public int NewFeedbacks { get; set; }
        public int ProcessingFeedbacks { get; set; }
        public int CompletedFeedbacks { get; set; }
        public List<SurveyStatItem> SurveyStats { get; set; } = new();
        public List<CategoryStatItem> CategoryStats { get; set; } = new();
    }

    public class SurveyStatItem
    {
        public int SurveyId { get; set; }
        public string SurveyTitle { get; set; } = string.Empty;
        public int FeedbackCount { get; set; }
        public int CompletedCount { get; set; }
    }

    public class CategoryStatItem
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int FeedbackCount { get; set; }
    }
}
