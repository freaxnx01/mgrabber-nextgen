namespace MusicGrabber.Modules.Download.Domain;

public enum DownloadStatus
{
    Pending,
    Downloading,
    Normalizing,
    Completed,
    Failed
}
