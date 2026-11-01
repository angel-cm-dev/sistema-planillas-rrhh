using Planillas.Domain.Common;
using Planillas.Domain.Entities;
using Planillas.Domain.Enums;

namespace Planillas.UnitTests;

public class SolicitudVacacionesTests
{
    private static readonly DateOnly Inicio = new(2026, 12, 1);

    [Fact]
    public void Dias_se_cuentan_en_calendario_e_inclusivos()
    {
        var s = SolicitudVacaciones.Crear(1, Inicio, Inicio.AddDays(14), null, 30);
        Assert.Equal(15, s.Dias);
        Assert.Equal(EstadoSolicitud.Pendiente, s.Estado);
    }

    [Fact]
    public void No_permite_exceder_el_saldo_disponible()
    {
        var ex = Assert.Throws<DomainException>(() => SolicitudVacaciones.Crear(1, Inicio, Inicio.AddDays(10), null, 5));
        Assert.Contains("saldo", ex.Message);
    }

    [Fact]
    public void Fecha_fin_anterior_al_inicio_es_invalida()
    {
        Assert.Throws<DomainException>(() => SolicitudVacaciones.Crear(1, Inicio, Inicio.AddDays(-1), null, 30));
    }

    [Fact]
    public void Aprobar_registra_revisor_y_fecha()
    {
        var s = SolicitudVacaciones.Crear(1, Inicio, Inicio.AddDays(4), null, 30);
        s.Aprobar(revisorId: 7, comentario: "Ok");

        Assert.Equal(EstadoSolicitud.Aprobada, s.Estado);
        Assert.Equal(7, s.RevisadoPorId);
        Assert.NotNull(s.FechaRevision);
    }

    [Fact]
    public void No_se_puede_aprobar_una_solicitud_ya_procesada()
    {
        var s = SolicitudVacaciones.Crear(1, Inicio, Inicio.AddDays(4), null, 30);
        s.Rechazar(7, "Temporada alta");

        Assert.Throws<DomainException>(() => s.Aprobar(7, null));
        Assert.Throws<DomainException>(() => s.Cancelar());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Rechazar_exige_motivo(string? comentario)
    {
        var s = SolicitudVacaciones.Crear(1, Inicio, Inicio.AddDays(4), null, 30);
        Assert.Throws<DomainException>(() => s.Rechazar(7, comentario));
        Assert.Equal(EstadoSolicitud.Pendiente, s.Estado);
    }

    [Fact]
    public void Detecta_solapamiento_de_fechas()
    {
        var s = SolicitudVacaciones.Crear(1, Inicio, Inicio.AddDays(9), null, 30);
        Assert.True(s.SeSolapaCon(Inicio.AddDays(9), Inicio.AddDays(12)));
        Assert.False(s.SeSolapaCon(Inicio.AddDays(10), Inicio.AddDays(12)));
    }
}

public class EmpleadoTests
{
    [Fact]
    public void Gana_30_dias_por_cada_anio_completo()
    {
        var e = new Empleado { FechaIngreso = new DateOnly(2023, 6, 15) };
        Assert.Equal(60, e.DiasVacacionesGanados(new DateOnly(2025, 6, 15)));
        Assert.Equal(30, e.DiasVacacionesGanados(new DateOnly(2025, 6, 14)));
        Assert.Equal(0, e.DiasVacacionesGanados(new DateOnly(2024, 1, 1)));
    }

    [Fact]
    public void Regimen_afp_requiere_afp()
    {
        var e = new Empleado { Regimen = RegimenPensionario.Afp };
        Assert.Throws<DomainException>(e.NormalizarRegimen);
    }

    [Fact]
    public void Regimen_onp_limpia_datos_de_afp()
    {
        var e = new Empleado { Regimen = RegimenPensionario.Onp, AfpId = 2, Cuspp = "123" };
        e.NormalizarRegimen();
        Assert.Null(e.AfpId);
        Assert.Null(e.Cuspp);
    }
}

public class ContratoYPlanillaTests
{
    [Fact]
    public void Plazo_fijo_requiere_fecha_fin()
    {
        var c = new Contrato { Tipo = TipoContrato.PlazoFijo, FechaInicio = new DateOnly(2026, 1, 1) };
        Assert.Throws<DomainException>(c.Validar);
    }

    [Fact]
    public void Finalizar_dos_veces_es_invalido()
    {
        var c = new Contrato { Tipo = TipoContrato.Indeterminado, FechaInicio = new DateOnly(2026, 1, 1) };
        c.Finalizar(new DateOnly(2026, 6, 30));
        Assert.Equal(EstadoContrato.Finalizado, c.Estado);
        Assert.Throws<DomainException>(() => c.Finalizar(new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void Planilla_cerrada_no_es_editable()
    {
        var p = new Planilla { Anio = 2026, Mes = 9 };
        p.Boletas.Add(new BoletaPago());
        p.Cerrar();

        Assert.Equal(EstadoPlanilla.Cerrada, p.Estado);
        Assert.Throws<DomainException>(p.AsegurarEditable);
    }

    [Fact]
    public void No_se_cierra_una_planilla_vacia()
    {
        Assert.Throws<DomainException>(new Planilla { Anio = 2026, Mes = 9 }.Cerrar);
    }
}
