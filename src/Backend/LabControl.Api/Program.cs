using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using LabControl.Api.Hubs;
using LabControl.Api.Middlewares;
using LabControl.Application;
using LabControl.Infrastructure;
using LabControl.Infrastructure.Identity;
using LabControl.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/labcontrol-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Registrar Servicios de Capas (Application e Infrastructure)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 3. Controllers & SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. Rate Limiting (Anti-DDoS)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("FixedPolicy", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
});

// 5. CORS para Intranet LAN (Soporte completo para SignalR Cross-Origin)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowIntranetLAN", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 6. HealthChecks integrados
builder.Services.AddHealthChecks();

var app = builder.Build();

// 7. Inicialización y Sembrado de Datos Automático (DbInitializer)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        await DbInitializer.SeedDatabaseAsync(dbContext, userManager, roleManager);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Ocurrió un error al sembrar los datos iniciales en la base de datos.");
    }
}

// Pipeline de Middleware
app.UseGlobalExceptionHandling();
app.UseSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCors("AllowIntranetLAN");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LaboratorioHub>("/hubs/laboratorio");
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
