using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MusicGrabber.Modules.Download.Application;
using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Modules.Download.Application.Ports.Driving;
using MusicGrabber.Modules.Download.Application.UseCases.DeleteFile;
using MusicGrabber.Modules.Download.Application.UseCases.GetDownloadStats;
using MusicGrabber.Modules.Download.Application.UseCases.GetJobStatus;
using MusicGrabber.Modules.Download.Application.UseCases.ListUserFiles;
using MusicGrabber.Modules.Download.Application.UseCases.SearchYouTube;
using MusicGrabber.Modules.Download.Application.UseCases.StartDownload;
using MusicGrabber.Modules.Download.Application.UseCases.StartPlaylistDownload;
using MusicGrabber.Modules.Download.Infrastructure.Adapters;
using MusicGrabber.Modules.Download.Infrastructure.Adapters.Persistence;
using MusicGrabber.Shared.Contracts;

namespace MusicGrabber.Modules.Download.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDownloadModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DownloadDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Default")));

        // Repositories
        services.AddScoped<IDownloadJobRepository, DownloadJobRepository>();

        // Infrastructure adapters
        services.AddScoped<IAudioExtractor, YtDlpExtractor>();
        services.AddScoped<IAudioNormalizer, FfmpegNormalizer>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddHttpClient<IYouTubeSearchService, YouTubeDataApiService>();

        // Use case handlers
        services.AddScoped<SearchYouTubeHandler>();
        services.AddScoped<StartDownloadHandler>();
        services.AddScoped<StartPlaylistDownloadHandler>();
        services.AddScoped<GetJobStatusHandler>();
        services.AddScoped<ListUserFilesHandler>();
        services.AddScoped<DeleteFileHandler>();
        services.AddScoped<GetDownloadStatsHandler>();

        // Composite driving port (errata #15)
        services.AddScoped<IDownloadService, DownloadService>();

        // Facade for cross-module communication (errata #15 — delegates to IDownloadService)
        services.AddScoped<IDownloadFacade, DownloadFacadeAdapter>();

        return services;
    }
}
