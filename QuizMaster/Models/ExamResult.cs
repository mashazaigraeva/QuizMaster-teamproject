public class ExamResult
{
    public int Id { get; set; }
    
    public long TelegramId { get; set; } 
    
    public int SubjectId { get; set; }
    public Subject Subject { get; set; }
    
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}