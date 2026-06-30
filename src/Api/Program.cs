using Doriati.Notify.Engine.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApi(builder.Configuration);

var app = builder.Build();

app.UseApiMiddlewares();
app.MapApiEndpoints();

app.Run();
