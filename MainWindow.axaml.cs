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
// ImageSharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using AvaBitmap = Avalonia.Media.Imaging.Bitmap;
// Telegram
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

    // Config vị trí (Vĩnh Long)
    private const double LAT = 10.0668;
    private const double LON = 105.9088;

    // Config Telegram
    private TelegramBotClient? _botClient;
    private int _lastUpdateId = 0;

    // =================================================================
    // 👇👇👇 HÃY DÁN TOKEN CỦA BẠN VÀO GIỮA 2 DẤU NGOẶC KÉP DƯỚI ĐÂY 👇👇👇
    private const string BOT_TOKEN = "BOT_TOKEN_HERE";
    // =================================================================

    public MainWindow()
    {
        InitializeComponent();

        // 1. Setup Đồng hồ
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (s, e) => UpdateTime();
        _clockTimer.Start();

        // 2. Setup Slideshow
        _slideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _slideTimer.Tick += (s, e) => ChangeImage();

        // 3. Setup Telegram (Đã sửa lỗi Unreachable code)
        InitTelegram();

        // 4. Load ảnh và khởi chạy
        LoadImagesFromAutoFolder();
        UpdateTime();
        _ = UpdateWeatherAsync();

        var weatherTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        weatherTimer.Tick += async (s, e) => await UpdateWeatherAsync();
        weatherTimer.Start();
    }

    private void InitTelegram()
    {
        // Chạy Async để không block UI lúc khởi động
        _ = Task.Run(async () =>
        {
            try
            {
                // Khởi tạo Bot
                var bot = new TelegramBotClient(BOT_TOKEN);

                // Test kết nối thử
                var me = await bot.GetMeAsync();
                Console.WriteLine($"✅ TELEGRAM KẾT NỐI THÀNH CÔNG: {me.Username}");

                // Xóa Webhook cũ để đảm bảo nhận tin nhắn
                await bot.DeleteWebhookAsync();

                // Gán vào biến toàn cục để dùng sau
                _botClient = bot;

                // Quay về luồng chính để bật Timer
                Dispatcher.UIThread.Post(() =>
                {
                    _teleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                    _teleTimer.Tick += async (s, e) => await CheckTelegramMessages();
                    _teleTimer.Start();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ LỖI TELEGRAM: {ex.Message}");
                Console.WriteLine("👉 Kiểm tra lại TOKEN. Nếu token đúng, hãy check mạng internet.");
            }
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

    // --- TELEGRAM LOGIC ---
    private async Task CheckTelegramMessages()
    {
        if (_botClient == null) return;
        try
        {
            // Lấy tin nhắn
            var updates = await _botClient.GetUpdatesAsync(offset: _lastUpdateId + 1, limit: 10);

            foreach (var update in updates)
            {
                _lastUpdateId = update.Id;

                var msg = update.Message ?? update.ChannelPost; // Hỗ trợ cả Group và Channel

                if (msg != null && !string.IsNullOrEmpty(msg.Text))
                {
                    string text = msg.Text.Trim();
                    string sender = msg.Chat.Title ?? msg.Chat.FirstName ?? "Telegram";

                    Console.WriteLine($">>> NHẬN TIN: {text}");

                    // Lệnh xóa
                    if (text.Equals("/clear", StringComparison.OrdinalIgnoreCase))
                    {
                        ClearAllMessages();
                    }
                    else
                    {
                        ShowToast(text, sender);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(">>> LỖI CHECK MESSAGE: " + ex.Message);
        }
    }

    private void ShowToast(string message, string senderName)
    {
        Dispatcher.UIThread.Post(() =>
        {
            AddMessageToStack(message, senderName);
        });
    }

    private void ClearAllMessages()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (MessageStack != null) MessageStack.Children.Clear();
            Console.WriteLine(">>> ĐÃ XÓA SẠCH TIN NHẮN");
        });
    }

    private void AddMessageToStack(string message, string senderName)
    {
        if (MessageStack == null) return;

        var newBubble = CreateMessageControl(message, senderName);
        MessageStack.Children.Add(newBubble);

        if (MessageStack.Children.Count > 3)
        {
            MessageStack.Children.RemoveAt(0);
        }
    }

    // Giao diện Quote (Trích dẫn)
    private Control CreateMessageControl(string message, string? senderName)
    {
        // 1. Border bao ngoài (Style: Simulated Glass)
        var border = new Border
        {
            // Nền đen bán trong suốt (Không dùng Blur để nhẹ máy)
            Background = new SolidColorBrush(Avalonia.Media.Color.Parse("#CC0f172a")), // Màu xanh đen đậm (Slate-900), 80% opacity

            // Bo góc tròn trịa hiện đại (Apple style)
            CornerRadius = new CornerRadius(16),

            // Viền mỏng
            BorderThickness = new Thickness(1.5),

            Padding = new Thickness(16, 12),
            Margin = new Thickness(0, 0, 0, 12),
            Opacity = 0 // Bắt đầu ẩn
        };

        // --- KỸ THUẬT GIẢ KÍNH: Tạo viền Gradient phát sáng ---
        // Viền trên sáng, viền dưới tối -> Tạo cảm giác ánh sáng chiếu vào cạnh kính
        var borderGradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Avalonia.Media.Color.Parse("#50FFFFFF"), 0.0), // Trắng mờ góc trên trái
                new GradientStop(Avalonia.Media.Color.Parse("#00FFFFFF"), 0.5), // Trong suốt ở giữa
                new GradientStop(Avalonia.Media.Color.Parse("#10FFFFFF"), 1.0)  // Hơi sáng nhẹ góc dưới phải
            }
        };
        border.BorderBrush = borderGradient;

        // Animation hiện dần
        var transition = new Transitions();
        transition.Add(new DoubleTransition { Property = Visual.OpacityProperty, Duration = TimeSpan.FromSeconds(0.4), Easing = new Avalonia.Animation.Easings.CubicEaseOut() });
        border.Transitions = transition;

        // Animation trượt nhẹ từ phải sang (Transform)
        var transformGroup = new TransformGroup();
        var translate = new TranslateTransform(20, 0); // Dịch sang phải 20px
        transformGroup.Children.Add(translate);
        border.RenderTransform = transformGroup;


        // 2. Cấu trúc nội dung (Grid)
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto, *")
        };

        // Cột 0: Icon Telegram (Trong vòng tròn mờ)
        var iconBorder = new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(18), // Hình tròn
            Background = new SolidColorBrush(Avalonia.Media.Color.Parse("#2038bdf8")), // Xanh dương nhạt nền
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Top
        };

        var icon = new Avalonia.Controls.PathIcon
        {
            Data = Geometry.Parse("M11.944 0A12 12 0 0 0 0 12a12 12 0 0 0 12 12 12 12 0 0 0 12-12A12 12 0 0 0 12 0a12 12 0 0 0-.056 0zm4.962 7.224c.1-.002.321.023.465.14a.506.506 0 0 1 .171.325c.016.093.036.306.02.472-.18 1.898-.962 6.502-1.36 8.627-.168.9-.499 1.201-.82 1.23-.696.065-1.225-.46-1.9-.902-1.056-.693-1.653-1.124-2.678-1.8-.135-.461.088-.865.253-1.057l.128-.135c.038-.033.262-.27.525-.53l.366-.363c1.55-1.55 1.488-1.503 1.246-1.566-.242-.063-.64.128-2.636 1.475-.363.246-.922.56-1.07.653-.984.618-2.074.622-2.735.416-.661-.206-1.397-.442-1.397-.442s-.496-.285.344-.613c3.963-1.558 7.21-2.793 9.743-3.705 2.533-.912 3.033-.966 3.32-.966z"),
            Foreground = new SolidColorBrush(Avalonia.Media.Color.Parse("#38bdf8")), // Màu xanh Sky Blue
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        iconBorder.Child = icon;
        Grid.SetColumn(iconBorder, 0);

        // Cột 1: Nội dung
        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(textStack, 1);

        // Tên người gửi
        var nameBlock = new TextBlock
        {
            Text = senderName ?? "Telegram",
            Foreground = new SolidColorBrush(Avalonia.Media.Color.Parse("#94a3b8")), // Màu xám xanh (Slate-400)
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 2, 0, 4)
        };

        // Nội dung tin nhắn
        var msgBlock = new TextBlock
        {
            Text = message,
            Foreground = Brushes.White,
            FontSize = 15,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 5,
            TextTrimming = TextTrimming.CharacterEllipsis,
            LineHeight = 22
        };

        textStack.Children.Add(nameBlock);
        textStack.Children.Add(msgBlock);

        grid.Children.Add(iconBorder);
        grid.Children.Add(textStack);
        border.Child = grid;

        // Kích hoạt animation (Hiện + Trượt sang trái về vị trí gốc)
        Dispatcher.UIThread.Post(() =>
        {
            border.Opacity = 1;
            translate.X = 0; // Trượt về 0
        }, DispatcherPriority.Background);

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