using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using QuizMaster.Data;
using QuizMaster.Models;

namespace QuizMaster.Utils
{
    public class JsonImporter
    {
        public static async Task<(int addedCount, string error)> ImportQuestionsAsync(Stream jsonStream, AppDbContext db)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                var importedData = await JsonSerializer.DeserializeAsync<List<QuestionImportModel>>(jsonStream, options);

                if (importedData == null || importedData.Count == 0)
                {
                    return (0, "Файл порожній або має неправильний формат.");
                }                    

                int count = 0;

                foreach (var item in importedData)
                {
                    var subject = db.Subjects.FirstOrDefault(s => s.Name == item.SubjectName);
                    if (subject == null)
                    {
                        subject = new Subject { Name = item.SubjectName };
                        db.Subjects.Add(subject);
                        await db.SaveChangesAsync(); 
                    }

                    var newQuestion = new Question
                    {
                        SubjectId = subject.Id,
                        Text = item.Text,
                        OptionA = item.OptionA,
                        OptionB = item.OptionB,
                        OptionC = item.OptionC,
                        OptionD = item.OptionD,
                        CorrectOption = item.CorrectOption,
                        Explanation = item.Explanation
                    };

                    db.Questions.Add(newQuestion);
                    count++;
                }
                await db.SaveChangesAsync();
                return (count, string.Empty);
            }
            catch (Exception ex)
            {
                return (0, $"Помилка парсингу: {ex.Message}");
            }
        }
    }
}