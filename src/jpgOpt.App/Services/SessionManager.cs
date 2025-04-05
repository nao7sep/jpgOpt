using System;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using jpgOpt.App.Models;
using pawKit.Core.IO;

namespace jpgOpt.App.Services;

public class SessionManager
{
    private const string SessionFileName = "session.json";

    private const uint DefaultJpegQuality = 85;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public Session CreateSession(string outputDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(outputDirectoryPath))
            throw new ArgumentException("Output directory path cannot be null or empty.", nameof(outputDirectoryPath));

        PathOperations.EnsurePathIsFullyQualified(outputDirectoryPath, nameof(outputDirectoryPath));

        var normalizedDirectoryPath = PathOperations.NormalizePath(outputDirectoryPath);
        var filePath = PathOperations.CombineAbsolutePath(normalizedDirectoryPath, SessionFileName);
        var fileInfo = new FileInfo(filePath);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedAtUtc = DateTime.UtcNow,
            OutputDirectoryPath = normalizedDirectoryPath,
            OutputDirectory = new DirectoryInfo(normalizedDirectoryPath),
            OutputFile = fileInfo,
            JpegQuality = DefaultJpegQuality,
            IsJpegQualityLocked = false
        };

        return session;
    }

    public async Task SaveSessionToStreamAsync(Session session, Stream stream)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        session.LastModifiedAtUtc = DateTime.UtcNow;

        await JsonSerializer.SerializeAsync(stream, session, _jsonOptions);
    }

    public async Task<Session> LoadSessionFromStreamAsync(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        var session = await JsonSerializer.DeserializeAsync<Session>(stream, _jsonOptions);

        if (session == null)
            throw new JsonException("Failed to deserialize session from stream.");

        return session;
    }

    public async Task SaveSessionToFileAsync(Session session, string filePath)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        PathOperations.EnsurePathIsFullyQualified(filePath, nameof(filePath));

        var normalizedFilePath = PathOperations.NormalizePath(filePath);
        var directoryPath = Path.GetDirectoryName(normalizedFilePath);

        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        using var fileStream = new FileStream(normalizedFilePath, FileMode.Create, FileAccess.Write);
        await SaveSessionToStreamAsync(session, fileStream);
    }

    public async Task<Session> LoadSessionFromFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        PathOperations.EnsurePathIsFullyQualified(filePath, nameof(filePath));

        var normalizedFilePath = PathOperations.NormalizePath(filePath);

        if (!File.Exists(normalizedFilePath))
            throw new FileNotFoundException("Session file not found.", normalizedFilePath);

        using var fileStream = new FileStream(normalizedFilePath, FileMode.Open, FileAccess.Read);
        return await LoadSessionFromStreamAsync(fileStream);
    }
}