# ADR-0001 · Clean Architecture en cuatro proyectos

- **Estado:** Aceptada
- **Fecha:** 2026-11

## Contexto

El sistema concentra reglas de negocio sensibles (cálculo de remuneraciones, flujo de aprobación de vacaciones, inmutabilidad de planillas cerradas). Esas reglas deben poder probarse de forma aislada y sobrevivir a cambios de base de datos o de framework web.

## Decisión

Separar la solución en `Domain`, `Application`, `Infrastructure` y `Api`, con dependencias que apuntan siempre hacia el dominio. Las reglas viven en entidades (`SolicitudVacaciones.Aprobar`, `Planilla.Cerrar`) y en servicios de dominio puros (`CalculadoraPlanilla`). Los controladores son delgados: solo traducen HTTP a casos de uso.

## Consecuencias

- ✔ La calculadora y las reglas tienen pruebas unitarias rápidas, sin BD ni HTTP.
- ✔ Cambiar SQL Server por otro motor solo afecta `Infrastructure`.
- ✔ Estructura reconocible para cualquier desarrollador .NET.
- ✘ Más proyectos y archivos que una API monolítica para un dominio de tamaño medio.
- Se evitó intencionalmente MediatR/CQRS: los servicios de aplicación por módulo son suficientes y más fáciles de seguir para quien aprende.
