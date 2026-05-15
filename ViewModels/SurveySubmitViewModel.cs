using QLTTYKPH.Models;

namespace QLTTYKPH.ViewModels
{
    public class SurveySubmitViewModel
    {
        public int SurveyId { get; set; }
        public string SurveyTitle { get; set; } = string.Empty;
        public string? SurveyDescription { get; set; }
        public List<QuestionAnswerViewModel> Questions { get; set; } = new();
    }

    public class QuestionAnswerViewModel
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; }
        public List<string> OptionList { get; set; } = new();
        public string? AnswerText { get; set; }
        public List<string> SelectedOptions { get; set; } = new();
    }
}
