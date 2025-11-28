using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Layout;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using AvaBitmap = Avalonia.Media.Imaging.Bitmap;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;

namespace PiClock;

public partial class MainWindow : Window
{
    private DispatcherTimer _clockTimer;
    private DispatcherTimer _slideTimer;
    private DispatcherTimer? _teleTimer;

    private string[] _imageFiles = Array.Empty<string>();
    private int _currentImageIndex = 0;

    private const double LAT = 10.0668;
    private const double LON = 105.9088;

    private TelegramBotClient? _botClient;
    private int _lastUpdateId = 0;

    // <--- TOKEN CỦA BẠN --->
    private const string BOT_TOKEN = "BOT_TOKEN_HERE";

    public MainWindow()
    {
        InitializeComponent();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (s, e) => UpdateTime();
        _clockTimer.Start();

        _slideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _slideTimer.Tick += (s, e) => ChangeImage();

        InitTelegram();

        LoadImagesFromAutoFolder();
        UpdateTime();
        _ = UpdateWeatherAsync();

        var weatherTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        weatherTimer.Tick += async (s, e) => await UpdateWeatherAsync();
        weatherTimer.Start();
    }

    private void InitTelegram()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var bot = new TelegramBotClient(BOT_TOKEN);
                await bot.DeleteWebhookAsync(); // Fix lỗi webhook
                _botClient = bot;

                Dispatcher.UIThread.Post(() =>
                {
                    _teleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                    _teleTimer.Tick += async (s, e) => await CheckTelegramMessages();
                    _teleTimer.Start();
                });
            }
            catch { Console.WriteLine("Lỗi Telegram (Check Token/Mạng)"); }
        });
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
        catch { }
    }

    private async void ChangeImage()
    {
        if (_imageFiles.Length == 0) return;
        string nextFile = _imageFiles[_currentImageIndex];

        try
        {
            var newBitmap = await Task.Run(() =>
            {
                using (var image = SixLabors.ImageSharp.Image.Load(nextFile))
                {
                    image.Mutate(x => x.AutoOrient());
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new SixLabors.ImageSharp.Size(1920, 1080),
                        Mode = ResizeMode.Max
                    }));
                    var ms = new MemoryStream();
                    image.SaveAsBmp(ms);
                    ms.Position = 0;
                    return new AvaBitmap(ms);
                }
            });

            if (BackgroundImage != null) BackgroundImage.Source = newBitmap;
            _currentImageIndex = (_currentImageIndex + 1) % _imageFiles.Length;
        }
        catch { _currentImageIndex = (_currentImageIndex + 1) % _imageFiles.Length; }
    }

    private void UpdateTime()
    {
        if (TxtTime == null) return;
        try
        {
            var now = DateTime.Now;
            TxtTime.Text = now.ToString("HH:mm");

            var culture = new System.Globalization.CultureInfo("vi-VN");
            if (TxtDayName != null) TxtDayName.Text = now.ToString("dddd", culture).ToUpper();
            if (TxtFullDate != null) TxtFullDate.Text = now.ToString("dd.MM.yyyy");
        }
        catch { }
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
            if (current != null && TxtTemp != null)
            {
                double temp = current["temperature"]?.Value<double>() ?? 0;
                int code = current["weathercode"]?.Value<int>() ?? 0;

                TxtTemp.Text = $"{Math.Round(temp)}°";
                TxtWeatherDesc.Text = GetWeatherDesc(code).ToUpper();
                TxtWeatherIcon.Text = GetWeatherIcon(code);
            }
        }
        catch { }
    }

    private async Task CheckTelegramMessages()
    {
        if (_botClient == null) return;
        try
        {
            var updates = await _botClient.GetUpdatesAsync(offset: _lastUpdateId + 1, limit: 5);
            foreach (var update in updates)
            {
                _lastUpdateId = update.Id;
                var msg = update.Message ?? update.ChannelPost;
                if (msg != null && !string.IsNullOrEmpty(msg.Text))
                {
                    string text = msg.Text.Trim();
                    string sender = msg.Chat.Title ?? msg.Chat.FirstName ?? "Telegram";

                    if (text.Equals("/clear", StringComparison.OrdinalIgnoreCase))
                    {
                        ClearAllMessages();
                    }
                    else
                    {
                        ShowFeedItem(text, sender);
                    }
                }
            }
        }
        catch { }
    }

    private void ClearAllMessages()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (MessageStack != null) MessageStack.Children.Clear();
        });
    }

    private void ShowFeedItem(string message, string senderName)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (MessageStack == null) return;

            var newItem = CreateFeedItem(message, senderName);

            // Thêm vào đầu danh sách (Trên cùng)
            MessageStack.Children.Insert(0, newItem);

            // LOGIC MỚI: Chỉ giữ 2 tin mới nhất
            if (MessageStack.Children.Count > 2)
            {
                // Xóa tin dưới cùng (cũ nhất)
                MessageStack.Children.RemoveAt(MessageStack.Children.Count - 1);
            }
        });
    }

    private Control CreateFeedItem(string message, string senderName)
    {
        // Card Tin Nhắn Lớn
        var border = new Border
        {
            // Nền đen mờ 80% (không quá tối để đọc chữ dài)
            Background = new SolidColorBrush(Avalonia.Media.Color.Parse("#CC050505")),

            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.Parse("#F97316")), // Viền cam
            BorderThickness = new Thickness(4, 0, 0, 0), // Vạch cam bên trái

            Padding = new Thickness(24, 20), // Padding rộng
            Margin = new Thickness(0, 0, 0, 20), // Cách nhau xa
            Opacity = 0
        };

        var transition = new Transitions();
        transition.Add(new DoubleTransition { Property = Visual.OpacityProperty, Duration = TimeSpan.FromSeconds(0.5) });
        border.Transitions = transition;

        var stack = new StackPanel();

        // Header: Tên + Giờ
        var headerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 10) };

        var nameBlock = new TextBlock
        {
            Text = senderName.ToUpper(),
            Foreground = new SolidColorBrush(Avalonia.Media.Color.Parse("#F97316")),
            FontSize = 24, // Tên to
            FontWeight = FontWeight.Bold
        };

        var timeBlock = new TextBlock
        {
            Text = DateTime.Now.ToString("HH:mm"),
            Foreground = new SolidColorBrush(Avalonia.Media.Color.Parse("#60FFFFFF")),
            FontSize = 18,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        headerStack.Children.Add(nameBlock);
        headerStack.Children.Add(timeBlock);

        // Nội dung tin nhắn: CHO PHÉP DÀI
        var msgBlock = new TextBlock
        {
            Text = message,
            Foreground = Brushes.White,
            FontSize = 28, // Chữ rất to (nhìn rõ từ 5m)
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 12, // Cho phép tối đa 12 dòng (Rất dài)
            TextTrimming = TextTrimming.CharacterEllipsis,
            LineHeight = 36 // Dãn dòng thoáng
        };

        stack.Children.Add(headerStack);
        stack.Children.Add(msgBlock);
        border.Child = stack;

        Dispatcher.UIThread.Post(() => border.Opacity = 1, DispatcherPriority.Background);

        return border;
    }

    private string GetWeatherDesc(int code) => code switch
    {
        0 => "Trời quang",
        1 or 2 or 3 => "Có mây",
        45 or 48 => "Sương mù",
        >= 51 and <= 67 => "Mưa",
        >= 95 => "Giông bão",
        _ => "Không rõ"
    };

    private string GetWeatherIcon(int code)
    {
        if (code == 0) return "☀";
        if (code <= 3) return "☁";
        if (code >= 51) return "🌧";
        return "☁";
    }
}