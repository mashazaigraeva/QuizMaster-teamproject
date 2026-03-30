public class UserSession
{
    public long TelegramId { get; set; }
    
    public List<int> TicketQuestionIds { get; set; } = new List<int>();
    
    public int CurrentQuestionIndex { get; set; } = 0;
    public int CorrectAnswersCount { get; set; } = 0;
    
    public bool IsExamMode { get; set; }
    public int SubjectId { get; set; }
}