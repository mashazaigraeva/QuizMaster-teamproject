using Microsoft.EntityFrameworkCore;
using QuizMaster.Models;

namespace QuizMaster.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; } 

        public AppDbContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "quizmaster.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}
