using Microsoft.Extensions.Configuration;
using VetClinic.Application.Interfaces;

namespace VetClinic.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;

    public LocalFileStorage(IConfiguration configuration)
    {
        var configured = configuration["Storage:AttachmentsPath"] ?? "uploads/attachments";
        _basePath = Path.GetFullPath(configured);
    }

    public async Task<string> SaveAsync(string relativeFolder, string fileName, Stream content, CancellationToken ct = default)
    {
        var safeName = Path.GetFileName(fileName);
        var storedName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{safeName}";
        var folder = Path.Combine(_basePath, relativeFolder);
        Directory.CreateDirectory(folder);

        var absolutePath = Path.Combine(folder, storedName);
        await using var stream = File.Create(absolutePath);
        await content.CopyToAsync(stream, ct);

        return Path.Combine(relativeFolder, storedName).Replace('\\', '/');
    }

    public string? GetPhysicalPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;

        var full = Path.GetFullPath(Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase)) return null;
        return File.Exists(full) ? full : null;
    }
}