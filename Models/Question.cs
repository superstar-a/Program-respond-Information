using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLTTYKPH.Models
{
    public enum QuestionType
    {
        Text = 0,
        SingleChoice = 1,
        MultipleChoice = 2,
        Rating = 3
    }

    public class Question
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nội dung câu hỏi không được để trống")]
        [Display(Name = "Nội dung câu hỏi")]
        public string Text { get; set; } = string.Empty;

        [Display(Name = "Loại câu hỏi")]
        public QuestionType Type { get; set; } = QuestionType.Text;

        [Display(Name = "Các lựa chọn (mỗi lựa chọn một dòng)")]
        [StringLength(500)]
        public string? Options { get; set; }

        [Display(Name = "Thứ tự")]
        public int Order { get; set; } = 0;

        [Required]
        public int SurveyId { get; set; }

        [ForeignKey("SurveyId")]
        public Survey? Survey { get; set; }

        public ICollection<FeedbackAnswer> FeedbackAnswers { get; set; } = new List<FeedbackAnswer>();
    }
}
