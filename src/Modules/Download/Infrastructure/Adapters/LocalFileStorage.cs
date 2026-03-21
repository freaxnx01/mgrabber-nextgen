using MusicGrabber.Modules.Download.Application.Ports.Driven;

namespace MusicGrabber.Modules.Download.Infrastructure.Adapters;

public sealed class LocalFileStorage : IFileStorage
{
    public Task DeleteAsync(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string filePath)
    {
        return Task.FromResult(File.Exists(filePath));
    }
}
