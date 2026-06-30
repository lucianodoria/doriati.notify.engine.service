using Doriati.Notify.Engine.Application.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Doriati.Notify.Engine.Infrastructure.Telegram;

public sealed class TelegramSender : ITelegramSender
{
    public async Task SendAsync(
        string botToken,
        string chatId,
        string message,
        string? parseMode,
        CancellationToken cancellationToken)
    {
        var client = new TelegramBotClient(botToken);

        var selectedParseMode =
            Enum.TryParse<ParseMode>(parseMode, ignoreCase: true, out var pm)
                ? pm
                : ParseMode.MarkdownV2;

        await client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: selectedParseMode,
            cancellationToken: cancellationToken);
    }
}
