using Planillas.Domain.Common;
using Planillas.Domain.Entities;
using Planillas.Domain.Enums;

namespace Planillas.Domain.Payroll;

/// <summary>Tasas de la AFP vigentes al momento del cálculo (porcentajes: 10 = 10%).</summary>
public sealed record TasasAfp(string Nombre, decimal AportePct, decimal PrimaSeguroPct, decimal ComisionPct);

/// <summary>Datos de entrada de un trabajador para un periodo mensual.</summary>
public sealed record EntradaCalculo(
    decimal SueldoBasico,
    bool TieneHijos,
    RegimenPensionario Regimen,
    TasasAfp? Afp,
    int DiasFalta,
    IReadOnlyCollection<decimal> HorasExtraPorDia);

/// <summary>Resultado detallado del cálculo. Cada concepto se expone para que la boleta sea explicable.</summary>
public sealed record ResultadoCalculo
{
    public decimal SueldoBasico { get; init; }
    public decimal AsignacionFamiliar { get; init; }
    public decimal ValorHora { get; init; }
    public decimal HorasExtra25 { get; init; }
    public decimal HorasExtra35 { get; init; }
    public decimal MontoHorasExtra { get; init; }
    public int DiasFalta { get; init; }
    public decimal DescuentoFaltas { get; init; }
    public decimal TotalIngresos { get; init; }
    public decimal Onp { get; init; }
    public decimal AfpAporte { get; init; }
    public decimal AfpPrima { get; init; }
    public decimal AfpComision { get; init; }
    public decimal Renta5ta { get; init; }
    public decimal TotalDescuentos { get; init; }
    public decimal NetoPagar { get; init; }
    public decimal EsSalud { get; init; }
    public decimal CostoEmpleador => TotalIngresos + EsSalud;
}

/// <summary>
/// Cálculo DIDÁCTICO de la planilla mensual peruana (régimen general). Es una función pura:
/// mismos datos de entrada ⇒ mismo resultado, sin acceso a BD. Eso la hace trivial de probar.
///
/// Simplificaciones conscientes (documentadas en docs/arquitectura.md):
///  - No contempla gratificaciones, CTS, bonificación extraordinaria ni subsidios en el mes.
///  - La renta de 5ta categoría es una APROXIMACIÓN (ver <see cref="Renta5taCategoria"/>).
///  - Todas las tasas vienen de <see cref="ParametrosPlanilla"/> y <see cref="TasasAfp"/> (configurables).
/// </summary>
public static class CalculadoraPlanilla
{
    /// <summary>Mes comercial de 30 días usado en la práctica laboral peruana para valor día y valor hora.</summary>
    public const int DiasMesComercial = 30;
    /// <summary>Por día, las primeras 2 horas extra llevan la sobretasa menor (25%); las demás, la mayor (35%).</summary>
    public const decimal HorasConSobretasaMenor = 2m;

    public static ResultadoCalculo Calcular(EntradaCalculo entrada, ParametrosPlanilla p)
    {
        if (entrada.SueldoBasico <= 0)
            throw new DomainException("El sueldo básico debe ser mayor a cero.");
        if (entrada.DiasFalta is < 0 or > DiasMesComercial)
            throw new DomainException("Los días de falta deben estar entre 0 y 30.");
        if (entrada.Regimen == RegimenPensionario.Afp && entrada.Afp is null)
            throw new DomainException("Se requieren las tasas de la AFP para un afiliado al SPP.");

        // 1) Asignación familiar: 10% de la RMV (no del sueldo), sin importar cuántos hijos tenga.
        var asignacionFamiliar = entrada.TieneHijos ? Redondear(p.Rmv * p.AsignacionFamiliarPct / 100m) : 0m;

        // 2) Horas extra: valor hora = remuneración ordinaria / 30 días / horas de jornada.
        var valorHora = (entrada.SueldoBasico + asignacionFamiliar) / DiasMesComercial / p.HorasJornadaDiaria;
        decimal he25 = 0, he35 = 0;
        foreach (var horasDia in entrada.HorasExtraPorDia)
        {
            he25 += Math.Min(horasDia, HorasConSobretasaMenor);
            he35 += Math.Max(horasDia - HorasConSobretasaMenor, 0);
        }
        var montoHorasExtra = Redondear(
            he25 * valorHora * (1 + p.HoraExtra25Pct / 100m) +
            he35 * valorHora * (1 + p.HoraExtra35Pct / 100m));

        // 3) Faltas injustificadas: se descuenta el valor día del básico.
        var descuentoFaltas = Redondear(entrada.SueldoBasico / DiasMesComercial * entrada.DiasFalta);

        var totalIngresos = entrada.SueldoBasico + asignacionFamiliar + montoHorasExtra - descuentoFaltas;

        // 4) Sistema pensionario: ONP es una tasa única; AFP se compone de aporte + prima + comisión.
        decimal onp = 0, afpAporte = 0, afpPrima = 0, afpComision = 0;
        if (entrada.Regimen == RegimenPensionario.Onp)
        {
            onp = Redondear(totalIngresos * p.OnpPct / 100m);
        }
        else
        {
            var afp = entrada.Afp!;
            afpAporte = Redondear(totalIngresos * afp.AportePct / 100m);
            // La prima de seguros solo se calcula hasta la remuneración máxima asegurable.
            afpPrima = Redondear(Math.Min(totalIngresos, p.TopePrimaAfp) * afp.PrimaSeguroPct / 100m);
            afpComision = Redondear(totalIngresos * afp.ComisionPct / 100m);
        }

        // 5) Renta de 5ta categoría (aproximación por proyección anual / 12).
        var renta5ta = Renta5taCategoria.RetencionMensual(totalIngresos, p.Uit);

        var totalDescuentos = onp + afpAporte + afpPrima + afpComision + renta5ta;

        // 6) EsSalud es APORTE DEL EMPLEADOR: no reduce el neto. Su base mínima es la RMV.
        var esSalud = Redondear(Math.Max(totalIngresos, p.Rmv) * p.EsSaludPct / 100m);

        return new ResultadoCalculo
        {
            SueldoBasico = entrada.SueldoBasico,
            AsignacionFamiliar = asignacionFamiliar,
            ValorHora = Redondear(valorHora),
            HorasExtra25 = he25,
            HorasExtra35 = he35,
            MontoHorasExtra = montoHorasExtra,
            DiasFalta = entrada.DiasFalta,
            DescuentoFaltas = descuentoFaltas,
            TotalIngresos = Redondear(totalIngresos),
            Onp = onp,
            AfpAporte = afpAporte,
            AfpPrima = afpPrima,
            AfpComision = afpComision,
            Renta5ta = renta5ta,
            TotalDescuentos = Redondear(totalDescuentos),
            NetoPagar = Redondear(totalIngresos - totalDescuentos),
            EsSalud = esSalud
        };
    }

    /// <summary>Redondeo comercial a céntimos (0.005 sube), como en una boleta real.</summary>
    public static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
