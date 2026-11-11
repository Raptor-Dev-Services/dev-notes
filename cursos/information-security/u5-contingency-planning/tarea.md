# Tarea - U5 - Análisis de impacto al negocio de una institución de crédito

**Seguridad de la información**
**Tipo:** individual.
**Tiempo estimado:** 4 horas.
**Valor:** 7.5 puntos.

---

## Objetivo

Determinar las características y los elementos que un plan de continuidad del negocio debe considerar, a partir de un análisis de impacto al negocio (BIA) hecho conforme a la regulación bancaria mexicana.

---

## Parte 1 - Lecturas (1 hora)

| Recurso | Para qué |
|---|---|
| [`14-plan-de-continuidad.md`](../../../12-seguridad/04-seguridad-informacion/14-plan-de-continuidad.md) | El BIA, sus pasos y sus métricas |
| Disposiciones de la CNBV para instituciones de crédito: artículo 164 Bis y Anexo 67 | Lo que se exige al plan de continuidad de un banco |
| NIST SP 800-34 Rev. 1, sección del BIA | La plantilla del análisis |

---

## Parte 2 - El análisis (3 horas)

Trabajen con una institución de crédito ficticia o con la información pública de una real, sin datos confidenciales.

### 1. Procesos críticos

Identifiquen y clasifiquen por criticidad al menos cinco procesos (por ejemplo: pagos y transferencias, banca electrónica, operación de sucursales y cajeros, otorgamiento de crédito, reportes regulatorios). Para cada uno, los recursos de los que depende: sistemas y bases de datos, personal clave, sedes y proveedores.

### 2. Impacto en el tiempo

Para cada proceso, estimen el impacto de una interrupción a 1 hora, 4 horas, 24 horas y una semana, en cuatro dimensiones: financiera, a clientes, regulatoria y reputacional. Usen una escala explícita (por ejemplo, de 1 a 5).

### 3. Métricas de recuperación

| Proceso | MTD | RTO | RPO | Nivel mínimo de operación | Dependencias |
|---|---|---|---|---|---|

Verifiquen que el RTO sea menor que el MTD y expliquen de dónde salen las cifras.

### 4. Prioridades e implicaciones

- El orden de recuperación de los recursos.
- Qué estrategias de continuidad requieren esas métricas (respaldos, sitio alterno, acuerdos con proveedores).
- Qué contingencias, según el artículo 164 Bis, obligarían a avisar a la CNBV, y en qué plazo.

Pueden presentarlo como documento o como presentación, siempre con introducción, desarrollo y conclusiones.

---

## Entregable

Un archivo PDF (documento o presentación) en Arial 12 con interlineado 1.5, con introducción, las cuatro partes del análisis, conclusión y referencias en APA 7.

---

## Rúbrica

| Criterio | Excelente | Bueno | Regular | Deficiente |
|---|---|---|---|---|
| **Presentación y ortografía** (1 pt) | Cumple el formato, sin errores: 1 | De 2 a 4 errores: 0.7 | Se aleja del formato, de 5 a 7 errores: 0.4 | No cumple el formato u 8 errores o más: 0 |
| **Comparación de componentes** (1.5 pts) | Compara procesos con detalle por criticidad, impacto y métricas: 1.5 | Comparación general: 1.13 | Comparación escueta: 0.75 | No compara: 0 |
| **Calidad del contenido** (3 pts) | El análisis se apega a la regulación, con cifras justificadas y ejemplos: 3 | Correcto con pocos ejemplos: 2.25 | Relación vaga con la regulación: 1.5 | No se relaciona con el tema: 0 |
| **Conclusiones** (2 pts) | Claras, en lenguaje sencillo, y se desprenden del análisis: 2 | Ligadas al trabajo con poca precisión: 1.33 | Ambiguas: 0.67 | Confusas: 0 |

**Total:** 7.5 puntos.

---

## Referencias

- Comisión Nacional Bancaria y de Valores. (2005, con reformas posteriores). *Disposiciones de carácter general aplicables a las instituciones de crédito*. CNBV. https://www.cnbv.gob.mx/Normatividad/Disposiciones%20de%20car%C3%A1cter%20general%20aplicables%20a%20las%20instituciones%20de%20cr%C3%A9dito.pdf
- Swanson, M., Bowen, P., Phillips, A. W., Gallup, D., y Lynes, D. (2010). *Contingency planning guide for federal information systems* (NIST Special Publication 800-34 Rev. 1). National Institute of Standards and Technology. https://csrc.nist.gov/pubs/sp/800/34/r1/upd1/final

---

*Rogelio Arriaga Gonzalez*
