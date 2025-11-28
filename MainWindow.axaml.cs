using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
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
        Dispatcher.UIThread.Post(() => AddMessageToStack(message, senderName));
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

        if (MessageStack.Children.Count > _config.Telegram.MaxVisibleMessages)
        {
            MessageStack.Children.RemoveAt(0);
        }
    }

    private Control CreateMessageControl(string message, string? senderName)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#CC0f172a")),
            CornerRadius = new CornerRadius(16),
            BorderThickness = new Thickness(1.5),
            Padding = new Thickness(16, 12),
            Margin = new Thickness(0, 0, 0, 12),
            Opacity = 0
        };

        var borderGradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Color.Parse("#50FFFFFF"), 0.0),
                new GradientStop(Color.Parse("#00FFFFFF"), 0.5),
                new GradientStop(Color.Parse("#10FFFFFF"), 1.0)
            }
        };
        border.BorderBrush = borderGradient;

        var transition = new Avalonia.Animation.Transitions();
        transition.Add(new Avalonia.Animation.DoubleTransition
        {
            Property = Visual.OpacityProperty,
            Duration = TimeSpan.FromSeconds(0.4),
            Easing = new Avalonia.Animation.Easings.CubicEaseOut()
        });
        border.Transitions = transition;

        var transformGroup = new TransformGroup();
        var translate = new TranslateTransform(20, 0);
        transformGroup.Children.Add(translate);
        border.RenderTransform = transformGroup;

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto, *")
        };

        var iconBorder = new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(18),
            Background = new SolidColorBrush(Color.Parse("#2038bdf8")),
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Top
        };

        var icon = new PathIcon
        {
            Data = Geometry.Parse("M11.944 0A12 12 0 0 0 0 12a12 12 0 0 0 12 12 12 12 0 0 0 12-12A12 12 0 0 0 12 0a12 12 0 0 0-.056 0zm4.962 7.224c.1-.002.321.023.465.14a.506.506 0 0 1 .171.325c.016.093.036.306.02.472-.18 1.898-.962 6.502-1.36 8.627-.168.9-.499 1.201-.82 1.23-.696.065-1.225-.46-1.9-.902-1.056-.693-1.653-1.124-2.678-1.8-.135-.461.088-.865.253-1.057l.128-.135c.038-.033.262-.27.525-.53l.366-.363c1.55-1.55 1.488-1.503 1.246-1.566-.242-.063-.64.128-2.636 1.475-.363.246-.922.56-1.07.653-.984.618-2.074.622-2.735.416-.661-.206-1.397-.442-1.397-.442s-.496-.285.344-.613c3.963-1.558 7.21-2.793 9.743-3.705 2.533-.912 3.033-.966 3.32-.966z"),
            Foreground = new SolidColorBrush(Color.Parse("#38bdf8")),
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        iconBorder.Child = icon;
        Grid.SetColumn(iconBorder, 0);

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(textStack, 1);

        var nameBlock = new TextBlock
        {
            Text = senderName ?? "Telegram",
            Foreground = new SolidColorBrush(Color.Parse("#94a3b8")),
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 2, 0, 4)
        };

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

        Dispatcher.UIThread.Post(() =>
        {
            border.Opacity = 1;
            translate.X = 0;
        }, DispatcherPriority.Background);

        return border;
    }
}
