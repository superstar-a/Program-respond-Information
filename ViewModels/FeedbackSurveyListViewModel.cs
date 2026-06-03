namespace QLTTYKPH.ViewModels
{
    public class FeedbackSurveyListViewModel
    {
        public int SurveyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? DepartmentName { get; set; }
        public string? ClassName { get; set; }
        public bool IsPublished { get; set; }
        
        public int TotalFeedbacks { get; set; }
        public int NewFeedbacks { get; set; }
        public int ProcessingFeedbacks { get; set; }
        public int CompletedFeedbacks { get; set; }
    }
}
