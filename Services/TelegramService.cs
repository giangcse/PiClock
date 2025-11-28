using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using PiClock.Models;

namespace PiClock.Services;

public class TelegramService
{
    private TelegramBotClient? _botClient;
    private int _lastUpdateId = 0;
    private readonly TelegramConfig _config;

    public event Action<string, string>? OnMessageReceived;
    public event Action? OnClearMessages;
    public bool IsConnected => _botClient != null;

    public TelegramService(TelegramConfig config)
    {
        _config = config;
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_config.BotToken) || _config.BotToken == "BOT_TOKEN_HERE")
            {
                Console.WriteLine("⚠️ Telegram Bot Token chưa được cấu hình");
                return false;
            }

            _botClient = new TelegramBotClient(_config.BotToken);
            var me = await _botClient.GetMeAsync();
            await _botClient.DeleteWebhookAsync();
            
            Console.WriteLine($"✅ TELEGRAM KẾT NỐI THÀNH CÔNG: @{me.Username}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ LỖI TELEGRAM: {ex.Message}");
            _botClient = null;
            return false;
        }
    }

    public async Task CheckMessagesAsync()
    {
        if (_botClient == null) return;

        try
        {
            var updates = await _botClient.GetUpdatesAsync(offset: _lastUpdateId + 1, limit: 10);

            foreach (var update in updates)
            {
                _lastUpdateId = update.Id;
                var msg = update.Message ?? update.ChannelPost;

                if (msg?.Text != null)
                {
                    string text = msg.Text.Trim();
                    string sender = msg.Chat.Title ?? msg.Chat.FirstName ?? "Telegram";

                    if (text.Equals("/clear", StringComparison.OrdinalIgnoreCase))
                    {
                        OnClearMessages?.Invoke();
                    }
                    else
                    {
                        OnMessageReceived?.Invoke(text, sender);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi check message: {ex.Message}");
        }
    }
}
