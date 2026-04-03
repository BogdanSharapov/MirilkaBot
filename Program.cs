using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class Program
{
    private static ITelegramBotClient _botClient;
    private static readonly Random _random = new Random();
    private static readonly HashSet<long> _users = new HashSet<long>() { 536999309, 552682543 };

    static async Task Main(string[] args)
    {
        // Запуск веб-сервера для Health Check в фоновом потоке
        _ = Task.Run(StartWebServer);

        // --- Инициализация и запуск Telegram бота (ваш существующий код) ---
        string token = "8664165820:AAFKbiWlI2h6tmPpff5G_HAuCjgbEmooQKc";
        if (string.IsNullOrEmpty(token)) {
            Console.WriteLine("❌ Токен не найден. Установите переменную окружения TELEGRAM_BOT_TOKEN.");
            return;
        }
        _botClient = new TelegramBotClient(token);
        using CancellationTokenSource cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) => { e.Cancel = true; cts.Cancel(); };
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => { cts.Cancel(); };

        ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = new[] { UpdateType.Message } };
        _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cts.Token);

        Console.WriteLine("✅ Бот и Health Check сервер запущены.");
        try { await Task.Delay(-1, cts.Token); }
        catch (TaskCanceledException) { Console.WriteLine("Бот остановлен."); }
    }

    // Минимальный веб-сервер для Health Check
    static async Task StartWebServer()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        
        // Эндпоинт для проверки работоспособности Render
        app.MapGet("/", () => "Bot is running");
        app.MapGet("/health", () => "OK");

        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        await app.RunAsync($"http://0.0.0.0:{port}");
    }

    // --- Обработчики команд бота (ваш существующий код) ---
    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message) return;
        var message = update.Message;
        if (message?.Text == null) return;

        var chatId = message.Chat.Id;
        var userName = message.From.FirstName;

        if (message.Text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            string welcome = "Привет! Я бот-мирилка 🤝\nКогда поссоритесь, отправь команду /conflict, и я решу, кто должен написать первым.";
            await botClient.SendMessage(chatId, welcome, cancellationToken: cancellationToken);
            return;
        }

        if (message.Text.Equals("/conflict", StringComparison.OrdinalIgnoreCase) ||
            message.Text.Equals("/конфликт", StringComparison.OrdinalIgnoreCase))
        {
            bool isYou = _random.Next(2) == 0;
            string result = isYou ? "🎲 Жребий брошен! Первым пишет Богдан." : "🎲 Жребий брошен! Первой пишет Раилина ❤️";
            foreach (var userId in _users)
            {
                try { await botClient.SendMessage(userId, result, cancellationToken: cancellationToken); }
                catch (Exception ex) { Console.WriteLine($"Не удалось отправить сообщение {userId}: {ex.Message}"); }
            }
            return;
        }

        if (message.Text.Equals("/id", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendMessage(chatId, $"Ваш ID: {chatId}", cancellationToken: cancellationToken);
            return;
        }
    }

    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiEx => $"Ошибка Telegram API: {apiEx.ErrorCode} - {apiEx.Message}",
            _ => exception.ToString()
        };
        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}