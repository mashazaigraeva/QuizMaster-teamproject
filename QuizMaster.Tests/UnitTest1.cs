using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using QuizMaster.Services;
using QuizMaster.Models;
using QuizMaster.Data;

namespace QuizMaster.Tests
{
    public class TestDbContext : AppDbContext
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(_dbName);
        }
    }

    [TestFixture]
    public class TestLogicServiceTests
    {
        private static long _userIdCounter = 1000;

        private long GetUniqueUserId()
        {
            return Interlocked.Increment(ref _userIdCounter);
        }

        private Question CreateValidQuestion(int id, int subjectId, string correctOption = "A")
        {
            return new Question
            {
                Id = id,
                SubjectId = subjectId,
                Text = $"Тестове питання {id}?",
                OptionA = "Варіант А",
                OptionB = "Варіант Б",
                OptionC = "Варіант В",
                OptionD = "Варіант Г",
                CorrectOption = correctOption,
                Explanation = "Тестове пояснення."
            };
        }

        [Test]
        public async Task StartNewTestAsync_ThrowsException_IfInsufficientQuestions()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Основи програмування" });
            db.Questions.Add(CreateValidQuestion(1, 1));
            await db.SaveChangesAsync();

            var ex = Assert.ThrowsAsync<Exception>(async delegate 
            {
                await service.StartNewTestAsync(userId, 1, 5, false);
            });
            
            Assert.That(ex.Message, Does.Contain("Недостатньо питань"));
        }

        [Test]
        public async Task StartNewTestAsync_CreatesSession_WithCorrectQuestionsCount()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Алгоритми та структури даних" });
            db.Questions.AddRange(
                CreateValidQuestion(1, 1),
                CreateValidQuestion(2, 1),
                CreateValidQuestion(3, 1)
            );
            await db.SaveChangesAsync();

            var session = await service.StartNewTestAsync(userId, 1, 2, true);

            Assert.That(session, Is.Not.Null);
            Assert.That(session.TelegramId, Is.EqualTo(userId));
            Assert.That(session.TicketQuestionIds.Count, Is.EqualTo(2));
            Assert.That(session.IsExamMode, Is.True);
            Assert.That(session.CurrentQuestionIndex, Is.EqualTo(0));
        }

        [Test]
        public async Task StartNewTestAsync_ExactNumberOfQuestions_CreatesSession()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Комп'ютерні мережі" });
            db.Questions.Add(CreateValidQuestion(1, 1));
            await db.SaveChangesAsync();

            var session = await service.StartNewTestAsync(userId, 1, 1, false);

            Assert.That(session.TicketQuestionIds.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetCurrentQuestionAsync_ReturnsNull_IfSessionDoesNotExist()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long nonExistentUserId = GetUniqueUserId();

            var question = await service.GetCurrentQuestionAsync(nonExistentUserId);

            Assert.That(question, Is.Null);
        }

        [Test]
        public async Task GetCurrentQuestionAsync_ReturnsNull_IfIndexExceedsTotalQuestions()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Математичний аналіз" });
            db.Questions.Add(CreateValidQuestion(1, 1, "A"));
            await db.SaveChangesAsync();

            await service.StartNewTestAsync(userId, 1, 1, false);
            await service.ProcessAnswerAsync(userId, "A"); 

            var nextQuestion = await service.GetCurrentQuestionAsync(userId);
            Assert.That(nextQuestion, Is.Null);
        }

        [Test]
        public async Task GetCurrentQuestionAsync_ReturnsCorrectQuestionData()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Основи програмування" });
            db.Questions.Add(CreateValidQuestion(100, 1, "A"));
            await db.SaveChangesAsync();

            await service.StartNewTestAsync(userId, 1, 1, false);

            var question = await service.GetCurrentQuestionAsync(userId);

            Assert.That(question, Is.Not.Null);
            Assert.That(question.Text, Is.EqualTo("Тестове питання 100?"));
            Assert.That(question.CorrectOption, Is.EqualTo("A"));
        }

        [Test]
        public async Task GetCurrentQuestionAsync_WithoutAnswering_ReturnsSameQuestion()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Алгоритми та структури даних" });
            db.Questions.Add(CreateValidQuestion(1, 1, "A"));
            await db.SaveChangesAsync();

            await service.StartNewTestAsync(userId, 1, 1, false);

            var firstCall = await service.GetCurrentQuestionAsync(userId);
            var secondCall = await service.GetCurrentQuestionAsync(userId); 

            Assert.That(firstCall.Id, Is.EqualTo(secondCall.Id));
        }

        [Test]
        public void ProcessAnswerAsync_ThrowsException_IfNoSession()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            Assert.ThrowsAsync<Exception>(async delegate 
            {
                await service.ProcessAnswerAsync(userId, "A");
            });
        }

        [Test]
        public async Task ProcessAnswerAsync_CorrectAnswer_ReturnsTrue_And_IncrementsScore()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Комп'ютерні мережі" });
            db.Questions.Add(CreateValidQuestion(1, 1, "C"));
            await db.SaveChangesAsync();

            var session = await service.StartNewTestAsync(userId, 1, 1, false);

            bool isCorrect = await service.ProcessAnswerAsync(userId, "C");

            Assert.That(isCorrect, Is.True);
            Assert.That(session.CorrectAnswersCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ProcessAnswerAsync_IncorrectAnswer_B_ReturnsFalse_DoesNotIncrementScore()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Математичний аналіз" });
            db.Questions.Add(CreateValidQuestion(1, 1, "A"));
            await db.SaveChangesAsync();

            var session = await service.StartNewTestAsync(userId, 1, 1, false);

            bool isCorrect = await service.ProcessAnswerAsync(userId, "B");

            Assert.That(isCorrect, Is.False);
            Assert.That(session.CorrectAnswersCount, Is.EqualTo(0));
        }

        [Test]
        public async Task ProcessAnswerAsync_IncorrectAnswer_WrongData_ReturnsFalse_DoesNotIncrementScore()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Основи програмування" });
            db.Questions.Add(CreateValidQuestion(1, 1, "A"));
            await db.SaveChangesAsync();

            var session = await service.StartNewTestAsync(userId, 1, 1, false);

            bool isCorrect = await service.ProcessAnswerAsync(userId, "wrong_data");

            Assert.That(isCorrect, Is.False);
            Assert.That(session.CorrectAnswersCount, Is.EqualTo(0));
        }

        [Test]
        public async Task ProcessAnswerAsync_IsCaseInsensitive_Lowercase_a()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Алгоритми та структури даних" });
            db.Questions.Add(CreateValidQuestion(1, 1, "A"));
            await db.SaveChangesAsync();

            await service.StartNewTestAsync(userId, 1, 1, false);

            bool isCorrect = await service.ProcessAnswerAsync(userId, "a");

            Assert.That(isCorrect, Is.True);
        }

        [Test]
        public async Task ProcessAnswerAsync_IsCaseInsensitive_Uppercase_A()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Комп'ютерні мережі" });
            db.Questions.Add(CreateValidQuestion(1, 1, "A"));
            await db.SaveChangesAsync();

            await service.StartNewTestAsync(userId, 1, 1, false);

            bool isCorrect = await service.ProcessAnswerAsync(userId, "A");

            Assert.That(isCorrect, Is.True);
        }

        [Test]
        public async Task ProcessAnswerAsync_AlwaysIncrementsCurrentQuestionIndex()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Математичний аналіз" });
            db.Questions.AddRange(
                CreateValidQuestion(1, 1, "A"),
                CreateValidQuestion(2, 1, "B")
            );
            await db.SaveChangesAsync();

            var session = await service.StartNewTestAsync(userId, 1, 2, false);

            await service.ProcessAnswerAsync(userId, "Z"); 

            Assert.That(session.CurrentQuestionIndex, Is.EqualTo(1)); 
        }

        [Test]
        public async Task FinishTestAsync_ReturnsErrorMessage_IfSessionNotFound()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            string result = await service.FinishTestAsync(userId);

            Assert.That(result, Does.Contain("Помилка"));
        }

        [Test]
        public async Task FinishTestAsync_SavesExamResult_ToDatabase()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Основи програмування" });
            db.Questions.Add(CreateValidQuestion(1, 1, "A"));
            await db.SaveChangesAsync();

            await service.StartNewTestAsync(userId, 1, 1, false);
            await service.ProcessAnswerAsync(userId, "A"); 

            await service.FinishTestAsync(userId);

            var savedResult = await db.ExamResults.FirstOrDefaultAsync(r => r.TelegramId == userId);
            Assert.That(savedResult, Is.Not.Null);
            Assert.That(savedResult.TotalQuestions, Is.EqualTo(1));
            Assert.That(savedResult.CorrectAnswers, Is.EqualTo(1));
        }

        [Test]
        public async Task FinishTestAsync_UpdatesUserTotalStats()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Алгоритми та структури даних" });
            db.Questions.Add(CreateValidQuestion(1, 1, "A"));
            
            db.Users.Add(new User { TelegramId = userId, Username = "Test", TotalAnswers = 5, CorrectAnswers = 2 });
            await db.SaveChangesAsync();

            await service.StartNewTestAsync(userId, 1, 1, false);
            await service.ProcessAnswerAsync(userId, "A"); 

            await service.FinishTestAsync(userId);

            var updatedUser = await db.Users.FirstOrDefaultAsync(u => u.TelegramId == userId);
            Assert.That(updatedUser.TotalAnswers, Is.EqualTo(6)); 
            Assert.That(updatedUser.CorrectAnswers, Is.EqualTo(3)); 
        }

        [Test]
        public async Task FinishTestAsync_ZeroCorrectAnswers_CalculatesZeroPercentage()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Комп'ютерні мережі" });
            db.Questions.Add(CreateValidQuestion(1, 1, "A"));
            await db.SaveChangesAsync();

            await service.StartNewTestAsync(userId, 1, 1, false);
            await service.ProcessAnswerAsync(userId, "B"); 

            string result = await service.FinishTestAsync(userId);

            Assert.That(result, Does.Contain("0 з 1"));
            Assert.That(result, Does.Contain("0%"));
        }
        
        [Test]
        public async Task FinishTestAsync_AllCorrectAnswers_CalculatesHundredPercentage()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Математичний аналіз" });
            db.Questions.Add(CreateValidQuestion(1, 1, "A"));
            await db.SaveChangesAsync();

            await service.StartNewTestAsync(userId, 1, 1, false);
            await service.ProcessAnswerAsync(userId, "A"); 

            string result = await service.FinishTestAsync(userId);

            Assert.That(result, Does.Contain("1 з 1"));
            Assert.That(result, Does.Contain("100%"));
        }

        [Test]
        public async Task FinishTestAsync_RemovesSessionAfterCompletion()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Основи програмування" });
            db.Questions.Add(CreateValidQuestion(1, 1, "A"));
            await db.SaveChangesAsync();

            await service.StartNewTestAsync(userId, 1, 1, false);
            
            await service.FinishTestAsync(userId);

            var question = await service.GetCurrentQuestionAsync(userId);
            Assert.That(question, Is.Null);
        }

        [Test]
        public async Task StopSession_RemovesActiveSession()
        {
            using var db = new TestDbContext();
            var service = new TestLogicService(db);
            long userId = GetUniqueUserId();

            db.Subjects.Add(new Subject { Id = 1, Name = "Алгоритми та структури даних" });
            db.Questions.Add(CreateValidQuestion(1, 1, "A"));
            await db.SaveChangesAsync();

            await service.StartNewTestAsync(userId, 1, 1, false);

            service.StopSession(userId);

            var question = await service.GetCurrentQuestionAsync(userId);
            Assert.That(question, Is.Null); 
        }
    }
}