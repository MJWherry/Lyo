using Lyo.Authentication.AspNetCore.Endpoints;
using Lyo.Authentication.OpenIdConnect.Endpoints;
using Lyo.Common;
using Lyo.Config.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o => LyoJsonSerializerOptions.ApplyTo(o.SerializerOptions));
builder.Services.AddProblemDetails();
builder.Services.AddConfigApi(builder.Configuration);
var application = builder.Build();
// Give bodiless 4xx/5xx responses (e.g. bare NotFound) an RFC 7807 problem details body.
application.UseStatusCodePages();
if (application.Environment.IsDevelopment()) {
    application.MapOpenApi();
    application.MapScalarApiReference();
}

application.UseAuthentication();
application.UseAuthorization();
application.MapLyoJwks();
application.MapLyoAuthEndpoints();
application.MapLyoTokenManagementEndpoints();
application.MapConfigApiEndpoints();
application.Run();