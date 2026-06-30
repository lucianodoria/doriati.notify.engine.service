using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

namespace Doriati.Notify.Engine.Api.Extensions;

public sealed class AllowedClientsEndpointFilter : IEndpointFilter
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AllowedClientsEndpointFilter> _logger;

    public AllowedClientsEndpointFilter(
        IConfiguration configuration,
        ILogger<AllowedClientsEndpointFilter> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;

        var clientId = http.Request.Headers["X-Client-Id"].ToString();
        var clientSecret = http.Request.Headers["X-Client-Secret"].ToString();

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            _logger.LogWarning("Missing client credentials headers.");
            return ValueTask.FromResult<object?>(Results.Unauthorized());
        }

        var allowedClients = _configuration.GetSection("AllowedClients").Get<Dictionary<string, string>>();
        if (allowedClients is null || !allowedClients.TryGetValue(clientId, out var expectedSecret))
        {
            _logger.LogWarning("Invalid client id: {ClientId}", clientId);
            return ValueTask.FromResult<object?>(Results.Unauthorized());
        }

        if (!CryptographicOperations.FixedTimeEquals(Hash(clientSecret), Hash(expectedSecret)))
        {
            _logger.LogWarning("Invalid client secret for client id: {ClientId}", clientId);
            return ValueTask.FromResult<object?>(Results.Unauthorized());
        }

        return next(context);
    }

    private static byte[] Hash(string value) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
