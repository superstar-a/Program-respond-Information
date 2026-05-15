using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLTTYKPH.Models
{
    public enum FeedbackStatus
    {
        New = 0,
        Processing = 1,
        Completed = 2
    }

    public class Feedback
    {
        public int Id { get; set; }

        [Display(Name = "Trạng thái")]
        public FeedbackStatus Status { get; set; } = FeedbackStatus.New;

        [Display(Name = "Thời gian gửi")]
        public DateTime? SubmittedAt { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public int SurveyId { get; set; }

        [ForeignKey("SurveyId")]
        public Survey? Survey { get; set; }

        public ICollection<FeedbackAnswer> FeedbackAnswers { get; set; } = new List<FeedbackAnswer>();
        public ICollection<ProcessingRecord> ProcessingRecords { get; set; } = new List<ProcessingRecord>();
    }
}
