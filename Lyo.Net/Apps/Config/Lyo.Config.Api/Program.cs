using Lyo.Config.Api;
using Lyo.Config.Api.Security;
using Lyo.Common;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(o => LyoJsonSerializerOptions.ApplyTo(o.SerializerOptions));

builder.Services.AddConfigApi(builder.Configuration);

var application = builder.Build();

if (application.Environment.IsDevelopment()) {
    application.MapOpenApi();
    application.MapScalarApiReference();
}

application.UseMiddleware<RequireConfigApiKeyMiddleware>();
application.MapConfigApiEndpoints();
application.Run();
