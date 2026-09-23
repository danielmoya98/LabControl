using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LabControl.Application.Common.Interfaces;

namespace LabControl.Infrastructure.Services;

public class ReportePdfService : IReportePdfService
{
    private static readonly string ColorGuindo = "#9E0044";
    private static readonly string ColorGrisOscuro = "#1E293B";
    private static readonly string ColorBorde = "#E2E8F0";
    private static readonly string ColorFondoPar = "#F8FAFC";

    static ReportePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    private static byte[]? ObtenerLogoBytes()
    {
        string[] rutasPosibles =
        [
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "images", "logo_univalle.png"),
            Path.Combine(AppContext.BaseDirectory, "images", "logo_univalle.png"),
            @"D:\PROYECTOS\LAB-CONTROL\LabControl\src\WebAdmin\LabControl.WebAdmin\wwwroot\images\logo_univalle.png",
            @"D:\PROYECTOS\LAB-CONTROL\LabControl\src\Client\LabControl.Client.Kiosk\Assets\logo_univalle.png"
        ];

        foreach (var ruta in rutasPosibles)
        {
            if (File.Exists(ruta))
            {
                try { return File.ReadAllBytes(ruta); } catch { }
            }
        }
        return null;
    }

    public byte[] GenerarReporteAsistenciaClase(ReporteAsistenciaClaseDto datos)
    {
        var logo = ObtenerLogoBytes();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(ColorGrisOscuro));

                // 1. ENCABEZADO
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (logo != null)
                        {
                            row.ConstantItem(45).Image(logo);
                            row.ConstantItem(12);
                        }

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("UNIVERSIDAD DEL VALLE").FontSize(12).Bold().FontColor(ColorGuindo);
                            c.Item().Text(datos.SedeNombre.ToUpperInvariant()).FontSize(8).Bold().FontColor("#64748B");
                            c.Item().Text("DIRECCIÓN DE TECNOLOGÍAS DE INFORMACIÓN — CONTROL DE LABORATORIOS").FontSize(7.5f).FontColor("#64748B");
                        });

                        row.ConstantItem(160).AlignRight().Column(c =>
                        {
                            c.Item().Text("PLANILLA OFICIAL DE ASISTENCIA").FontSize(8.5f).Bold().FontColor(ColorGuindo);
                            c.Item().Text($"Fecha: {datos.Fecha:dd/MM/yyyy}").FontSize(8);
                            c.Item().Text($"Horario: {datos.HoraInicioProgramada:hh\\:mm} - {datos.HoraFinProgramada:hh\\:mm}").FontSize(8);
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(ColorGuindo);

                    // Tarjeta de Metadatos de la Clase
                    col.Item().PaddingTop(6).PaddingBottom(6).Background("#F1F5F9").Padding(8).Row(row =>
                    {
                        row.RelativeItem(3).Column(c =>
                        {
                            c.Item().Text(t =>
                            {
                                t.Span("Materia: ").Bold();
                                t.Span(!string.IsNullOrWhiteSpace(datos.MateriaSigla) ? $"[{datos.MateriaSigla}] " : "");
                                t.Span(datos.MateriaNombre ?? "Práctica de Cómputo");
                            });
                            c.Item().Text(t =>
                            {
                                t.Span("Docente a Cargo: ").Bold();
                                t.Span(datos.DocenteNombre ?? "No Asignado");
                                if (!string.IsNullOrWhiteSpace(datos.DocenteEmail))
                                    t.Span($" ({datos.DocenteEmail})").FontSize(7.5f);
                            });
                        });

                        row.RelativeItem(2).Column(c =>
                        {
                            c.Item().Text(t => { t.Span("Laboratorio: ").Bold(); t.Span(datos.AulaNombre); });
                            c.Item().Text(t => { t.Span("Grupo / Paralelo: ").Bold(); t.Span(datos.GrupoParalelo ?? "Único"); });
                        });

                        row.RelativeItem(2).AlignRight().Column(c =>
                        {
                            c.Item().Text(t => { t.Span("Capacidad Aula: ").Bold(); t.Span($"{datos.CapacidadAula} PCs"); });
                            c.Item().Text(t => { t.Span("Asistentes en Terminales: ").Bold(); t.Span($"{datos.Estudiantes.Count} alumnos"); });
                        });
                    });

                    col.Item().PaddingBottom(4);
                });

                // 2. TABLA DE ESTUDIANTES Y TERMINALES
                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30); // N°
                            columns.ConstantColumn(40); // Puesto
                            columns.ConstantColumn(75); // Hostname
                            columns.RelativeColumn(3);  // Estudiante
                            columns.ConstantColumn(50); // Inicio
                            columns.ConstantColumn(50); // Fin
                            columns.ConstantColumn(45); // Minutos
                            columns.RelativeColumn(2);  // Firma
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(ColorGuindo).Padding(4).AlignCenter().Text("N°").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).AlignCenter().Text("PUESTO").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).Text("TERMINAL").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).Text("CORREO INSTITUCIONAL").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).AlignCenter().Text("INGRESO").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).AlignCenter().Text("SALIDA").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).AlignCenter().Text("DURAC.").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).AlignCenter().Text("FIRMA CONFORMIDAD").FontColor("#FFFFFF").Bold().FontSize(8);
                        });

                        if (datos.Estudiantes.Count == 0)
                        {
                            table.Cell().ColumnSpan(8).Padding(20).AlignCenter()
                                .Text("No se registraron sesiones de uso durante este bloque horario en el laboratorio.")
                                .Italic().FontColor("#64748B");
                        }
                        else
                        {
                            int index = 1;
                            foreach (var est in datos.Estudiantes)
                            {
                                var bg = index % 2 == 0 ? ColorFondoPar : "#FFFFFF";

                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text(index.ToString());
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text(est.NumeroPuesto > 0 ? $"P-{est.NumeroPuesto:D2}" : "-").Bold();
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).Text(est.Hostname);
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).Text(est.EmailEstudiante).FontSize(8);
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text(est.FechaHoraInicio.ToString("HH:mm:ss"));
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text(est.FechaHoraFin.HasValue ? est.FechaHoraFin.Value.ToString("HH:mm:ss") : "En curso");
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text(est.DuracionMinutos.HasValue ? $"{est.DuracionMinutos} min" : "-");
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text(""); // Espacio para firma física

                                index++;
                            }
                        }
                    });
                });

                // 3. PIE DE PÁGINA CON FIRMAS
                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(25).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignCenter().LineHorizontal(0.75f).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).AlignCenter().Text("Docente Titular / Responsable").Bold().FontSize(8);
                            c.Item().AlignCenter().Text(datos.DocenteNombre ?? "Firma del Docente").FontSize(7.5f).FontColor("#64748B");
                        });

                        row.ConstantItem(40);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignCenter().LineHorizontal(0.75f).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).AlignCenter().Text("Encargado de Laboratorio").Bold().FontSize(8);
                            c.Item().AlignCenter().Text("Revisión Técnica y Conformidad").FontSize(7.5f).FontColor("#64748B");
                        });

                        row.ConstantItem(40);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignCenter().LineHorizontal(0.75f).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).AlignCenter().Text("Jefatura de Carrera / DTI").Bold().FontSize(8);
                            c.Item().AlignCenter().Text("Sello de Recepción").FontSize(7.5f).FontColor("#64748B");
                        });
                    });

                    col.Item().PaddingTop(12).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("LabControl v2026.9 · Sistema de Control y Monitoreo de Laboratorios · Generado el ").FontSize(7).FontColor("#94A3B8");
                            t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(7).FontColor("#94A3B8");
                        });
                        row.ConstantItem(80).AlignRight().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(7).FontColor("#94A3B8"));
                            t.Span("Pág. ");
                            t.CurrentPageNumber();
                            t.Span(" de ");
                            t.TotalPages();
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerarReporteAuditoria(ReporteAuditoriaPdfDto datos)
    {
        var logo = ObtenerLogoBytes();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(ColorGrisOscuro));

                // ENCABEZADO
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (logo != null)
                        {
                            row.ConstantItem(45).Image(logo);
                            row.ConstantItem(12);
                        }

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("UNIVERSIDAD DEL VALLE").FontSize(12).Bold().FontColor(ColorGuindo);
                            c.Item().Text(datos.SedeNombre.ToUpperInvariant()).FontSize(8).Bold().FontColor("#64748B");
                            c.Item().Text("DIRECCIÓN DE TECNOLOGÍAS DE INFORMACIÓN — CONTROL DE LABORATORIOS").FontSize(7.5f).FontColor("#64748B");
                        });

                        row.ConstantItem(220).AlignRight().Column(c =>
                        {
                            c.Item().Text("INFORME OFICIAL DE AUDITORÍA DE SESIONES").FontSize(8.5f).Bold().FontColor(ColorGuindo);
                            c.Item().Text($"Período: {(datos.FechaInicio.HasValue ? datos.FechaInicio.Value.ToString("dd/MM/yyyy") : "Inicio")} al {(datos.FechaFin.HasValue ? datos.FechaFin.Value.ToString("dd/MM/yyyy") : "Presente")}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(datos.FiltrosTexto))
                                c.Item().Text(datos.FiltrosTexto).FontSize(7.5f).FontColor("#64748B");
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(ColorGuindo);

                    // Tarjetas KPI de Resumen
                    col.Item().PaddingTop(6).PaddingBottom(6).Row(row =>
                    {
                        row.RelativeItem().Background("#F1F5F9").Padding(6).Column(c =>
                        {
                            c.Item().Text("TOTAL SESIONES").FontSize(7).Bold().FontColor("#64748B");
                            c.Item().Text(datos.TotalRegistros.ToString("N0")).FontSize(12).Bold().FontColor(ColorGuindo);
                        });
                        row.ConstantItem(8);
                        row.RelativeItem().Background("#F1F5F9").Padding(6).Column(c =>
                        {
                            c.Item().Text("HORAS-MÁQUINA").FontSize(7).Bold().FontColor("#64748B");
                            c.Item().Text($"{datos.TotalHorasUso:N1} hrs").FontSize(12).Bold().FontColor("#16A34A");
                        });
                        row.ConstantItem(8);
                        row.RelativeItem().Background("#F1F5F9").Padding(6).Column(c =>
                        {
                            c.Item().Text("ESTUDIANTES ÚNICOS").FontSize(7).Bold().FontColor("#64748B");
                            c.Item().Text(datos.TotalEstudiantesUnicos.ToString("N0")).FontSize(12).Bold().FontColor("#7C3AED");
                        });
                        row.ConstantItem(8);
                        row.RelativeItem().Background("#F1F5F9").Padding(6).Column(c =>
                        {
                            c.Item().Text("DURACIÓN PROMEDIO").FontSize(7).Bold().FontColor("#64748B");
                            c.Item().Text($"{datos.PromedioMinutosPorSesion:F1} min").FontSize(12).Bold().FontColor("#D97706");
                        });
                    });

                    col.Item().PaddingBottom(4);
                });

                // TABLA DE SESIONES
                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40); // ID
                            columns.ConstantColumn(80); // Terminal
                            columns.RelativeColumn(2);  // Aula
                            columns.RelativeColumn(3);  // Estudiante
                            columns.RelativeColumn(2);  // Inicio
                            columns.RelativeColumn(2);  // Fin
                            columns.ConstantColumn(45); // Durac.
                            columns.RelativeColumn(2);  // Cierre
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(ColorGuindo).Padding(4).AlignCenter().Text("ID").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).Text("TERMINAL").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).Text("AULA").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).Text("ESTUDIANTE").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).AlignCenter().Text("INICIO").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).AlignCenter().Text("FIN").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).AlignCenter().Text("DURACIÓN").FontColor("#FFFFFF").Bold().FontSize(8);
                            header.Cell().Background(ColorGuindo).Padding(4).AlignCenter().Text("MOTIVO CIERRE").FontColor("#FFFFFF").Bold().FontSize(8);
                        });

                        int index = 1;
                        foreach (var ses in datos.Sesiones)
                        {
                            var bg = index % 2 == 0 ? ColorFondoPar : "#FFFFFF";

                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text($"#{ses.SesionId}");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).Text(ses.Hostname).Bold();
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).Text(ses.NombreAula);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).Text(ses.EmailEstudiante);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text(ses.FechaHoraInicio.ToString("dd/MM/yyyy HH:mm"));
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text(ses.FechaHoraFin.HasValue ? ses.FechaHoraFin.Value.ToString("dd/MM/yyyy HH:mm") : "Activa");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text(ses.DuracionMinutos.HasValue ? $"{ses.DuracionMinutos} min" : "-");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text(ses.TipoCierre.ToString());

                            index++;
                        }
                    });
                });

                // PIE DE PÁGINA
                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignCenter().LineHorizontal(0.75f).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).AlignCenter().Text("Encargado de Laboratorio").Bold().FontSize(8);
                            c.Item().AlignCenter().Text("Emisión y Certificación de Uso").FontSize(7.5f).FontColor("#64748B");
                        });

                        row.ConstantItem(60);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignCenter().LineHorizontal(0.75f).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).AlignCenter().Text("Vicerrectorado Académico / Decanatura").Bold().FontSize(8);
                            c.Item().AlignCenter().Text("Recepción y Aprobación").FontSize(7.5f).FontColor("#64748B");
                        });
                    });

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("LabControl v2026.9 · Documento de Control y Auditoría Académica · ").FontSize(7).FontColor("#94A3B8");
                            t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(7).FontColor("#94A3B8");
                        });
                        row.ConstantItem(80).AlignRight().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(7).FontColor("#94A3B8"));
                            t.Span("Pág. ");
                            t.CurrentPageNumber();
                            t.Span(" de ");
                            t.TotalPages();
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerarReporteEnergia(ReporteEnergiaPdfDto datos)
    {
        var logo = ObtenerLogoBytes();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(ColorGrisOscuro));

                // ENCABEZADO
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (logo != null)
                        {
                            row.ConstantItem(45).Image(logo);
                            row.ConstantItem(12);
                        }

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("UNIVERSIDAD DEL VALLE").FontSize(12).Bold().FontColor(ColorGuindo);
                            c.Item().Text(datos.SedeNombre.ToUpperInvariant()).FontSize(8).Bold().FontColor("#64748B");
                            c.Item().Text("DIRECCIÓN ADMINISTRATIVA Y FINANCIERA — AUDITORÍA ENERGÉTICA").FontSize(7.5f).FontColor("#64748B");
                        });

                        row.ConstantItem(180).AlignRight().Column(c =>
                        {
                            c.Item().Text("INFORME DE RESPONSABILIDAD ENERGÉTICA").FontSize(8.5f).Bold().FontColor(ColorGuindo);
                            c.Item().Text($"Período: {(datos.FechaInicio.HasValue ? datos.FechaInicio.Value.ToString("dd/MM/yyyy") : "Histórico")} - {(datos.FechaFin.HasValue ? datos.FechaFin.Value.ToString("dd/MM/yyyy") : "Hoy")}").FontSize(8);
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(ColorGuindo);

                    // KPIs
                    col.Item().PaddingTop(6).PaddingBottom(6).Row(row =>
                    {
                        row.RelativeItem().Background("#F1F5F9").Padding(6).Column(c =>
                        {
                            c.Item().Text("HORAS DESPERDICIADAS").FontSize(7).Bold().FontColor("#64748B");
                            c.Item().Text($"{datos.TotalHorasDesperdiciadas:N1} hrs").FontSize(12).Bold().FontColor("#D97706");
                        });
                        row.ConstantItem(8);
                        row.RelativeItem().Background("#F1F5F9").Padding(6).Column(c =>
                        {
                            c.Item().Text("TOTAL INCIDENTES").FontSize(7).Bold().FontColor("#64748B");
                            c.Item().Text(datos.TotalIncidentes.ToString("N0")).FontSize(12).Bold().FontColor("#DC2626");
                        });
                        row.ConstantItem(8);
                        row.RelativeItem().Background("#F1F5F9").Padding(6).Column(c =>
                        {
                            c.Item().Text("TERMINALES AFECTADAS").FontSize(7).Bold().FontColor("#64748B");
                            c.Item().Text(datos.TotalEquiposAfectados.ToString("N0")).FontSize(12).Bold().FontColor("#2563EB");
                        });
                    });

                    col.Item().PaddingBottom(4);
                });

                // CONTENIDO: RANKING + INCIDENTES
                page.Content().Column(col =>
                {
                    if (datos.TopInfractores.Count > 0)
                    {
                        col.Item().PaddingTop(4).Text("RANKING DE ESTUDIANTES CON EQUIPOS DESATENDIDOS / NO APAGADOS").Bold().FontSize(9).FontColor(ColorGuindo);
                        col.Item().PaddingTop(2).Table(t =>
                        {
                            t.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(25);
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(3);
                                cols.ConstantColumn(60);
                                cols.ConstantColumn(70);
                            });

                            t.Header(h =>
                            {
                                h.Cell().Background("#334155").Padding(3).AlignCenter().Text("#").FontColor("#FFFFFF").Bold().FontSize(7.5f);
                                h.Cell().Background("#334155").Padding(3).Text("ESTUDIANTE").FontColor("#FFFFFF").Bold().FontSize(7.5f);
                                h.Cell().Background("#334155").Padding(3).Text("CORREO").FontColor("#FFFFFF").Bold().FontSize(7.5f);
                                h.Cell().Background("#334155").Padding(3).AlignCenter().Text("FALTAS").FontColor("#FFFFFF").Bold().FontSize(7.5f);
                                h.Cell().Background("#334155").Padding(3).AlignCenter().Text("DESPERDICIO").FontColor("#FFFFFF").Bold().FontSize(7.5f);
                            });

                            int rank = 1;
                            foreach (var inf in datos.TopInfractores.Take(5))
                            {
                                var bg = rank % 2 == 0 ? ColorFondoPar : "#FFFFFF";
                                t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text(rank.ToString());
                                t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).Text(inf.NombreEstudiante ?? "Estudiante");
                                t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).Text(inf.EmailEstudiante);
                                t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text($"{inf.IncidentesCount}").Bold().FontColor("#DC2626");
                                t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text($"{inf.TotalHorasDesperdiciadas:N1} hrs");
                                rank++;
                            }
                        });
                        col.Item().PaddingBottom(10);
                    }

                    col.Item().PaddingTop(6).Text("DETALLE DE INCIDENTES CERTIFICADOS EN TERMINALES").Bold().FontSize(9).FontColor(ColorGuindo);
                    col.Item().PaddingTop(2).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(65);
                            cols.ConstantColumn(85);
                            cols.RelativeColumn(2);
                            cols.ConstantColumn(45);
                            cols.RelativeColumn(3);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(ColorGuindo).Padding(4).Text("TERMINAL").FontColor("#FFFFFF").Bold().FontSize(8);
                            h.Cell().Background(ColorGuindo).Padding(4).Text("AULA").FontColor("#FFFFFF").Bold().FontSize(8);
                            h.Cell().Background(ColorGuindo).Padding(4).Text("ÚLTIMO USUARIO").FontColor("#FFFFFF").Bold().FontSize(8);
                            h.Cell().Background(ColorGuindo).Padding(4).AlignCenter().Text("HORAS").FontColor("#FFFFFF").Bold().FontSize(8);
                            h.Cell().Background(ColorGuindo).Padding(4).Text("MOTIVO DETECCIÓN").FontColor("#FFFFFF").Bold().FontSize(8);
                        });

                        int idx = 1;
                        foreach (var inc in datos.Incidentes)
                        {
                            var bg = idx % 2 == 0 ? ColorFondoPar : "#FFFFFF";
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).Text(inc.Hostname).Bold();
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).Text(inc.AulaNombre);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).Text(inc.UltimoEstudianteEmail);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).AlignCenter().Text($"{inc.HorasInactivaEncendida:N1} h");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(ColorBorde).Padding(3).Text(inc.MotivoInfraccion).FontSize(7.5f);
                            idx++;
                        }
                    });
                });

                // PIE DE PÁGINA
                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignCenter().LineHorizontal(0.75f).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).AlignCenter().Text("Encargado de Laboratorios").Bold().FontSize(8);
                            c.Item().AlignCenter().Text("Responsable de Monitoreo").FontSize(7.5f).FontColor("#64748B");
                        });

                        row.ConstantItem(60);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignCenter().LineHorizontal(0.75f).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).AlignCenter().Text("Dirección Administrativa y Financiera (DAF)").Bold().FontSize(8);
                            c.Item().AlignCenter().Text("Visto Bueno de Eficiencia").FontSize(7.5f).FontColor("#64748B");
                        });
                    });

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("LabControl v2026.9 · Informe de Eficiencia y Auditoría de Activos · ").FontSize(7).FontColor("#94A3B8");
                            t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(7).FontColor("#94A3B8");
                        });
                        row.ConstantItem(80).AlignRight().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(7).FontColor("#94A3B8"));
                            t.Span("Pág. ");
                            t.CurrentPageNumber();
                            t.Span(" de ");
                            t.TotalPages();
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
