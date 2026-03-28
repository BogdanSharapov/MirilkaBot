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
        string token = "8664165820:AAFKbiWlI2h6tmPpff5G_HAuCjgbEmooQKc";
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Укажите токен в переменной окружения TELEGRAM_BOT_TOKEN");
            return;
        }

        _botClient = new TelegramBotClient(token);

        using CancellationTokenSource cts = new CancellationTokenSource();

        // Настройки получения обновлений
        ReceiverOptions receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message } // получаем только сообщения
        };

        // Запускаем получение обновлений
        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cts.Token
        );

        Console.WriteLine("Бот запущен. Нажмите Enter для остановки...");
        Console.ReadLine();

        cts.Cancel(); // останавливаем бота
    }

    // Обработчик входящих сообщений
    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message)
            return;

        var message = update.Message;
        if (message.Text == null)
            return;

        var chatId = message.Chat.Id;
        var userName = message.From.FirstName;

        // Команда /start
        if (message.Text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            string welcome = "Привет! Я бот-мирилка 🤝\n" +
                             "Когда поссоритесь, отправь команду /conflict, и я решу, кто должен написать первым.";
            await botClient.SendMessage(chatId, welcome, cancellationToken: cancellationToken);
            return;
        }

        // Команда /mir (или /примирение)
        if (message.Text.Equals("/conflict", StringComparison.OrdinalIgnoreCase) ||
            message.Text.Equals("/конфликт", StringComparison.OrdinalIgnoreCase))
        {
            // Выбираем случайного участника
            bool isYou = _random.Next(2) == 0; // 0 — ты, 1 — девушка

            string result;
            if (isYou)
                result = $"🎲 Жребий брошен! Первым пишет Богдан.";
            else
                result = "🎲 Жребий брошен! Первой пишет Раилина ❤️";

            await botClient.SendMessage(chatId, result, cancellationToken: cancellationToken);
            return;
        }
    }

    // Обработчик ошибок
    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Ошибка Telegram API: {apiRequestException.ErrorCode} - {apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}