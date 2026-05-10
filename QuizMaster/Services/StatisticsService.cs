using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Data;

namespace QuizMaster.Services
{
    public class StatisticsService
    {
        private readonly AppDbContext _context;

        public StatisticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetUserStatisticsAsync(long telegramId)
        {
            var userResults = await _context.ExamResults
                .Include(tr => tr.Subject)
                .Where(tr => tr.TelegramId == telegramId)
                .ToListAsync();

            if (!userResults.Any())
            {
                return "📈 У тебе ще немає пройдених тестів. Час це виправити!";
            }

            int totalTestsTaken = userResults.Count;
            int totalQuestionsAnswered = userResults.Sum(tr => tr.TotalQuestions);
            int totalCorrectAnswers = userResults.Sum(tr => tr.CorrectAnswers);
            
            double overallAccuracy = totalQuestionsAnswered > 0 
                ? (double)totalCorrectAnswers / totalQuestionsAnswered * 100 
                : 0;

            var report = new StringBuilder();
            report.AppendLine("📊 **Твоя персональна статистика:**");
            report.AppendLine($"📝 Всього пройдено тестів: {totalTestsTaken}");
            report.AppendLine($"🎯 Загальна точність: {Math.Round(overallAccuracy, 1)}%\n");

            var subjectStats = userResults
                .GroupBy(tr => tr.Subject.Name)
                .Select(g => new
                {
                    SubjectName = g.Key,
                    TotalQ = g.Sum(x => x.TotalQuestions),
                    CorrectQ = g.Sum(x => x.CorrectAnswers),
                    Accuracy = g.Sum(x => x.TotalQuestions) > 0 
                        ? (double)g.Sum(x => x.CorrectAnswers) / g.Sum(x => x.TotalQuestions) * 100 
                        : 0
                })
                .ToList();

            report.AppendLine("📚 **Успішність за дисциплінами:**");
            foreach (var stat in subjectStats)
            {
                report.AppendLine($"- {stat.SubjectName}: {Math.Round(stat.Accuracy, 1)}% ({stat.CorrectQ}/{stat.TotalQ})");
            }

            var weakestSubject = subjectStats.OrderBy(s => s.Accuracy).FirstOrDefault();
            if (weakestSubject != null)
            {
                report.AppendLine($"\n⚠️ **Твоє слабке місце:** {weakestSubject.SubjectName}.");
                report.AppendLine("💡 Рекомендуємо приділити більше уваги цій темі під час наступних тренувань!");
            }

            return report.ToString();
        }
    }
}