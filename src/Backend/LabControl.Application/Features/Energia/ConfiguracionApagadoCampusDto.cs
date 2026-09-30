namespace LabControl.Application.Features.Energia;

public class ConfiguracionApagadoCampusDto
{
    public bool Habilitado { get; set; } = true;
    public string HoraApagado { get; set; } = "21:30";
    public int MinutosAvisoPrevio { get; set; } = 5;
    public bool SoloLunesAViernes { get; set; } = true;
    public string? UltimaEjecucionFecha { get; set; }
}
