using System.Globalization;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Blazor.Helpers;

public static class PeriodDisplay
{
    /// <summary>
    /// Etiqueta de un periodo para listas y combos.
    ///
    /// Cuando el periodo se crea desde la importacion o dejando el nombre vacio, el nombre
    /// almacenado YA es "Mes Año" ("Agosto 2026"), asi que volver a concatenar el mes y el
    /// año daria un lio como "Agosto 2026 — 2026 agosto". Se devuelve el nombre tal cual.
    ///
    /// Pero el nombre es renombrable ("Sprint de lanzamiento"), y entonces el mes y el año
    /// derivados si aportan: se muestran entre parentesis para que el combo no pierda cual
    /// es la identidad del periodo.
    /// </summary>
    public static string Label(this PeriodDto period)
    {
        var mes = new CultureInfo("es-ES").DateTimeFormat.GetMonthName(period.Month);
        var derivado = $"{mes} {period.Year}";
        var nombre = (period.Name ?? string.Empty).Trim();

        return string.Equals(nombre, derivado, StringComparison.OrdinalIgnoreCase)
            ? nombre
            : $"{nombre} ({derivado})";
    }
}