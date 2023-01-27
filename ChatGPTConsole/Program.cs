using OpenAI_API;

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var botClient = new TelegramBotClient(Environment.GetEnvironmentVariable("TELEGRAM_CHATGPTBOT_ACCESS_TOKEN"));

using CancellationTokenSource cts = new();

ReceiverOptions receiverOptions = new()
{
	AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
};

botClient.StartReceiving(
	updateHandler: HandleUpdateAsync,
	pollingErrorHandler: HandlePollingErrorAsync,
	receiverOptions: receiverOptions,
	cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
	if (update.Message is not { } message)
		return;
	if (message.Text is not { } messageText)
		return;

	var chatId = message.Chat.Id;

	Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

	var text = string.Empty;

	if (messageText.Equals("/start"))
		text = "Привет:)";
	else
	{
		var api = new OpenAIAPI(APIAuthentication.LoadFromEnv());

		text = await api.Completions.CreateAndFormatCompletion(
			new CompletionRequest(
				messageText,
				Model.DavinciText,
				max_tokens: 150,
				temperature: 0.9,
				top_p: 1,
				presencePenalty: 0.6,
				frequencyPenalty: 0));
		var lastIndex = text.LastIndexOf("\n");

		text = text.Remove(0, lastIndex + 1);
	}

	Console.WriteLine($"Answer: '{text}'. message in chat {chatId}.");

	var sentMessage = await botClient.SendTextMessageAsync(
		chatId: chatId,
		text,
		replyToMessageId: update.Message.MessageId,
		cancellationToken: cancellationToken);
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
	var ErrorMessage = exception switch
	{
		ApiRequestException apiRequestException
			=> $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
		_ => exception.ToString()
	};

	Console.WriteLine(ErrorMessage);

	return Task.CompletedTask;
}