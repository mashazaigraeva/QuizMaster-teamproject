using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuizMaster.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient = new();

        public async Task<string> GenerateQuestionsAsync(string subject, int count)
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")?.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                return null;
            }

            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            var prompt = $"Згенеруй {count} тестових питань з дисципліни '{subject}'. " +
                        "ВАЖЛИВО: Усі питання, варіанти відповідей та пояснення мають бути написані ВИКЛЮЧНО УКРАЇНСЬКОЮ МОВОЮ! " +
                        "Поверни ВИКЛЮЧНО валідний масив JSON об'єктів. Без форматування ```json, без вступу чи висновків. " +
                        "Кожен об'єкт повинен мати такі поля (англійською): " +
                        "Text, OptionA, OptionB, OptionC, OptionD, CorrectOption (лише буква: A, B, C або D), Explanation. " +
                        "ВАЖЛИВО:Категорично заборонено використовувати синтаксис LaTeX (знаки $ чи $$) та складну розмітку! " +
                        "Всі математичні формули, ряди, інтеграли та границі записуй виключно звичайним текстом. " +
                        "Використовуй лінійний запис: x^2, sqrt(x), lim(x->0), інтеграл, Сума(n=1 до нескінченності) 1/корінь(n).";

            var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(requestUrl, content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Помилка Gemini] {response.StatusCode}: {responseText}");
                    return null;
                }

                using var doc = JsonDocument.Parse(responseText);
                var generatedText = doc.RootElement
                                       .GetProperty("candidates")[0]
                                       .GetProperty("content")
                                       .GetProperty("parts")[0]
                                       .GetProperty("text").GetString();

                return generatedText?.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Помилка Мережі] {ex.Message}");
                return null;
            }
        }

        public async Task<string> AskTutorAsync(string studentQuestion)
        {
            string systemPrompt = "Ти розумний і доброзичливий викладач університету. " +
                                "Дай коротку, зрозумілу і точну відповідь студенту на його запитання. " +
                                "ВАЖЛИВО: Не використовуй розмітку Markdown (зірочки **, решітки #) та HTML. Пиши звичайним чистим текстом.";

            string fullPrompt = $"{systemPrompt}\n\nЗапитання студента: {studentQuestion}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = fullPrompt } }
                    }
                }
            };

            string jsonPayload = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            string requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
            HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, content);

            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync();

            using (var document = System.Text.Json.JsonDocument.Parse(responseJson))
            {
                var root = document.RootElement;
                var answer = root
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return answer ?? "Вибач, я не зміг сформулювати відповідь.";
            }
        }
    }
}