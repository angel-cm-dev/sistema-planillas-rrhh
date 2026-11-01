namespace Planillas.Domain.Payroll;

/// <summary>
/// Impuesto a la Renta de 5ta categoría — APROXIMACIÓN DIDÁCTICA.
///
/// Procedimiento simplificado:
///   1. Proyección anual = remuneración del mes × 14 (12 sueldos + 2 gratificaciones).
///   2. Renta neta = proyección − 7 UIT (deducción de ley).
///   3. Impuesto anual con escala progresiva acumulativa (8%, 14%, 17%, 20%, 30%).
///   4. Retención mensual = impuesto anual / 12.
///
/// El procedimiento real de SUNAT (art. 40 del Reglamento) usa divisores distintos según el mes
/// (12, 9, 8, 5, 4, 1), resta retenciones previas y considera otros ingresos del año.
/// Por eso aquí el resultado es REFERENCIAL y no debe usarse para declarar impuestos.
/// </summary>
public static class Renta5taCategoria
{
    public const decimal FactorProyeccion = 14m;
    public const decimal DeduccionUit = 7m;

    /// <summary>Tramos en UIT (límite superior acumulado) y su tasa marginal.</summary>
    public static readonly IReadOnlyList<(decimal HastaUit, decimal Tasa)> Tramos =
    [
        (5m, 0.08m),
        (20m, 0.14m),
        (35m, 0.17m),
        (45m, 0.20m),
        (decimal.MaxValue, 0.30m)
    ];

    public static decimal ImpuestoAnual(decimal rentaNetaAnual, decimal uit)
    {
        if (rentaNetaAnual <= 0) return 0m;
        decimal impuesto = 0, limiteInferior = 0;
        foreach (var (hastaUit, tasa) in Tramos)
        {
            var limiteSuperior = hastaUit == decimal.MaxValue ? decimal.MaxValue : hastaUit * uit;
            if (rentaNetaAnual <= limiteInferior) break;
            var baseTramo = Math.Min(rentaNetaAnual, limiteSuperior) - limiteInferior;
            impuesto += baseTramo * tasa;
            limiteInferior = limiteSuperior;
        }
        return impuesto;
    }

    public static decimal RetencionMensual(decimal remuneracionMensual, decimal uit)
    {
        var proyeccion = remuneracionMensual * FactorProyeccion;
        var rentaNeta = proyeccion - DeduccionUit * uit;
        return CalculadoraPlanilla.Redondear(ImpuestoAnual(rentaNeta, uit) / 12m);
    }
}
