using CentroP.Api.Common.Behaviors;
using CentroP.Api.Common.Exceptions;
using CentroP.Api.Common.Interfaces;
using CentroP.Api.Features.Clientes;
using CentroP.Api.Features.Laboratorios;
using CentroP.Api.Features.Provincias;
using CentroP.Api.Features.Scanner;
using CentroP.Api.Features.Stock;
using CentroP.Api.Features.Sucursales;
using FluentValidation;
using CentroP.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json;
using System.Threading.RateLimiting;

// ── Bootstrap logger ──────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "CentroP")
        .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/centrorp-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}"));

    // ── EF Core ───────────────────────────────────────────────────────────────
    builder.Services.AddDbContext<CentroPDbContext>(opts =>
        opts.UseSqlServer(builder.Configuration.GetConnectionString("CentroP"))
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

    // ── MediatR + ValidationBehavior ─────────────────────────────────────────
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    });

    // ── Validadores FluentValidation ─────────────────────────────────────────
    builder.Services.AddScoped<IValidator<GetMovimientosStockQuery>, GetMovimientosStockValidator>();
    builder.Services.AddScoped<IValidator<EscanearProductoQuery>, EscanearProductoValidator>();

    // ── IDbConnectionFactory ─────────────────────────────────────────────────
    builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

    // ── HybridCache ───────────────────────────────────────────────────────────
    builder.Services.AddHybridCache();

    // ── Rate Limiting ─────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("fixed", limiter =>
        {
            limiter.PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100);
            limiter.Window = TimeSpan.FromSeconds(
                builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60));
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = builder.Configuration.GetValue("RateLimiting:QueueLimit", 10);
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // ── OpenAPI ───────────────────────────────────────────────────────────────
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((doc, _, _) =>
        {
            doc.Info.Title = "CentroP API";
            doc.Info.Version = "v1";
            doc.Info.Description = "API core del sistema de farmacias CentroP — Fase 1 (Solo Lectura)";
            return Task.CompletedTask;
        });
    });

    // ── ProblemDetails + ExceptionHandlers ────────────────────────────────────
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // ── Health Checks ─────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            connectionString: builder.Configuration.GetConnectionString("CentroP")!,
            healthQuery: "SELECT 1",
            name: "centrorp-sql",
            tags: ["db", "sql", "sqlserver"]);

    // ─────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Middleware pipeline (orden importa) ───────────────────────────────────
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} → {StatusCode} en {Elapsed:0.0} ms";
    });
    app.UseRateLimiter();

    // ── OpenAPI + Scalar UI ───────────────────────────────────────────────────
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
        options
            .WithTitle("CentroP API")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));

    // ── Health Checks ─────────────────────────────────────────────────────────
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                entries = report.Entries.ToDictionary(
                    e => e.Key,
                    e => new { status = e.Value.Status.ToString(), description = e.Value.Description })
            });
            await context.Response.WriteAsync(result);
        }
    });
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    // ── Feature Endpoints ─────────────────────────────────────────────────────
    app.MapSucursalesEndpoints();
    app.MapProvinciasEndpoints();
    app.MapLaboratoriosEndpoints();
    app.MapClientesEndpoints();
    app.MapStockEndpoints();
    app.MapScannerEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al iniciar.");
}
finally
{
    Log.CloseAndFlush();
}
