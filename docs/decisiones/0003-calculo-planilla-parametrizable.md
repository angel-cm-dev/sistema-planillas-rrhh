# ADR-0003 · Cálculo de planilla como función pura con parámetros en BD

- **Estado:** Aceptada
- **Fecha:** 2026-11

## Contexto

Las tasas legales peruanas cambian (UIT y RMV anualmente; comisiones AFP según la SBS). Además, la planilla de un mes ya pagado no debe cambiar aunque luego se actualicen sueldos o tasas.

## Decisión

1. `CalculadoraPlanilla.Calcular(entrada, parametros)` es una **función pura** del dominio: recibe todos los datos y devuelve cada concepto por separado. No accede a BD ni al reloj.
2. Los valores legales viven en las tablas `ParametrosPlanilla` y `Afps`, editables por el rol Admin. **No hay tasas en el código** (salvo constantes estructurales: mes comercial de 30 días, 2 h con sobretasa menor, factor 14 y 7 UIT de la aproximación de renta).
3. Cada `BoletaPago` guarda los montos calculados y datos del trabajador (**snapshot**). Una planilla `Cerrada` es inmutable; mientras está en `Borrador` puede regenerarse.
4. La renta de 5ta categoría se implementa como **aproximación documentada** (proyección × 14 − 7 UIT, escala progresiva, ÷ 12), no como el procedimiento mensual completo de SUNAT.

## Consecuencias

- ✔ El cálculo se prueba exhaustivamente con valores conocidos (ONP 13% de 1500 = 195.00; renta de un sueldo de 10,000 = 1,062.67).
- ✔ Actualizar la UIT o una comisión no requiere desplegar código.
- ✔ El simulador reutiliza exactamente la misma función que la planilla real.
- ✘ Gratificaciones, CTS, prorrateos e ingresos variables quedan fuera del alcance (ver roadmap).
- ✘ La renta de 5ta puede diferir del cálculo oficial en meses distintos a enero; el README y la boleta lo advierten.
