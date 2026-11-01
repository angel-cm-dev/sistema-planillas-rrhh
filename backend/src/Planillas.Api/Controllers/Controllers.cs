using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planillas.Api.Infrastructure;
using Planillas.Application.Common;
using Planillas.Application.Features.Asistencias;
using Planillas.Application.Features.Auth;
using Planillas.Application.Features.Catalogos;
using Planillas.Application.Features.Contratos;
using Planillas.Application.Features.Dashboard;
using Planillas.Application.Features.Empleados;
using Planillas.Application.Features.Planillas;
using Planillas.Application.Features.Vacaciones;
using Planillas.Domain.Payroll;

namespace Planillas.Api.Controllers;

// Controladores delgados: solo enrutan HTTP → caso de uso. Validación, reglas y errores viven en
// las capas internas (servicios de aplicación, dominio y GlobalExceptionHandler).

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Inicia sesión y devuelve un JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken ct) => auth.LoginAsync(request, ct);

    /// <summary>Datos del usuario autenticado.</summary>
    [HttpGet("me")]
    [Authorize]
    public Task<UsuarioDto> Me(CancellationToken ct) => auth.MeAsync(ct);
}

[ApiController]
[Route("api/empleados")]
[Authorize(Roles = Roles.Gestores)]
public class EmpleadosController(EmpleadoService service) : ControllerBase
{
    /// <summary>Lista paginada. Filtros: search, areaId, regimen (Onp|Afp), activo. Orden: nombre, dni, sueldo, ingreso, area.</summary>
    [HttpGet]
    public Task<PagedResult<EmpleadoListDto>> List([FromQuery] EmpleadoQuery q, CancellationToken ct) => service.ListAsync(q, ct);

