using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Application.Features.Energia;
using LabControl.Domain.Enums;

namespace LabControl.Api.Services;

public class CampusShutdownManager
{
    private readonly string _configFilePath;
    private readonly object _lock = new();
    private readonly ILogger<CampusShutdownManager> _logger;
    private ConfiguracionApagadoCampusDto _config;

    public CampusShutdownManager(IWebHostEnvironment env, ILogger<CampusShutdownManager> logger)
    {
        _logger = logger;
        _configFilePath = Path.Combine(env.ContentRootPath, "config-apagado-campus.json");
        _config = LoadConfig();
    }

    public ConfiguracionApagadoCampusDto GetConfiguracion()
    {
        lock (_lock)
        {
            return new ConfiguracionApagadoCampusDto
            {
                Habilitado = _config.Habilitado,
                HoraApagado = _config.HoraApagado,
                MinutosAvisoPrevio = _config.MinutosAvisoPrevio,
                SoloLunesAViernes = _config.SoloLunesAViernes,
                UltimaEjecucionFecha = _config.UltimaEjecucionFecha
            };
        }
    }

    public void SaveConfiguracion(ConfiguracionApagadoCampusDto nueva)
    {
        lock (_lock)
        {
            _config.Habilitado = nueva.Habilitado;
            _config.HoraApagado = string.IsNullOrWhiteSpace(nueva.HoraApagado) ? "21:30" : nueva.HoraApagado.Trim();
            _config.MinutosAvisoPrevio = Math.Clamp(nueva.MinutosAvisoPrevio, 1, 30);
            _config.SoloLunesAViernes = nueva.SoloLunesAViernes;
            _config.UltimaEjecucionFecha = nueva.UltimaEjecucionFecha;

            try
            {
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al persistir la configuración de apagado masivo del campus.");
            }
        }
    }

    public async Task EnviarAlertaPreventivaCampusAsync(IServiceScopeFactory scopeFactory, string mensaje, CancellationToken ct = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<ISignalRNotificationService>();
            await notificationService.SendAlertaGlobalAsync(mensaje, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar alerta preventiva de apagado al campus.");
        }
    }

    public async Task<int> EjecutarApagadoCampusAsync(IServiceScopeFactory scopeFactory, string motivo, CancellationToken ct = default)
    {
        _logger.LogWarning("Iniciando orden de APAGADO MASIVO EN TODO EL CAMPUS. Motivo: {Motivo}", motivo);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<ISignalRNotificationService>();

            // 1. Enviar orden global de apagado vía SignalR a todas las terminales conectadas
            await notificationService.SendComandoEnergiaGlobalAsync("SHUTDOWN", motivo, ct);

            // 2. Redundancia aula por aula y terminal por terminal registrada
            var aulas = await context.Aulas.Include(a => a.Computadoras).ToListAsync(ct);
            int totalEquipos = 0;

            foreach (var aula in aulas)
            {
                await notificationService.SendComandoEnergiaAulaAsync(aula.Id, "SHUTDOWN", motivo, ct);
                foreach (var pc in aula.Computadoras)
                {
                    totalEquipos++;
                    await notificationService.SendComandoEnergiaTerminalAsync(pc.Hostname, "SHUTDOWN", motivo, ct);
                }
            }

            // 3. Cerrar formalmente sesiones activas abiertas
            var sesionesActivas = await context.SesionesUso
                .Where(s => s.FechaHoraFin == null)
                .ToListAsync(ct);

            foreach (var sesion in sesionesActivas)
            {
                sesion.Finalizar(TipoCierreSesion.AdminRemoto, DateTime.UtcNow);
            }

            // 4. Actualizar estado de las computadoras que estaban en uso a Disponible
            var computadorasEnUso = await context.Computadoras
                .Where(c => c.EstadoActual == EstadoComputadora.EnUso)
                .ToListAsync(ct);

            foreach (var pc in computadorasEnUso)
            {
                pc.CambiarEstado(EstadoComputadora.Disponible);
            }

            await context.SaveChangesAsync(ct);

            // Actualizar fecha de última ejecución
            lock (_lock)
            {
                _config.UltimaEjecucionFecha = DateTime.UtcNow.ToString("yyyy-MM-dd");
                SaveConfiguracion(_config);
            }

            _logger.LogInformation("Apagado masivo del campus completado. Se enviaron órdenes a {Total} equipos.", totalEquipos);
            return totalEquipos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al ejecutar apagado masivo del campus.");
            throw;
        }
    }

    private ConfiguracionApagadoCampusDto LoadConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<ConfiguracionApagadoCampusDto>(json);
                if (config != null) return config;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo leer archivo de configuración de apagado del campus. Creando valores por defecto.");
        }

        var defaultConfig = new ConfiguracionApagadoCampusDto
        {
            Habilitado = true,
            HoraApagado = "21:30",
            MinutosAvisoPrevio = 5,
            SoloLunesAViernes = true
        };

        try
        {
            var json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFilePath, json);
        }
        catch { }

        return defaultConfig;
    }
}
