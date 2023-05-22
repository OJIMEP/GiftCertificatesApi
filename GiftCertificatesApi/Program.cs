using AuthLibrary.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Serilog.Formatting.Elasticsearch;
using Serilog;
using GiftCertificates.Api;
using GiftCertificates.Application;
using GiftCertificates.Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddCors();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplication(configuration);
builder.Services.AddApi(configuration);

builder.Services.AddHttpContextAccessor();

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Http(
        requestUri: configuration["ElasticConfiguration:Uri"], null, textFormatter: new ElasticsearchJsonFormatter(inlineFields: true)
        )
    .Enrich.WithProperty("Environment", configuration["Environment"])
    .Enrich.WithProperty("ServiceName", "GiftCertificates")
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

builder.Host.UseSerilog(Log.Logger);

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                           ForwardedHeaders.XForwardedProto
});

app.UseCors(corsBuilder => corsBuilder
    .AllowAnyHeader()
    .AllowAnyMethod()
    .SetIsOriginAllowedToAllowWildcardSubdomains()
    .WithOrigins(builder.Configuration.GetSection("CorsOrigins").Get<List<string>>().ToArray()
    )
);

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        //var db = services.GetRequiredService<DateTimeServiceContext>();
        //db.Database.Migrate();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
    }

    try
    {
        var userManager = services.GetRequiredService<UserManager<DateTimeServiceUser>>();
        var rolesManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await RoleInitializer.InitializeAsync(userManager, rolesManager, configuration);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }

    try
    {
        //var db = services.GetRequiredService<DateTimeServiceContext>();
        //await RoleInitializer.CleanTokensAsync(db);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while clearing the database.");
    }
}

app.Run();
