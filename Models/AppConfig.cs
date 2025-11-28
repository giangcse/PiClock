using Newtonsoft.Json;
using System;
using System.IO;

namespace PiClock.Models;

public class AppConfig
{
    public LocationConfig Location { get; set; } = new();
    public TelegramConfig Telegram { get; set; } = new();
    public SlideshowConfig Slideshow { get; set; } = new();
    public WeatherConfig Weather { get; set; } = new();

    public static AppConfig Load()
    {
        try
        {
            string configPath = Path.Combine(AppContext.BaseDirectory, "Config", "AppConfig.json");
            
            if (!File.Exists(configPath))
            {
                var defaultConfig = new AppConfig();
                defaultConfig.Save();
                Console.WriteLine($"⚙️ Đã tạo file config mặc định: {configPath}");
                return defaultConfig;
            }

            string json = File.ReadAllText(configPath);
            var config = JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            Console.WriteLine("✅ Đã load config thành công");
            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi load config: {ex.Message}");
            return new AppConfig();
        }
    }

    public void Save()
    {
        try
        {
            string configPath = Path.Combine(AppContext.BaseDirectory, "Config", "AppConfig.json");
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(configPath, json);
            Console.WriteLine("💾 Đã lưu config");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi lưu config: {ex.Message}");
        }
    }
}

public class LocationConfig
{
    public double Latitude { get; set; } = 10.0668;
    public double Longitude { get; set; } = 105.9088;
    public string Name { get; set; } = "Vĩnh Long, Việt Nam";
}

public class TelegramConfig
{
    public string BotToken { get; set; } = "BOT_TOKEN_HERE";
    public int CheckIntervalSeconds { get; set; } = 5;
    public int MaxVisibleMessages { get; set; } = 3;
}

public class SlideshowConfig
{
    public int IntervalSeconds { get; set; } = 10;
    public string ImageFolder { get; set; } = "images";
    public int KenBurnsAnimationSeconds { get; set; } = 20;
}

public class WeatherConfig
{
    public int UpdateIntervalMinutes { get; set; } = 30;
}
