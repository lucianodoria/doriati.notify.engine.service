using Doriati.Notify.Engine.Api.Extensions;
using Doriati.Notify.Engine.Application.TelegramNotifications.Dtos;

namespace Doriati.Notify.Engine.Api.Endpoints.TelegramNotifications;

public static class TelegramNotificationEndpoints
{
    public static IEndpointRouteBuilder MapTelegramNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/notifications")
            .WithTags("Notifications");

        group
            .MapPost("/telegram", CreateTelegramNotificationHandler.HandleAsync)
            .AddEndpointFilter<AllowedClientsEndpointFilter>()
            .Accepts<TelegramNotificationRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Enviar notificação Telegram")
            .WithDescription(
                "Envia uma mensagem para o Telegram usando o BOT_TOKEN e CHAT_ID fornecidos pela aplicação de origem.\n\n" +
                "Autenticação M2M (headers obrigatórios):\n" +
                "- X-Client-Id\n" +
                "- X-Client-Secret\n\n" +
                "Payload:\n" +
                "- BotToken: token do bot\n" +
                "- ChatId: destino\n" +
                "- Message: mensagem\n" +
                "- ParseMode: opcional (padrão MarkdownV2)");

        return endpoints;
    }
}
