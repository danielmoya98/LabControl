using System.Text;
using ClosedXML.Excel;
using LabControl.Application.Common.Interfaces;

namespace LabControl.Infrastructure.Services;

public class ReporteExcelService : IReporteExcelService
{
    public byte[] GenerarReporteAuditoriaExcel(List<SesionAuditoriaDto> sesiones, string? subtituloFiltros = null)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Auditoría de Sesiones");

        // 1. Encabezado Institucional
        worksheet.Cell(1, 1).Value = "UNIVERSIDAD DEL VALLE — AUDITORÍA DE LABORATORIOS DE CÓMPUTO";
        worksheet.Range(1, 1, 1, 10).Merge();
        worksheet.Row(1).Height = 32;
        var titleCell = worksheet.Cell(1, 1).Style;
        titleCell.Font.Bold = true;
        titleCell.Font.FontSize = 14;
        titleCell.Font.FontColor = XLColor.White;
        titleCell.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A"); // Navy Blue Univalle
        titleCell.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleCell.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        // Subtítulo
        string subtitulo = string.IsNullOrWhiteSpace(subtituloFiltros)
            ? $"Reporte generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Total registros: {sesiones.Count}"
            : $"Filtros: {subtituloFiltros} | Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Total: {sesiones.Count}";

        worksheet.Cell(2, 1).Value = subtitulo;
        worksheet.Range(2, 1, 2, 10).Merge();
        worksheet.Row(2).Height = 20;
        var subCell = worksheet.Cell(2, 1).Style;
        subCell.Font.Italic = true;
        subCell.Font.FontSize = 10;
        subCell.Font.FontColor = XLColor.FromHtml("#475569");
        subCell.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        subCell.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        // 2. Cabeceras de Columnas
        string[] headers = 
        {
            "Nº Sesión", "Aula", "Terminal", "Correo Estudiante", 
            "Fecha/Hora Inicio", "Fecha/Hora Fin", "Duración (min)", 
            "Tipo de Cierre", "Estado Sync", "Fecha Sync (UTC)"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(4, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#334155"); // Slate Gray
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#94A3B8");
        }
        worksheet.Row(4).Height = 26;

        // 3. Filas de Datos
        int filaActual = 5;
        foreach (var s in sesiones)
        {
            worksheet.Cell(filaActual, 1).Value = s.SesionId;
            worksheet.Cell(filaActual, 2).Value = s.NombreAula;
            worksheet.Cell(filaActual, 3).Value = s.Hostname;
            worksheet.Cell(filaActual, 4).Value = s.EmailEstudiante;
            worksheet.Cell(filaActual, 5).Value = s.FechaHoraInicio.ToString("dd/MM/yyyy HH:mm:ss");
            worksheet.Cell(filaActual, 6).Value = s.FechaHoraFin.HasValue ? s.FechaHoraFin.Value.ToString("dd/MM/yyyy HH:mm:ss") : "En curso...";
            worksheet.Cell(filaActual, 7).Value = s.DuracionMinutos.HasValue ? s.DuracionMinutos.Value : 0;
            worksheet.Cell(filaActual, 8).Value = s.TipoCierre.ToString();
            worksheet.Cell(filaActual, 9).Value = s.SyncStatus.ToString();
            worksheet.Cell(filaActual, 10).Value = s.FechaSincronizacion.ToString("dd/MM/yyyy HH:mm");

            // Alineaciones
            worksheet.Cell(filaActual, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(filaActual, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(filaActual, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(filaActual, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(filaActual, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(filaActual, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            worksheet.Cell(filaActual, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(filaActual, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(filaActual, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Cebra sutil
            if (filaActual % 2 == 0)
            {
                worksheet.Range(filaActual, 1, filaActual, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            }

            for (int col = 1; col <= 10; col++)
            {
                worksheet.Cell(filaActual, col).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                worksheet.Cell(filaActual, col).Style.Border.BottomBorderColor = XLColor.FromHtml("#CBD5E1");
            }

            filaActual++;
        }

        // 4. Fila de Totales
        var totalMinutos = sesiones.Sum(s => s.DuracionMinutos ?? 0);
        var totalHoras = Math.Round(totalMinutos / 60.0, 2);

        worksheet.Cell(filaActual, 1).Value = "RESUMEN:";
        worksheet.Cell(filaActual, 2).Value = $"{sesiones.Count} sesiones registradas";
        worksheet.Range(filaActual, 2, filaActual, 5).Merge();
        worksheet.Cell(filaActual, 6).Value = "TOTAL TIEMPO:";
        worksheet.Cell(filaActual, 7).Value = $"{totalHoras} hrs ({totalMinutos} min)";
        worksheet.Range(filaActual, 7, filaActual, 10).Merge();

        var totalRange = worksheet.Range(filaActual, 1, filaActual, 10);
        totalRange.Style.Font.Bold = true;
        totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        totalRange.Style.Border.TopBorder = XLBorderStyleValues.Medium;
        totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
        worksheet.Row(filaActual).Height = 24;

        // Auto-ajustar ancho de columnas
        worksheet.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    public byte[] GenerarReporteAuditoriaCsv(List<SesionAuditoriaDto> sesiones)
    {
        var sb = new StringBuilder();

        // UTF-8 BOM para compatibilidad directa con Excel en español
        sb.Append('\uFEFF');

        // Encabezados separados por punto y coma
        sb.AppendLine("ID_Sesion;Aula;Terminal;Correo_Estudiante;Fecha_Inicio;Fecha_Fin;Duracion_Minutos;Tipo_Cierre;Sync_Status;Fecha_Sincronizacion");

        foreach (var s in sesiones)
        {
            var inicio = s.FechaHoraInicio.ToString("yyyy-MM-dd HH:mm:ss");
            var fin = s.FechaHoraFin.HasValue ? s.FechaHoraFin.Value.ToString("yyyy-MM-dd HH:mm:ss") : "En curso";
            var duracion = s.DuracionMinutos ?? 0;
            var sync = s.FechaSincronizacion.ToString("yyyy-MM-dd HH:mm:ss");

            sb.AppendLine($"{s.SesionId};\"{s.NombreAula}\";\"{s.Hostname}\";\"{s.EmailEstudiante}\";{inicio};{fin};{duracion};\"{s.TipoCierre}\";\"{s.SyncStatus}\";{sync}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
