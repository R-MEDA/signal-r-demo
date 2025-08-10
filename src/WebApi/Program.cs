using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebApi;
using WebApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add SignalR to the service
builder.Services.AddSignalR().AddAzureSignalR("<azure-siganlr-connection-string>");

builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/weather-reading", async (
    [FromBody] WeatherData data,
    IHubContext<WeatherHub> hubContext) =>
{
    await hubContext.Clients.All.SendAsync("WeatherDataProcessed", data);
    Console.WriteLine($"Data Received {data}");
    return Results.StatusCode(StatusCodes.Status201Created);
})
.WithName("PostWeratherReading");

app.MapHub<WeatherHub>("/weather-hub");

app.UseCors(p => p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials());

app.Run();