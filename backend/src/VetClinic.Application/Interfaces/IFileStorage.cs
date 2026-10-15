namespace VetClinic.Application.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(string relativeFolder, string fileName, Stream content, CancellationToken ct = default);
    string? GetPhysicalPath(string? relativePath);
    void Delete(string? relativePath);
}
