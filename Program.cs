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

    static async Task Main(string[] args)
    {
        string token = "8664165820:AAFKbiWlI2h6tmPpff5G_HAuCjgbEmooQKc"; // замените на переменную окружения!

        _botClient = new TelegramBotClient(token);
        using CancellationTokenSource cts = new CancellationTokenSource();

        // Обработка сигналов завершения (SIGTERM, Ctrl+C)
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("Получен сигнал завершения. Остановка...");
            e.Cancel = true;
            cts.Cancel();
        };

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            Console.WriteLine("Процесс завершается. Остановка бота...");
            cts.Cancel();
        };

        // Настройки получения обновлений
        ReceiverOptions receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        // Запускаем получение обновлений
        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cts.Token
        );

        Console.WriteLine("Бот запущен. Ожидание...");

        // Ждём, пока не придёт сигнал остановки
        try
        {
            await Task.Delay(-1, cts.Token);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Бот остановлен.");
        }
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message)
            return;

        var message = update.Message;
        if (message?.Text == null)
            return;

        var chatId = message.Chat.Id;
        var userName = message.From.FirstName;

        if (message.Text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            string welcome = "Привет! Я бот-мирилка 🤝\n" +
                             "Когда поссоритесь, отправь команду /conflict, и я решу, кто должен написать первым.";
            await botClient.SendMessage(chatId, welcome, cancellationToken: cancellationToken);
            return;
        }

        if (message.Text.Equals("/conflict", StringComparison.OrdinalIgnoreCase) ||
            message.Text.Equals("/конфликт", StringComparison.OrdinalIgnoreCase))
        {
            bool isYou = _random.Next(2) == 0;
            string result = isYou
                ? "🎲 Жребий брошен! Первым пишет Богдан."
                : "🎲 Жребий брошен! Первой пишет Раилина ❤️";

            await botClient.SendMessage(chatId, result, cancellationToken: cancellationToken);
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