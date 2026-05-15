using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLTTYKPH.Models
{
    public class FeedbackAnswer
    {
        public int Id { get; set; }

        [Display(Name = "Câu trả lời")]
        public string? AnswerText { get; set; }

        [Required]
        public int FeedbackId { get; set; }

        [ForeignKey("FeedbackId")]
        public Feedback? Feedback { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [ForeignKey("QuestionId")]
        public Question? Question { get; set; }
    }
}
