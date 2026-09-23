using System.IO;
using System.Text.Json;

namespace LabControl.Client.Kiosk.Services;

public class ConfigModel
{
    public string ApiBaseUrl { get; set; } = "http://192.168.50.132:5256/";
    public int AulaId { get; set; } = 1;
    public string AulaNombre { get; set; } = "Laboratorio";
    public int ComputadoraId { get; set; }
    public string Hostname { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string ClaveTecnico { get; set; } = "AdminLab@2026";
    public int MinutosInactividadMaximo { get; set; } = 15;
    public int AccionInactividad { get; set; } = 0; // 0 = Apagar, 1 = CerrarSesion
    public DateTime? UltimoApagadoReportadoUtc { get; set; }
    public string? UltimoEstudianteSesion { get; set; }
}

public static class LocalStorageService
{
    private static string AppConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kiosk-config.json");
    private static string DataConfigPath => Path.Combine(GetDataDirectory(), "kiosk-config.json");

    public static string GetDataDirectory()
    {
        try
        {
            // Detección de partición descongelada (Thawed Partition / ThawSpace) para Deep Freeze
            // Si el laboratorio cuenta con una unidad secundaria persistente como D:\ o T:\
            string[] thawCandidates = [ @"D:\LabControlData", @"T:\LabControlData" ];
            foreach (var candidate in thawCandidates)
            {
                try
                {
                    var root = Path.GetPathRoot(candidate);
                    if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
                    {
                        if (!Directory.Exists(candidate))
                        {
                            Directory.CreateDirectory(candidate);
                        }
                        return candidate;
                    }
                }
                catch { }
            }

            var common = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LabControl");
            if (!Directory.Exists(common))
            {
                Directory.CreateDirectory(common);
            }
            return common;
        }
        catch
        {
            try
            {
                var local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LabControl");
                if (!Directory.Exists(local))
                {
                    Directory.CreateDirectory(local);
                }
                return local;
            }
            catch
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }
    }

    public static bool HasConfiguration()
    {
        var config = LoadConfig();
        return config != null && config.AulaId > 0 && !string.IsNullOrWhiteSpace(config.ApiBaseUrl);
    }

    public static ConfigModel? LoadConfig()
    {
        string? path = null;
        if (File.Exists(DataConfigPath)) path = DataConfigPath;
        else if (File.Exists(AppConfigPath)) path = AppConfigPath;

        if (path == null) return null;

        try
        {
            var json = File.ReadAllText(path);
            var model = JsonSerializer.Deserialize<ConfigModel>(json);
            if (model != null)
            {
                model.ApiBaseUrl = KioskApiService.NormalizeApiUrl(model.ApiBaseUrl);
            }
            return model;
        }
        catch
        {
            return null;
        }
    }

    public static void SaveConfig(ConfigModel config)
    {
        try
        {
            config.ApiBaseUrl = KioskApiService.NormalizeApiUrl(config.ApiBaseUrl);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

            // 1. Guardar en DataConfigPath (C:\ProgramData\LabControl - accesible con permisos de usuario estándar)
            try
            {
                var dataDir = GetDataDirectory();
                if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
                File.WriteAllText(DataConfigPath, json);
            }
            catch
            {
                // Silencioso
            }

            // 2. Intentar guardar también en la carpeta de la aplicación si hay permisos suficientes
            try
            {
                File.WriteAllText(AppConfigPath, json);
            }
            catch
            {
                // Silencioso si Program Files es de solo lectura para el usuario actual
            }
        }
        catch
        {
            // Silencioso
        }
    }
}
