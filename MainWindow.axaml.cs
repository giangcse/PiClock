using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using AvaBitmap = Avalonia.Media.Imaging.Bitmap; 

namespace PiClock;

public partial class MainWindow : Window
{
    private DispatcherTimer _clockTimer;
    private DispatcherTimer _slideTimer;
    private string[] _imageFiles = Array.Empty<string>();
    private int _currentImageIndex = 0;
    
    // Config vị trí mặc định (Vĩnh Long)
    private const double LAT = 10.0668;
    private const double LON = 105.9088;

    public MainWindow()
    {
        InitializeComponent();
        
        // 1. Setup Đồng hồ (1 giây update 1 lần)
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (s, e) => UpdateTime();
        _clockTimer.Start();

        // 2. Setup Slideshow (10 giây đổi ảnh)
        _slideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _slideTimer.Tick += (s, e) => ChangeImage();
        
        // 3. Load ảnh tự động
        LoadImagesFromAutoFolder();

        // Khởi chạy
        UpdateTime();
        _ = UpdateWeatherAsync();
        
        // Timer update thời tiết 30p/lần
        var weatherTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        weatherTimer.Tick += async (s, e) => await UpdateWeatherAsync();
        weatherTimer.Start();
    }

    private void LoadImagesFromAutoFolder()
    {
        try 
        {
            string appPath = AppContext.BaseDirectory;
            string imagesPath = Path.Combine(appPath, "images");

            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            var extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
            _imageFiles = Directory.GetFiles(imagesPath)
                            .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                            .ToArray();

            if (_imageFiles.Length > 0)
            {
                _currentImageIndex = 0;
                ChangeImage(); 
                _slideTimer.Start();
            }
        }
        catch (Exception ex) { Console.WriteLine("Lỗi folder: " + ex.Message); }
    }

    private async void ChangeImage()
    {
        if (_imageFiles.Length == 0) return;

        // Fade Out
        BackgroundImage.Opacity = 0; 
        await Task.Delay(800); 

        try
        {
            string currentFile = _imageFiles[_currentImageIndex];
            using (var image = SixLabors.ImageSharp.Image.Load(currentFile))
            {
                image.Mutate(x => x.AutoOrient());
                image.Mutate(x => x.Resize(new ResizeOptions 
                {
                    Size = new SixLabors.ImageSharp.Size(1920, 1080),
                    Mode = ResizeMode.Max 
                }));

                using (var memoryStream = new MemoryStream())
                {
                    image.SaveAsBmp(memoryStream);
                    memoryStream.Position = 0;
                    BackgroundImage.Source = new AvaBitmap(memoryStream);
                }
            }
            _currentImageIndex = (_currentImageIndex + 1) % _imageFiles.Length;
        }
        catch { _currentImageIndex = (_currentImageIndex + 1) % _imageFiles.Length; }

        // Fade In
        BackgroundImage.Opacity = 1; 
    }

    private void UpdateTime()
    {
        var now = DateTime.Now;
        
        // Cập nhật theo style HTML mới: 00:00
        TxtTime.Text = now.ToString("HH:mm");
        
        var culture = new System.Globalization.CultureInfo("vi-VN");
        TxtDayName.Text = now.ToString("dddd", culture).ToUpper(); 
        TxtFullDate.Text = now.ToString("dd.MM.yyyy");
    }

    private async Task UpdateWeatherAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "PiClockApp/1.0");

            string url = $"https://api.open-meteo.com/v1/forecast?latitude={LAT}&longitude={LON}&current_weather=true";
            var json = await client.GetStringAsync(url);
            var data = JObject.Parse(json);
            
            var current = data["current_weather"];
            if (current != null)
            {
                double temp = current["temperature"]?.Value<double>() ?? 0;
                int code = current["weathercode"]?.Value<int>() ?? 0;

                TxtTemp.Text = $"{Math.Round(temp)}°";
                TxtWeatherDesc.Text = GetWeatherDesc(code).ToUpper(); // Uppercase cho giống HTML
                TxtWeatherIcon.Text = GetWeatherIcon(code);
            }
        }
        catch {}
    }

    private string GetWeatherDesc(int code)
    {
        return code switch {
            0 => "Trời quang", 1 or 2 or 3 => "Có mây", 45 or 48 => "Sương mù",
            >= 51 and <= 67 => "Mưa", >= 95 => "Giông bão", _ => "Không rõ"
        };
    }

    private string GetWeatherIcon(int code)
    {
        if (code == 0) return "☀";
        if (code <= 3) return "☁";
        if (code >= 51) return "🌧";
        return "☁";
    }
}