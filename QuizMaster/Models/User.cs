public class User
{
    public int Id { get; set; } 
    public long TelegramId { get; set; } 
    public string Username { get; set; } 
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    
    public int TotalAnswers { get; set; } = 0;
    public int CorrectAnswers { get; set; } = 0;
}