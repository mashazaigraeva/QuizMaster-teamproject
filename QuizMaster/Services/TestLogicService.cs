using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Data;
using QuizMaster.Models;

namespace QuizMaster.Services
{
    public class TestLogicService
    {
        private readonly AppDbContext _context;
        private static readonly Dictionary<long, UserSession> ActiveSessions = new();

        public TestLogicService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserSession> StartNewTestAsync(long telegramId, int subjectId, int questionsCount, bool isExam)
        {
            var allQuestions = await _context.Questions
                .Where(q => q.SubjectId == subjectId)
                .Select(q => q.Id)
                .ToListAsync();

            if (allQuestions.Count < questionsCount)
            {
                throw new Exception($"Недостатньо питань. Є: {allQuestions.Count}, потрібно: {questionsCount}");
            }

            var randomizedTicketIds = allQuestions
                .OrderBy(x => Guid.NewGuid())
                .Take(questionsCount)
                .ToList();

            var session = new UserSession
            {
                TelegramId = telegramId,
                SubjectId = subjectId,
                TicketQuestionIds = randomizedTicketIds,
                IsExamMode = isExam,
                CurrentQuestionIndex = 0,
                CorrectAnswersCount = 0
            };

            ActiveSessions[telegramId] = session;

            return session;
        }

        public async Task<Question> GetCurrentQuestionAsync(long telegramId)
        {
            if (!ActiveSessions.TryGetValue(telegramId, out var session))
            {
                return null;
            }  

            if (session.CurrentQuestionIndex >= session.TicketQuestionIds.Count)
            {
                return null;
            }
                
            int currentQuestionId = session.TicketQuestionIds[session.CurrentQuestionIndex];
            
            return await _context.Questions.FirstOrDefaultAsync(q => q.Id == currentQuestionId);
        }

        public async Task<bool> ProcessAnswerAsync(long telegramId, string selectedOption)
        {
            if (!ActiveSessions.TryGetValue(telegramId, out var session))
            {
                throw new Exception("Сесію не знайдено.");
            }
                
            var currentQuestion = await GetCurrentQuestionAsync(telegramId);
            if (currentQuestion == null)
            {
                return false;
            } 
            bool isCorrect = string.Equals(currentQuestion.CorrectOption, selectedOption, StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                session.CorrectAnswersCount++;
            }

            session.CurrentQuestionIndex++;

            return isCorrect;
        }

        public string FinishTest(long telegramId)
        {
            if (!ActiveSessions.TryGetValue(telegramId, out var session))
            {
                return "Помилка: результати не знайдено.";
            }
                
            int total = session.TicketQuestionIds.Count;
            int correct = session.CorrectAnswersCount;
            double percentage = (double)correct / total * 100;

            ActiveSessions.Remove(telegramId);

            return $"Тест завершено!\nТвій результат: {correct} з {total} правильних відповідей ({Math.Round(percentage)}%).";
        }
    }
}