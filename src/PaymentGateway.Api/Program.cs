using System.Text;

using FluentValidation;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using PaymentGateway.Api.HttpClients;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Metrics;
using PaymentGateway.Api.Middleware;
using PaymentGateway.Api.Models.Merchant;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validation.Merchant;

using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;


// https://medium.com/@mahmednisar/logging-like-a-pro-serilog-opentelemetry-in-net-3c9f219b9296
// Configure Serilog as it provides structured logging in json format these provide better searchability
// and integration with log management systems, and also has built in support for OpenTelemetry which
// allows us to correlate logs with traces and metrics for better observability.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", Environment.GetEnvironmentVariable("ServiceName") ?? "PaymentGateway")
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .WriteTo.Console(new JsonFormatter())
    // In a real application, you would configure the OpenTelemetry sink to send logs
    //.WriteTo.OpenTelemetry(options =>
    //{
    //    options.Endpoint = "http://localhost:4317"; // OTLP endpoint
    //    options.Protocol = OtlpProtocol.Grpc;
    //    options.ResourceAttributes = new Dictionary<string, object>
    //    {
    //        ["service.name"] = "your-service-name",
    //        ["service.version"] = "1.0.0"
    //    };
    //})
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<PaymentsRepository>();
builder.Services.AddSingleton<IISOCurrencyCodes, ISOCurrencyCodes>();
builder.Services.AddScoped<IValidator<PaymentRequest>, PaymentRequestValidator>();
builder.Services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddSingleton<IIdempotancyRepository, IdempotancyRepository>();
builder.Services.AddSingleton<IPaymentMetrics, PaymentMetrics>();
builder.Services.AddScoped<IAcquiringBank, AcquiringBank>();

builder.Services.AddHttpContextAccessor();
// Register the AcquiringBankClient with an HttpClient and add a resilience handler for retries and circuit breaking
builder.Services
    .AddHttpClient<IAcquiringBankClient, AcquiringBankClient>()
    .AddStandardResilienceHandler();



// setup authenication 
var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MerchantOnly", policy => policy.RequireRole("Merchant"));
});



/**
 * Adds tracing and metrics collection to the application using OpenTelemetry. 
 * They only log to the console here but in a real application you would configure exporters to 
 * send this data to a monitoring system such as Jaeger, prometheus or Application Insights.
 */
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: Environment.GetEnvironmentVariable("ServiceName") ?? "PaymentGateway",
            serviceVersion: Environment.GetEnvironmentVariable("ServiceVersion") ?? "1.0.0",
            serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("PaymentGateway.Api.Metrics")
        .AddConsoleExporter());

//https://oneuptime.com/blog/post/2026-02-06-opentelemetry-metrics-aspnet-core-web-apis/view

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseMiddleware<MetricsMiddleware>();

app.MapControllers();

app.Run();
