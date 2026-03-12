var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<IAudioExtractor, YtDlpExtractor>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Health check endpoint
app.MapGet("/api/health", async (IAudioExtractor extractor) =>
{
    var version = await extractor.GetVersionAsync();
    var ffmpegAvailable = File.Exists("/usr/bin/ffmpeg");
    
    return Results.Ok(new
    {
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        Services = new
        {
            YtDlp = version,
            Ffmpeg = ffmpegAvailable ? "Available" : "Not Found"
        }
    });
});

// Placeholder endpoints for Milestone 1B
app.MapGet("/api/search/youtube", () => Results.Ok(new { Message = "YouTube search - coming in Milestone 1B" }));
app.MapPost("/api/download/start", () => Results.Ok(new { Message = "Download start - coming in Milestone 1B" }));

app.Run();
