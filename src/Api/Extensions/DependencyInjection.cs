using Doriati.Notify.Engine.Api.Services;
using Doriati.Notify.Engine.Application.Abstractions;
using Doriati.Notify.Engine.Infrastructure.Messaging.RabbitMq;
using Doriati.Notify.Engine.Infrastructure.Telegram;
using Microsoft.OpenApi;

namespace Doriati.Notify.Engine.Api.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "Doriati Notify Engine (Telegram)",
                    Version = "v1",
                    Description =
                        "Microsserviço de notificações genérico para Telegram.\n\n" +
                        "Entradas suportadas:\n" +
                        "- HTTP (síncrono): POST /api/v1/notifications/telegram\n" +
                        "- RabbitMQ (assíncrono): consumo da fila configurada em RabbitMq:Queue\n\n" +
                        "Autenticação (M2M):\n" +
                        "- X-Client-Id\n" +
                        "- X-Client-Secret\n\n" +
                        "Observação:\n" +
                        "BotToken e ChatId são fornecidos pela aplicação origem no payload."
                });

            options.AddSecurityDefinition(
                "X-Client-Id",
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "X-Client-Id",
                    In = ParameterLocation.Header,
                    Description = "Client Id M2M (ex.: GCD_APP, BWEB_APP)."
                });

            options.AddSecurityDefinition(
                "X-Client-Secret",
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "X-Client-Secret",
                    In = ParameterLocation.Header,
                    Description = "Client Secret M2M associado ao X-Client-Id."
                });
        });

        services.AddSingleton<AllowedClientsEndpointFilter>();

        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<ITelegramSender, TelegramSender>();
        services.AddSingleton<RabbitMqTelegramConsumer>();
        services.AddHostedService<RabbitMqTelegramHostedService>();

        return services;
    }
}
