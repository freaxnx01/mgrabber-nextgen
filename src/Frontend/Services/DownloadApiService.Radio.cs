    // ========== Radio Methods ==========

    public async Task<List<RadioStationDto>?> GetRadioStationsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/radio/stations");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<RadioStationsResponseDto>();
            return result?.Stations?.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get radio stations");
            return null;
        }
    }

    public async Task<RadioPlaylistResponseDto?> GetRadioPlaylistAsync(string station, int limit = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/radio/playlist?station={Uri.EscapeDataString(station)}&limit={limit}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RadioPlaylistResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get radio playlist");
            return null;
        }
    }

    public async Task<RadioNowPlayingDto?> GetRadioNowPlayingAsync(string station)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/radio/now-playing?station={Uri.EscapeDataString(station)}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RadioNowPlayingDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get radio now playing");
            return null;
        }
    }

    public async Task<RadioDownloadResultDto?> DownloadRadioSongAsync(
        string station,
        string userId,
        string format,
        bool autoSelectBestMatch = true)
    {
        try
        {
            var request = new
            {
                Station = station,
                UserId = userId,
                Format = format,
                AutoSelectBestMatch = autoSelectBestMatch
            };

            var response = await _httpClient.PostAsJsonAsync("/api/radio/download-current", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RadioDownloadResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download radio song");
            return null;
        }
    }

    // ========== MusicBrainz Search Methods ==========
