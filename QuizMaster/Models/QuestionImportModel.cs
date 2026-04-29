namespace QuizMaster.Models
{
    public class QuestionImportModel
    {
        public string SubjectName { get; set; }
        public string Text { get; set; }
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public string CorrectOption { get; set; }
        public string Explanation { get; set; }
    }
}