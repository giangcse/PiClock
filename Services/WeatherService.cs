using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PiClock.Models;

namespace PiClock.Services;

public class WeatherService
{
    private readonly LocationConfig _location;
    private readonly HttpClient _httpClient;

    public WeatherService(LocationConfig location)
    {
        _location = location;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PiClockApp/2.0");
    }

    public async Task<WeatherData?> GetCurrentWeatherAsync()
    {
        try
        {
            string url = $"https://api.open-meteo.com/v1/forecast?latitude={_location.Latitude}&longitude={_location.Longitude}&current_weather=true";
            var json = await _httpClient.GetStringAsync(url);
            var data = JObject.Parse(json);

            var current = data["current_weather"];
            if (current != null)
            {
                return new WeatherData
                {
                    Temperature = current["temperature"]?.Value<double>() ?? 0,
                    WeatherCode = current["weathercode"]?.Value<int>() ?? 0
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi lấy thời tiết: {ex.Message}");
        }
        return null;
    }

    public static string GetWeatherDescription(int code) => code switch
    {
        0 => "Trời quang",
        1 or 2 or 3 => "Có mây",
        45 or 48 => "Sương mù",
        >= 51 and <= 67 => "Mưa",
        >= 95 => "Giông bão",
        _ => "Không rõ"
    };

    public static string GetWeatherIcon(int code)
    {
        if (code == 0) return "☀";
        if (code <= 3) return "☁";
        if (code >= 51) return "🌧";
        return "☁";
    }
}

public class WeatherData
{
    public double Temperature { get; set; }
    public int WeatherCode { get; set; }
}
