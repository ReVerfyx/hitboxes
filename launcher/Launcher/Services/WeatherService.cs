using System.Net.Http;
using System.Text.Json;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// Optional, off-by-default integration with OpenWeatherMap so the "Rain"
/// theme can follow real local weather instead of only a manual toggle.
/// Requires the user to supply their own free API key in settings.
/// </summary>
public static class WeatherService
{
    private static readonly HttpClient Http = new();

    public static async Task<bool> IsRainingAsync(string city, string apiKey)
    {
        try
        {
            string url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(city)}&appid={apiKey}";
            using var response = await Http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            var weatherArray = doc.RootElement.GetProperty("weather");
            foreach (var entry in weatherArray.EnumerateArray())
            {
                string main = entry.GetProperty("main").GetString() ?? string.Empty;
                if (main is "Rain" or "Drizzle" or "Thunderstorm")
                {
                    return true;
                }
            }
            return false;
        }
        catch
        {
            // Network/parse failure: fall back to non-rain rather than
            // interrupting the launcher's theme logic.
            return false;
        }
    }
}
