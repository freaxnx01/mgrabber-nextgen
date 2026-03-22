namespace MusicGrabber.Modules.Download.Application.Ports.Driven;

public interface IFileStorage
{
    Task DeleteAsync(string filePath);
    Task<bool> ExistsAsync(string filePath);
}
