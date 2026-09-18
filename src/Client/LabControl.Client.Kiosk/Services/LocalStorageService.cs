using System.IO;
using System.Text.Json;

namespace LabControl.Client.Kiosk.Services;

public class ConfigModel
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5256/";
    public int AulaId { get; set; }
    public string AulaNombre { get; set; } = "";
    public int ComputadoraId { get; set; }
    public string Hostname { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string ClaveTecnico { get; set; } = "AdminLab@2026";
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
