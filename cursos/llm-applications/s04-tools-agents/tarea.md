# Tarea - S04 - Herramientas y agentes

**Aplicaciones con LLM: de la API al agente**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1 a 2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/11-vibe-coding/06-arquitectura-agentica.md` | Todo el documento: busca lo que comparte con el agente de la práctica (piezas con criterio, límites, quién decide) | 40 min |

### Recomendadas

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/11-vibe-coding/07-llm-en-produccion.md` | Salida estructurada; Validar el significado; Errores y reintentos del SDK | 30 min |

### Libros (por capítulo)

| Libro | Capítulos | Para qué |
|-------|-----------|----------|
| *Building LLM Powered Applications* (Valentina Alto, 2024) | Cap. 6 | Agentes y herramientas en una aplicación |
| *Building Data-Driven Applications with LlamaIndex* (Andrei Gheorghiu, 2024) | Cap. 8 | Agentes sobre datos propios |

Los dos son de 2024 y usan librerías de Python: léelos por los conceptos. En el curso el bucle se
escribe a mano, en C#, para ver dónde viven los controles.

---

## Parte 2 - Ejercicio (2 horas)

**La bitácora de cada decisión del agente, legible por una persona.**

La bitácora JSONL de la práctica sirve a una máquina. Quien revisa un incidente necesita leer en dos
minutos qué pidió la persona, qué decidió el agente en cada paso, quién autorizó qué y cuánto costó.

### Lo que construyes

Un comando nuevo del CLI:

```bash
dotnet run --project src/Asistente.Cli -- bitacora bitacoras/agente-20261108-153000.jsonl
```

Que lee el archivo y escribe junto a él un `.md` con este formato:

```markdown
# Corrida del agente - 2026-11-08 15:30 UTC

**Petición:** Una alumna no recibe el correo para confirmar su cuenta...
**Resultado:** terminado en 3 pasos | **Costo:** 0.0021 USD | **Confirmaciones:** 1 autorizada, 0 rechazadas

## Paso 1

| Hora UTC | Tipo | Detalle | Costo acumulado USD |
|---|---|---|---|
| 15:30:02 | herramienta_pedida | buscar_tickets {"texto":"correo de confirmación","soloAbiertos":true} | 0.0006 |
| 15:30:02 | herramienta_resultado | buscar_tickets: No hay tickets que coincidan. | 0.0006 |

## Paso 2
...
```

### Reglas

1. **Un resumen arriba:** petición, cómo terminó (`fin` o `detenido` con su motivo), número de pasos,
   costo final y cuántas confirmaciones hubo de cada tipo.
2. **Una tabla por paso**, con los eventos en orden. El paso 0 (la petición) va solo en el resumen.
3. **Los rechazos se ven:** una fila de `confirmacion` con `rechazada` va en **negritas**.
4. **Sin romper la tabla:** un detalle con `|` o saltos de línea se escapa o se recorta, y los
   detalles de más de 200 caracteres se recortan con `...`.
5. **Sin datos personales reales:** genera la bitácora de entrega con una petición inventada
   (nombres y correos ficticios). Si tu bitácora tiene un `error_interno`, revisa que su detalle no
   incluya una cadena de conexión antes de entregarla.
6. **Con prueba:** una prueba unitaria que, a partir de un JSONL pequeño escrito en la prueba,
   compruebe el resumen y que una confirmación rechazada aparece en negritas. Mírala fallar
   quitando las negritas antes de darla por buena.

---

## Entrega

- [ ] El comando `bitacora` en tu repositorio, con su prueba en verde.
- [ ] La salida de la prueba fallando sin las negritas y en verde con ellas.
- [ ] El `.md` generado de **dos** corridas inventadas: una que termina creando un ticket con tu
      confirmación y otra donde rechazas todo (por ejemplo, "crea tres tickets de prueba").
- [ ] Un párrafo: ¿qué aprendiste del comportamiento del agente leyendo sus bitácoras que no veías en
      la consola?
- [ ] El commit, sin `.env`, sin llaves y sin la carpeta `bitacoras/`.

---

## Criterio de aceptación

- [ ] Alguien que no estuvo en la corrida puede decir, leyendo solo el `.md`, qué pidió la persona,
      qué hizo el agente en cada paso y qué autorizó o rechazó la persona.
- [ ] El resumen coincide con los eventos (pasos, costo final y confirmaciones).
- [ ] Ninguna tabla se rompe al renderizar el Markdown.
- [ ] No hay datos personales reales ni secretos en lo que entregas.
- [ ] `dotnet build` sigue con 0 errores y 0 warnings.

---

## Preguntas frecuentes

**¿Puedo cambiar el formato de la bitácora JSONL?**
Sí, si lo justificas, pero el JSONL es el registro original: el Markdown se genera de él y no al revés.

**¿El comando necesita la API o la base?**
No. Solo lee un archivo y escribe otro. Por eso su prueba corre sin red.

**¿Y si mi bitácora no tiene un paso con herramientas?**
Pide algo que obligue al agente a buscar antes de responder. Una corrida de un solo paso no ejercita
la tabla por paso.

---

## Preparación para S05

- `BucleAgentePruebas` en verde y una bitácora de ejemplo generada.
- Tu repositorio con la etapa 4 completa: en la S05 el mismo proyecto suma un servidor MCP y el CLI se vuelve también su cliente.
- Piensa qué datos de tu sistema te gustaría que cualquier asistente pudiera consultar, y cuáles nunca.
