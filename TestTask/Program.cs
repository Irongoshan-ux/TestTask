using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.AddConsole();

var httpClientName = "houseClient";

builder.Services.AddHttpClient(httpClientName, client =>
{
    var uri = new Uri(builder.Configuration["GosUslugi:BaseUrl"]!);

    client.BaseAddress = uri;
    client.DefaultRequestHeaders.Host = uri.Host;
    client.DefaultRequestHeaders.Add("Session-GUID", Guid.NewGuid().ToString());
    client.DefaultRequestHeaders.Add("Request-GUID", Guid.NewGuid().ToString());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/getHouseByNumber", async ([AsParameters] HouseRequest request, IHttpClientFactory clientFactory,
                                        IConfiguration config, [FromServices] ILogger<HouseRequest> logger,
                                        CancellationToken cancellationToken) =>
{
    var httpClient = clientFactory.CreateClient(httpClientName);
    var url = config["GosUslugi:SearchByCadastreNumberUrl"];

    var httpResponse = await httpClient
        .PostAsync($"{url}?pageIndex=1&elementsPerPage={request.ElementsCount}", JsonContent.Create(request), cancellationToken);

    dynamic? parsedResult;
    try
    {
        httpResponse.EnsureSuccessStatusCode();
        parsedResult = await httpResponse.Content.ReadFromJsonAsync<dynamic>(cancellationToken);
    }
    catch(Exception e)
    {
        logger.LogError(e.Message);
        return Results.StatusCode(500);
    }

    return Results.Ok(parsedResult);
})
.WithOpenApi();

app.Run();

internal record HouseRequest(string? CadastreNumber, int ElementsCount = 100);