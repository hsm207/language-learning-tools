namespace BAMFJsonToMarkdown.Commands;

public interface ICommand
{
    Task<int> ExecuteAsync();
}