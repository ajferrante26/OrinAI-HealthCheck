using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("orin", client =>
{
    client.BaseAddress = new Uri("http://192.168.1.59:8999/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("orin");

    try
    {
        var upstream = await client.GetFromJsonAsync<HealthResponse>("health");

        if (upstream is null)
        {
            return Results.Json(
                new { status = false, detail = "empty response from OrinAI" },
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Ok(new { status = upstream.Status });
    }
    catch (HttpRequestException ex)
    {
        return Results.Json(
            new { status = false, detail = ex.Message },
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (TaskCanceledException)
    {
        return Results.Json(
            new { status = false, detail = "timeout calling OrinAI" },
            statusCode: StatusCodes.Status504GatewayTimeout);
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();

record HealthResponse(bool Status);