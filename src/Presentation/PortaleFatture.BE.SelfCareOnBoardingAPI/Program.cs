using System.Text.Json;
using PortaleFatture.BE.SelfCareOnBoardingAPI.Dto;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
    app.UseHttpsRedirection();


app.MapGet("/v2/institutions/onboarding/recipientCode/verification", async (string originId, string recipientCode, HttpContext context) =>
{
    var codiceIPA = context.Request.Query["originId"];
    var codiceSDI = context.Request.Query["recipientCode"];

    var bearerToken = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");

    Console.WriteLine($"originId: {codiceIPA}");
    Console.WriteLine($"recipientCode: {codiceSDI}");
    Console.WriteLine($"authtoken: {bearerToken}");

    if (string.IsNullOrEmpty(bearerToken))
    {
        var errorResponse = new
        {
            detail = "Authorization token is missing or invalid.",
            instance = "/v2/institutions/onboarding/" + recipientCode + "/verification",
            invalidParams = new[]
            {
                new
                {
                    name = "Authorization",
                    reason = "Bearer token is required but not provided."
                }
            },
            status = 401,
            title = "Unauthorized",
            type = "https://httpstatuses.com/401"
        };

        Console.WriteLine("ERROR 401: Unauthorized");
        Console.WriteLine($"Instance: {errorResponse.instance}");
        Console.WriteLine($"Detail: {errorResponse.detail}");
        Console.WriteLine($"Invalid Param: {errorResponse.invalidParams[0].name} - {errorResponse.invalidParams[0].reason}");
        Console.WriteLine($"Status: {errorResponse.status}");
        Console.WriteLine($"Title: {errorResponse.title}");

        return Results.Json(errorResponse, statusCode: 401);
    }

    if (codiceSDI == "DENIED_NO_BILLING")
        return Results.Ok("DENIED_NO_BILLING");

    if (codiceSDI == "DENIED_NO_ASSOCIATION")
        return Results.Ok("DENIED_NO_ASSOCIATION");

    return Results.Ok("ACCEPTED");
});


app.MapPut("/onboarding/{onboardingId}/update", async (string onboardingId, string status, OnboardingUpdateRequest request, HttpContext context) =>
{
    var bearerToken = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");

    Console.WriteLine($"onboardingId: {onboardingId}");
    Console.WriteLine($"status: {status}");
    Console.WriteLine($"authtoken: {bearerToken}");

    if (string.IsNullOrEmpty(bearerToken))
        return Results.Unauthorized();

    Console.WriteLine($"body request: {JsonSerializer.Serialize(request)}"); 
    return Results.Ok();
})
.WithName("UpdateOnboarding");


app.Run();