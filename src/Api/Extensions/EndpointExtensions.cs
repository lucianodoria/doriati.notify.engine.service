using Doriati.Notify.Engine.Api.Endpoints.TelegramNotifications;

namespace Doriati.Notify.Engine.Api.Extensions;

public static class EndpointExtensions
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapTelegramNotificationEndpoints();
        return app;
    }
}
