namespace CodeExplainer.Shared.Exceptions;

public class GlobalException : Exception
{
    public string Task { get; }

    public GlobalException(string task, string message)
        : base($"{task} failed, an error occurred: {message}")
    {
        Task = task;
    }

    public GlobalException(string task, string message, Exception inner)
        : base($"{task} failed, an error occurred: {message}", inner)
    {
        Task = task;
    }

    public GlobalException(string message)
        : base($"An error occurred: {message}")
    {
        Task = message;
    }
}