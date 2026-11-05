namespace MathMaturaProject .Data.Models;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TestId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswerIndex { get; set; }
    public int Marks { get; set; } = 1;
    public string Explanation { get; set; } = string.Empty;

    public Test? Test { get; set; }
}