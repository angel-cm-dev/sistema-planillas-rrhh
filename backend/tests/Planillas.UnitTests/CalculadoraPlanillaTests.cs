using Planillas.Domain.Common;
using Planillas.Domain.Entities;
using Planillas.Domain.Enums;
using Planillas.Domain.Payroll;

namespace Planillas.UnitTests;

public class CalculadoraPlanillaTests
{
    // Parámetros fijos para que los resultados esperados no dependan del seed.
    private static readonly ParametrosPlanilla P = new()
    {
        Uit = 5350m, Rmv = 1130m, AsignacionFamiliarPct = 10m, OnpPct = 13m, EsSaludPct = 9m,
        HoraExtra25Pct = 25m, HoraExtra35Pct = 35m, TopePrimaAfp = 12234.34m, HorasJornadaDiaria = 8m
    };

    private static readonly TasasAfp Integra = new("Integra", 10m, 1.37m, 1.55m);

    private static EntradaCalculo Entrada(decimal sueldo, RegimenPensionario regimen = RegimenPensionario.Onp,
        bool hijos = false, int faltas = 0, decimal[]? horasExtra = null) =>
        new(sueldo, hijos, regimen, regimen == RegimenPensionario.Afp ? Integra : null, faltas, horasExtra ?? []);

    [Fact]
    public void Onp_descuenta_13_por_ciento_de_la_remuneracion()
    {
        var r = CalculadoraPlanilla.Calcular(Entrada(1500m), P);

        Assert.Equal(1500m, r.TotalIngresos);
        Assert.Equal(195m, r.Onp);
        Assert.Equal(0m, r.AfpAporte);
        Assert.Equal(195m, r.TotalDescuentos);
        Assert.Equal(1305m, r.NetoPagar);
    }

    [Fact]
    public void Afp_suma_aporte_prima_y_comision()
    {
        var r = CalculadoraPlanilla.Calcular(Entrada(1500m, RegimenPensionario.Afp), P);

        Assert.Equal(150m, r.AfpAporte);     // 10%
        Assert.Equal(20.55m, r.AfpPrima);    // 1.37%
        Assert.Equal(23.25m, r.AfpComision); // 1.55%
        Assert.Equal(0m, r.Onp);
        Assert.Equal(1500m - 193.80m, r.NetoPagar);
    }

    [Fact]
    public void Prima_de_seguro_se_calcula_hasta_el_tope_asegurable()
    {
        var r = CalculadoraPlanilla.Calcular(Entrada(20000m, RegimenPensionario.Afp), P);

        Assert.Equal(CalculadoraPlanilla.Redondear(12234.34m * 0.0137m), r.AfpPrima);
        Assert.Equal(2000m, r.AfpAporte); // el aporte NO tiene tope
    }

    [Fact]
    public void Asignacion_familiar_es_10_por_ciento_de_la_RMV_no_del_sueldo()
    {
        var r = CalculadoraPlanilla.Calcular(Entrada(5000m, hijos: true), P);

        Assert.Equal(113m, r.AsignacionFamiliar);
        Assert.Equal(5113m, r.TotalIngresos);
    }

    [Fact]
    public void Horas_extra_primeras_dos_al_25_y_resto_al_35_por_ciento()
    {
        // Valor hora = 1500 / 30 / 8 = 6.25
        // Día con 3 h: 2 × 6.25 × 1.25 = 15.625 ; 1 × 6.25 × 1.35 = 8.4375 → 24.0625 → 24.06
        var r = CalculadoraPlanilla.Calcular(Entrada(1500m, horasExtra: [3m]), P);

        Assert.Equal(6.25m, r.ValorHora);
        Assert.Equal(2m, r.HorasExtra25);
        Assert.Equal(1m, r.HorasExtra35);
        Assert.Equal(24.06m, r.MontoHorasExtra);
    }

    [Fact]
    public void Las_horas_extra_se_tramifican_por_dia_y_no_por_mes()
    {
        // Dos días de 2 h → las 4 h van al 25% (si se sumara el mes, 2 h irían al 35%).
        var r = CalculadoraPlanilla.Calcular(Entrada(1500m, horasExtra: [2m, 2m]), P);

        Assert.Equal(4m, r.HorasExtra25);
        Assert.Equal(0m, r.HorasExtra35);
        Assert.Equal(31.25m, r.MontoHorasExtra);
    }

