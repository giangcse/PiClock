using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.Layout;
using System;
using System.Threading.Tasks;
using PiClock.Models;
using PiClock.Services;

namespace PiClock;

public partial class MainWindow : Window
{
    private readonly AppConfig _config;
    private readonly TelegramService _telegramService;
    private readonly WeatherService _weatherService;
    private readonly SlideshowService _slideshowService;

    private DispatcherTimer _clockTimer = null!;
    private DispatcherTimer _slideTimer = null!;
    private DispatcherTimer? _teleTimer;
    private DispatcherTimer _weatherTimer = null!;

    public MainWindow()
    {
        InitializeComponent();

        // Load config
        _config = AppConfig.Load();

        // Initialize services
        _telegramService = new TelegramService(_config.Telegram);
        _weatherService = new WeatherService(_config.Location);
        _slideshowService = new SlideshowService(_config.Slideshow);

        // Setup Telegram events
        _telegramService.OnMessageReceived += (message, sender) => ShowToast(message, sender);
        _telegramService.OnClearMessages += ClearAllMessages;

        InitializeTimers();
        StartApplication();
    }

    private void InitializeTimers()
    {
        // Clock timer
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (s, e) => UpdateTime();

        // Slideshow timer
        _slideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_config.Slideshow.IntervalSeconds)
        };
        _slideTimer.Tick += async (s, e) => await ChangeImageAsync();

        // Weather timer
        _weatherTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(_config.Weather.UpdateIntervalMinutes)
        };
        _weatherTimer.Tick += async (s, e) => await UpdateWeatherAsync();
    }

    private async void StartApplication()
    {
        // Start clock
        UpdateTime();
        _clockTimer.Start();

        // Load and start slideshow
        _slideshowService.LoadImages();
        if (_slideshowService.HasImages)
        {
            await ChangeImageAsync();
            _slideTimer.Start();
        }

        // Initialize Telegram
        _ = InitializeTelegramAsync();

        // Update weather
        await UpdateWeatherAsync();
        _weatherTimer.Start();
    }

    private async Task InitializeTelegramAsync()
    {
        await Task.Run(async () =>
        {
            bool success = await _telegramService.InitializeAsync();
            if (success)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _teleTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(_config.Telegram.CheckIntervalSeconds)
                    };
                    _teleTimer.Tick += async (s, e) => await _telegramService.CheckMessagesAsync();
                    _teleTimer.Start();
                });
            }
        });
    }

    private async Task ChangeImageAsync()
    {
        var bitmap = await _slideshowService.GetNextImageAsync();
        if (bitmap != null && BackgroundImage != null)
        {
            BackgroundImage.Source = bitmap;
        }
    }

    private void UpdateTime()
    {
        if (TxtTime == null) return;

        var now = DateTime.Now;
        TxtTime.Text = now.ToString("HH:mm");

        var culture = new System.Globalization.CultureInfo("vi-VN");
        if (TxtDayName != null) TxtDayName.Text = now.ToString("dddd", culture).ToUpper();
        if (TxtFullDate != null) TxtFullDate.Text = now.ToString("dd.MM.yyyy");

        // Update lunar date
        if (TxtLunarDate != null)
        {
            TxtLunarDate.Text = LunarCalendarService.GetLunarDateShort(now);
        }
    }

    private async Task UpdateWeatherAsync()
    {
        var weather = await _weatherService.GetCurrentWeatherAsync();
        if (weather != null)
        {
            if (TxtTemp != null)
                TxtTemp.Text = $"{Math.Round(weather.Temperature)}°";

            if (TxtWeatherDesc != null)
                TxtWeatherDesc.Text = WeatherService.GetWeatherDescription(weather.WeatherCode).ToUpper();

            if (TxtWeatherIcon != null)
                TxtWeatherIcon.Text = WeatherService.GetWeatherIcon(weather.WeatherCode);
        }
    }

    private void ShowToast(string message, string senderName)
    {
        Console.WriteLine($">>> SHOW TOAST: {senderName}: {message}");
        Dispatcher.UIThread.InvokeAsync(() => AddMessageToStack(message, senderName));
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
        Console.WriteLine($">>> ADD MESSAGE TO STACK: {message}");
        if (MessageStack == null)
        {
            Console.WriteLine(">>> MessageStack is NULL!");
            return;
        }

        var newBubble = CreateMessageControl(message, senderName);
        MessageStack.Children.Add(newBubble);
        Console.WriteLine($">>> MessageStack now has {MessageStack.Children.Count} children");

        if (MessageStack.Children.Count > _config.Telegram.MaxVisibleMessages)
        {
            MessageStack.Children.RemoveAt(0);
        }
    }

    private Control CreateMessageControl(string message, string? senderName)
    {
        // Load Inter font
        var interFont = new FontFamily("avares://PiClock/Assets/Fonts#Inter");

        var border = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#CC000000")),
            CornerRadius = new CornerRadius(16),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(20, 16),
            Margin = new Thickness(0, 0, 0, 0),
            Opacity = 0
        };

        // Add drop shadow effect like other elements
        border.Effect = new Avalonia.Media.DropShadowEffect
        {
            Color = Colors.Black,
            BlurRadius = 15,
            Opacity = 0.6,
            OffsetX = 0,
            OffsetY = 4
        };

        var transition = new Avalonia.Animation.Transitions();
        transition.Add(new Avalonia.Animation.DoubleTransition
        {
            Property = Visual.OpacityProperty,
            Duration = TimeSpan.FromSeconds(0.4),
            Easing = new Avalonia.Animation.Easings.CubicEaseOut()
        });
        transition.Add(new Avalonia.Animation.TransformOperationsTransition
        {
            Property = Border.RenderTransformProperty,
            Duration = TimeSpan.FromSeconds(0.4),
            Easing = new Avalonia.Animation.Easings.CubicEaseOut()
        });
        border.Transitions = transition;
        border.RenderTransform = TransformOperations.Parse("translateX(30px)");

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Top };

        // Sender name with orange color like day name
        var nameBlock = new TextBlock
        {
            Text = (senderName ?? "Telegram").ToUpper(),
            FontFamily = interFont,
            Foreground = new SolidColorBrush(Color.Parse("#F97316")), // Orange like TxtDayName
            FontSize = 20,
            FontWeight = FontWeight.ExtraBold,
            LetterSpacing = 1,
            Margin = new Thickness(0, 0, 0, 8)
        };

        // Message with white color
        var msgBlock = new TextBlock
        {
            Text = message,
            FontFamily = interFont,
            Foreground = new SolidColorBrush(Color.Parse("#FFFFFF")),
            FontSize = 24,
            FontWeight = FontWeight.Regular,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 6,
            TextTrimming = TextTrimming.CharacterEllipsis,
            LineHeight = 34
        };

        textStack.Children.Add(nameBlock);
        textStack.Children.Add(msgBlock);

        border.Child = textStack;

        // Delay the animation slightly to ensure the element is rendered first
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Delay(50); // Small delay to ensure layout is complete
            border.Opacity = 1;
            border.RenderTransform = TransformOperations.Parse("translateX(0px)");
        }, DispatcherPriority.Background);

        return border;
    }
}
