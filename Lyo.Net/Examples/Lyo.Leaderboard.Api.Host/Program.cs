using Lyo.Api;
using Lyo.Api.Middleware;
using Lyo.Authentication.AspNetCore.Endpoints;
using Lyo.Authentication.Sso.Endpoints;
using Lyo.Json;
using Lyo.Leaderboard.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddLyoApiCompression();
builder.Services.ConfigureHttpJsonOptions(o => LyoJsonSerializerOptions.ApplyTo(o.SerializerOptions));
builder.Services.AddLeaderboardApi(builder.Configuration);
var application = builder.Build();
application.UseMiddleware<LoggingMiddleware>();
application.UseLyoApiCompression();
if (application.Environment.IsDevelopment()) {
    application.MapOpenApi();
    application.MapScalarApiReference();
}

application.UseAuthentication();
application.UseAuthorization();
application.MapLyoJwks();
application.MapLyoAuthEndpoints();
application.MapLyoTokenManagementEndpoints();
application.MapLeaderboardApiEndpoints();
application.Run();
