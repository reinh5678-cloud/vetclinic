namespace VetClinic.Application.Common;

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Errors { get; set; } = Array.Empty<string>();

    public static OperationResult Ok(string message = "OK") => new() { Success = true, Message = message };
    public static OperationResult Fail(string message, params string[] errors) =>
        new() { Success = false, Message = message, Errors = errors };
}