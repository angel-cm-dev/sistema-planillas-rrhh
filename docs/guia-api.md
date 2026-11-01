# Guía de la API

Base URL: `http://localhost:5080` (con Docker o `dotnet run`). Documentación interactiva: **`/swagger`** (OpenAPI en `/openapi/v1.json`).

Todas las respuestas de error usan **ProblemDetails** (`application/problem+json`):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Datos de entrada inválidos",
  "status": 400,
  "detail": "Uno o más campos no son válidos.",
  "instance": "/api/empleados",
  "errors": { "dni": ["El DNI debe tener 8 dígitos."] },
  "traceId": "00-6b15b6b0..."
}
```

> Los ejemplos usan sintaxis bash. En PowerShell reemplace `\` por `` ` `` y use `curl.exe`.

## 1. Autenticación

```bash
TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@demo.pe","password":"Demo123!"}' | jq -r .token)

curl -s http://localhost:5080/api/auth/me -H "Authorization: Bearer $TOKEN"
```

Respuesta del login:

```json
{
  "token": "eyJhbGciOi...",
  "expiraEn": "2026-11-02T04:00:00Z",
  "usuario": { "id": 1, "email": "admin@demo.pe", "nombreCompleto": "Administrador Demo", "rol": "Admin", "empleadoId": null }
}
```

## 2. Empleados (Admin, Rrhh)

```bash
# Listado paginado, filtrado y ordenado
curl -s "http://localhost:5080/api/empleados?search=quispe&regimen=Afp&activo=true&sortBy=sueldo&sortDir=desc&page=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"

# Crear
curl -s -X POST http://localhost:5080/api/empleados \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{
    "dni":"71234567","nombres":"Valeria","apellidos":"Rojas Pinto","email":"valeria.rojas@empresa-demo.pe",
    "fechaIngreso":"2025-03-01","areaId":3,"cargoId":3,"sueldoBasico":4200,"tieneHijos":true,
    "regimen":"Afp","afpId":2,"cuspp":"612345VRP0X1"
  }'

# Baja lógica
curl -s -X DELETE http://localhost:5080/api/empleados/13 -H "Authorization: Bearer $TOKEN" -o /dev/null -w "%{http_code}\n"   # 204
```

Ordenamiento disponible (`sortBy`): `nombre`, `dni`, `sueldo`, `ingreso`, `area`.

## 3. Asistencia

```bash
# Registrar (o corregir) la asistencia de un día — upsert por empleado + fecha
curl -s -X POST http://localhost:5080/api/asistencias \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"empleadoId":1,"fecha":"2026-10-30","estado":"Presente","horaEntrada":"08:00:00","horaSalida":"20:00:00","horasExtra":3,"observacion":"Cierre de mes"}'

curl -s "http://localhost:5080/api/asistencias?empleadoId=1&desde=2026-10-01&hasta=2026-10-31" -H "Authorization: Bearer $TOKEN"
```

## 4. Vacaciones (flujo de aprobación)

```bash
EMP=$(curl -s -X POST http://localhost:5080/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"empleado@demo.pe","password":"Demo123!"}' | jq -r .token)

# El empleado consulta su saldo y solicita
curl -s http://localhost:5080/api/vacaciones/saldo -H "Authorization: Bearer $EMP"
curl -s -X POST http://localhost:5080/api/vacaciones -H "Authorization: Bearer $EMP" -H "Content-Type: application/json" \
  -d '{"fechaInicio":"2027-01-10","fechaFin":"2027-01-24","motivo":"Viaje"}'

# RR.HH. aprueba o rechaza (el rechazo exige comentario)
curl -s -X POST http://localhost:5080/api/vacaciones/5/aprobar  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"comentario":"Aprobado"}'
curl -s -X POST http://localhost:5080/api/vacaciones/5/rechazar -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"comentario":"Cierre contable"}'
```

| Situación | Respuesta |
|---|---|
| Fechas solapadas con otra solicitud viva | `409 Conflict` |
| Excede el saldo | `422 Unprocessable Entity` |
| Aprobar/rechazar algo que no está `Pendiente` | `422` |
| Un empleado intenta aprobar | `403 Forbidden` |

## 5. Planilla

```bash
# Generar (o recalcular si está en Borrador) el periodo
curl -s -X POST http://localhost:5080/api/planillas/generar \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"anio":2026,"mes":10}'

# Ver detalle y cerrar (después de cerrar es inmutable y visible para los empleados)
curl -s http://localhost:5080/api/planillas/3 -H "Authorization: Bearer $TOKEN"
curl -s -X POST http://localhost:5080/api/planillas/3/cerrar -H "Authorization: Bearer $TOKEN"

# Simulador didáctico (no guarda nada)
curl -s -X POST http://localhost:5080/api/planillas/simular \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"sueldoBasico":3500,"tieneHijos":true,"regimen":"Afp","afpId":2,"diasFalta":1,"horasExtraPorDia":[2,3]}'
```

## 6. Boletas

```bash
curl -s http://localhost:5080/api/boletas/mias -H "Authorization: Bearer $EMP"     # solo planillas cerradas
curl -s http://localhost:5080/api/boletas/1    -H "Authorization: Bearer $EMP"     # 403 si no es suya
```

## 7. Parámetros (Admin)

```bash
curl -s http://localhost:5080/api/catalogos/parametros -H "Authorization: Bearer $TOKEN"

curl -s -X PUT http://localhost:5080/api/catalogos/parametros \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"uit":5350,"rmv":1130,"asignacionFamiliarPct":10,"onpPct":13,"esSaludPct":9,"horaExtra25Pct":25,"horaExtra35Pct":35,"topePrimaAfp":12234.34,"horasJornadaDiaria":8}'

curl -s -X PUT http://localhost:5080/api/catalogos/afps/2 \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"aportePct":10,"primaSeguroPct":1.37,"comisionPct":1.55}'
```

## 8. Salud

```bash
curl -s http://localhost:5080/health
# {"status":"Healthy","checks":[{"name":"database","status":"Healthy"}]}
```