    [HttpGet("{id:int}")]
    public Task<EmpleadoDetailDto> Get(int id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    [ProducesResponseType<EmpleadoDetailDto>(201)]
    [ProducesResponseType<ValidationProblemDetails>(400)]
    public async Task<ActionResult<EmpleadoDetailDto>> Create(EmpleadoRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public Task<EmpleadoDetailDto> Update(int id, EmpleadoRequest request, CancellationToken ct) => service.UpdateAsync(id, request, ct);

    /// <summary>Baja lógica (el empleado queda inactivo; sus boletas se conservan).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DesactivarAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Perfil del usuario autenticado vinculado a un empleado (cualquier rol).</summary>
[ApiController]
[Route("api/perfil")]
[Authorize]
public class PerfilController(EmpleadoService service) : ControllerBase
{
    [HttpGet]
    public Task<EmpleadoDetailDto> Get(CancellationToken ct) => service.GetMineAsync(ct);
}

[ApiController]
[Route("api/catalogos")]
[Authorize]
public class CatalogosController(CatalogoService service) : ControllerBase
{
    [HttpGet("areas")]
    public Task<IReadOnlyList<CatalogoDto>> Areas(CancellationToken ct) => service.AreasAsync(ct);

    [HttpPost("areas")]
    [Authorize(Roles = Roles.Admin)]
    public Task<CatalogoDto> CrearArea(CatalogoRequest r, CancellationToken ct) => service.CrearAreaAsync(r, ct);

    [HttpGet("cargos")]
    public Task<IReadOnlyList<CatalogoDto>> Cargos(CancellationToken ct) => service.CargosAsync(ct);

    [HttpPost("cargos")]
    [Authorize(Roles = Roles.Admin)]
    public Task<CatalogoDto> CrearCargo(CatalogoRequest r, CancellationToken ct) => service.CrearCargoAsync(r, ct);

    [HttpGet("afps")]
    public Task<IReadOnlyList<AfpDto>> Afps(CancellationToken ct) => service.AfpsAsync(ct);

    /// <summary>Actualiza las tasas de una AFP (solo Admin).</summary>
    [HttpPut("afps/{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public Task<AfpDto> ActualizarAfp(int id, AfpRequest r, CancellationToken ct) => service.ActualizarAfpAsync(id, r, ct);

    [HttpGet("parametros")]
    [Authorize(Roles = Roles.Gestores)]
    public Task<ParametrosDto> Parametros(CancellationToken ct) => service.ParametrosAsync(ct);

    /// <summary>Actualiza UIT, RMV y tasas legales (solo Admin).</summary>
    [HttpPut("parametros")]
    [Authorize(Roles = Roles.Admin)]
    public Task<ParametrosDto> ActualizarParametros(ParametrosDto r, CancellationToken ct) => service.ActualizarParametrosAsync(r, ct);
}

[ApiController]
[Route("api/contratos")]
[Authorize(Roles = Roles.Gestores)]
public class ContratosController(ContratoService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ContratoDto>> List([FromQuery] ContratoQuery q, CancellationToken ct) => service.ListAsync(q, ct);

    [HttpPost]
    [ProducesResponseType<ContratoDto>(201)]
    public async Task<ActionResult<ContratoDto>> Create(ContratoRequest r, CancellationToken ct)
    {
        var created = await service.CreateAsync(r, ct);
        return Created($"/api/contratos/{created.Id}", created);
    }

    [HttpPost("{id:int}/finalizar")]
    public Task<ContratoDto> Finalizar(int id, FinalizarContratoRequest r, CancellationToken ct) => service.FinalizarAsync(id, r, ct);
}

[ApiController]
[Route("api/asistencias")]
[Authorize]
public class AsistenciasController(AsistenciaService service) : ControllerBase
{
    /// <summary>Gestores ven todo; un empleado solo su propia asistencia.</summary>
    [HttpGet]
    public Task<PagedResult<AsistenciaDto>> List([FromQuery] AsistenciaQuery q, CancellationToken ct) => service.ListAsync(q, ct);

    /// <summary>Registra o corrige la asistencia de un día (upsert por empleado + fecha).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Gestores)]
    public Task<AsistenciaDto> Registrar(AsistenciaRequest r, CancellationToken ct) => service.RegistrarAsync(r, ct);
}

[ApiController]
[Route("api/vacaciones")]
[Authorize]
public class VacacionesController(VacacionService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<SolicitudDto>> List([FromQuery] VacacionQuery q, CancellationToken ct) => service.ListAsync(q, ct);

    /// <summary>Saldo de días. Sin empleadoId devuelve el del usuario autenticado.</summary>
    [HttpGet("saldo")]
    public Task<SaldoVacacionesDto> Saldo([FromQuery] int? empleadoId, CancellationToken ct) => service.SaldoAsync(empleadoId, ct);

    [HttpPost]
    [ProducesResponseType<SolicitudDto>(201)]
    public async Task<ActionResult<SolicitudDto>> Crear(SolicitudRequest r, CancellationToken ct)
    {
        var created = await service.CrearAsync(r, ct);
        return Created($"/api/vacaciones/{created.Id}", created);
    }

    [HttpPost("{id:int}/aprobar")]
    [Authorize(Roles = Roles.Gestores)]
    public Task<SolicitudDto> Aprobar(int id, RevisionRequest r, CancellationToken ct) => service.AprobarAsync(id, r, ct);

    [HttpPost("{id:int}/rechazar")]
    [Authorize(Roles = Roles.Gestores)]
    public Task<SolicitudDto> Rechazar(int id, RevisionRequest r, CancellationToken ct) => service.RechazarAsync(id, r, ct);

    [HttpPost("{id:int}/cancelar")]
    public Task<SolicitudDto> Cancelar(int id, CancellationToken ct) => service.CancelarAsync(id, ct);
}

[ApiController]
[Route("api/planillas")]
[Authorize(Roles = Roles.Gestores)]
public class PlanillasController(PlanillaService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<PlanillaResumenDto>> List([FromQuery] PlanillaQuery q, CancellationToken ct) => service.ListAsync(q, ct);

    [HttpGet("{id:int}")]
    public Task<PlanillaDetalleDto> Get(int id, CancellationToken ct) => service.GetAsync(id, ct);

    /// <summary>Genera o recalcula (si está en Borrador) la planilla del periodo indicado.</summary>
    [HttpPost("generar")]
    public Task<PlanillaDetalleDto> Generar(GenerarPlanillaRequest r, CancellationToken ct) => service.GenerarAsync(r, ct);

    /// <summary>Cierra la planilla: queda inmutable y las boletas se publican a los empleados.</summary>
    [HttpPost("{id:int}/cerrar")]
    public Task<PlanillaResumenDto> Cerrar(int id, CancellationToken ct) => service.CerrarAsync(id, ct);

    /// <summary>Calculadora didáctica: calcula una boleta hipotética sin guardar nada.</summary>
    [HttpPost("simular")]
    public Task<ResultadoCalculo> Simular(SimulacionRequest r, CancellationToken ct) => service.SimularAsync(r, ct);
}

[ApiController]
[Route("api/boletas")]
[Authorize]
public class BoletasController(PlanillaService service) : ControllerBase
{
    /// <summary>Boletas del empleado autenticado (solo de planillas cerradas).</summary>
    [HttpGet("mias")]
    public Task<IReadOnlyList<BoletaResumenDto>> Mias(CancellationToken ct) => service.MisBoletasAsync(ct);

    /// <summary>Detalle de boleta. Un empleado solo puede ver las suyas (403 en otro caso).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<BoletaDto>(200)]
    [ProducesResponseType<ProblemDetails>(403)]
    public Task<BoletaDto> Get(int id, CancellationToken ct) => service.GetBoletaAsync(id, ct);
}

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = Roles.Gestores)]
public class DashboardController(DashboardService service) : ControllerBase
{
    [HttpGet]
    public Task<DashboardDto> Get(CancellationToken ct) => service.GetAsync(ct);
}