    [Fact]
    public void Faltas_descuentan_el_valor_dia_del_basico()
    {
        var r = CalculadoraPlanilla.Calcular(Entrada(3000m, faltas: 2), P);

        Assert.Equal(200m, r.DescuentoFaltas);
        Assert.Equal(2800m, r.TotalIngresos);
        Assert.Equal(364m, r.Onp); // 13% de la remuneración ya descontada
    }

    [Fact]
    public void EsSalud_es_aporte_del_empleador_y_no_afecta_el_neto()
    {
        var r = CalculadoraPlanilla.Calcular(Entrada(2000m), P);

        Assert.Equal(180m, r.EsSalud);
        Assert.Equal(2000m - 260m, r.NetoPagar);
        Assert.Equal(2180m, r.CostoEmpleador);
    }

    [Fact]
    public void EsSalud_usa_la_RMV_como_base_minima()
    {
        var r = CalculadoraPlanilla.Calcular(Entrada(1130m, faltas: 10), P);

        Assert.True(r.TotalIngresos < P.Rmv);
        Assert.Equal(101.70m, r.EsSalud); // 9% de 1130
    }

    [Fact]
    public void Sueldo_bajo_no_paga_renta_de_quinta()
    {
        var r = CalculadoraPlanilla.Calcular(Entrada(2500m), P);
        Assert.Equal(0m, r.Renta5ta);
    }

    [Fact]
    public void Sueldo_alto_retiene_renta_de_quinta()
    {
        var r = CalculadoraPlanilla.Calcular(Entrada(10000m), P);
        Assert.Equal(1062.67m, r.Renta5ta);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Sueldo_no_positivo_es_invalido(decimal sueldo)
    {
        Assert.Throws<DomainException>(() => CalculadoraPlanilla.Calcular(Entrada(sueldo), P));
    }

    [Fact]
    public void Afiliado_afp_sin_tasas_es_invalido()
    {
        var entrada = new EntradaCalculo(2000m, false, RegimenPensionario.Afp, null, 0, []);
        Assert.Throws<DomainException>(() => CalculadoraPlanilla.Calcular(entrada, P));
    }
}

public class Renta5taCategoriaTests
{
    private const decimal Uit = 5350m;

    [Fact]
    public void Renta_neta_cero_o_negativa_no_genera_impuesto()
    {
        Assert.Equal(0m, Renta5taCategoria.ImpuestoAnual(0m, Uit));
        Assert.Equal(0m, Renta5taCategoria.ImpuestoAnual(-500m, Uit));
    }

    [Fact]
    public void Primer_tramo_hasta_5_UIT_al_8_por_ciento()
    {
        Assert.Equal(26750m * 0.08m, Renta5taCategoria.ImpuestoAnual(26750m, Uit));
    }

    [Fact]
    public void Escala_es_progresiva_y_acumulativa()
    {
        // 102,550 = 26,750 al 8% + 75,800 al 14%
        Assert.Equal(2140m + 10612m, Renta5taCategoria.ImpuestoAnual(102550m, Uit));
    }

    [Fact]
    public void Tramo_superior_aplica_30_por_ciento_sobre_el_exceso_de_45_UIT()
    {
        var hasta45 = 5 * Uit * 0.08m + 15 * Uit * 0.14m + 15 * Uit * 0.17m + 10 * Uit * 0.20m;
        Assert.Equal(hasta45 + 1000m * 0.30m, Renta5taCategoria.ImpuestoAnual(45 * Uit + 1000m, Uit));
    }

    [Fact]
    public void Retencion_mensual_es_impuesto_anual_proyectado_entre_12()
    {
        // 10,000 × 14 = 140,000 − 7 UIT (37,450) = 102,550 → 12,752 / 12 = 1,062.67
        Assert.Equal(1062.67m, Renta5taCategoria.RetencionMensual(10000m, Uit));
    }
}
