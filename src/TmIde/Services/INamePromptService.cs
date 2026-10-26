namespace TmIde.Services;

public interface INamePromptService
{
    string? RequestName(
        string title,
        string message,
        string initialName = "");
}