namespace Doriati.Notify.Engine.Api.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseApiMiddlewares(this WebApplication app)
    {
        var swaggerEnabled = app.Configuration.GetValue("Swagger:Enabled", defaultValue: app.Environment.IsDevelopment());
        if (swaggerEnabled)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Doriati Notify Engine (Telegram) v1");
                options.DocumentTitle = "Doriati Notify Engine (Telegram) — OpenAPI";
            });
        }

        app.UseHttpsRedirection();
        return app;
    }
}
