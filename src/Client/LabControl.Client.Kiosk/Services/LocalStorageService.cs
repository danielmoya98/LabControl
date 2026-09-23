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
    private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kiosk-config.json");

    public static bool HasConfiguration()
    {
        return File.Exists(ConfigFilePath);
    }

    public static ConfigModel? LoadConfig()
    {
        if (!File.Exists(ConfigFilePath)) return null;
        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            return JsonSerializer.Deserialize<ConfigModel>(json);
        }
        catch
        {
            return null;
        }
    }

    public static void SaveConfig(ConfigModel config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFilePath, json);
    }
}
