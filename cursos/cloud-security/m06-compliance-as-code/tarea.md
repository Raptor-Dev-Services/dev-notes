# Tarea - M06 - Cumplimiento como código y arquitectura

**Seguridad en la nube y Zero Trust**
**Tiempo estimado:** 2 horas

---

## Parte 1 - Lecturas (45 min)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/10-cloud-native.md` | Principios cloud native y su relación con la configuración como código | 20 min |
| Documentación de AWS Config | "AWS Config managed rules" y la regla `s3-bucket-public-read-prohibited` | 25 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *Cloud Native Security Cookbook* (Josh Armitage, 2022) | Cap. 6 - infraestructura como código; cap. 7 - cumplimiento como código (etiquetar, detectar, prevenir y remediar) |
| *Security Architecture for Hybrid Cloud* (Mark Buckwell et al., 2024) | Cap. 7 - responsabilidades compartidas; cap. 9 - patrones y decisiones de arquitectura |
| *Policy as Code* (Jimmy Ray, 2024) | Cap. 1 y caps. 11 a 13 |
| *Security as Code* (BK Sarthas Das y Virginia Chu, 2023) | Caps. 3 a 5 |
| *Practical Cybersecurity Architecture* (Ed Moyle y Diana Kelley, 2020) | Caps. 3 a 6 |

---

## Parte 2 - Ejercicio: tres políticas, tres reglas, un ADR (75 min)

Toma **tres políticas de seguridad reales de tu equipo** (o, si no las hay, de la unidad 1 a 5 de este curso) y entrega, para cada una:

1. **La política** en una frase, en lenguaje humano
2. **El control** técnico que la cumple
3. **La regla** como código: una regla propia de `checkov`, una plantilla de regla de AWS Config o una definición de Azure Policy (las dos últimas pueden quedar como fragmento no ejecutado, rotuladas así, validadas por sintaxis)
4. **La prueba positiva y la negativa:** una plantilla que **debe** fallar y otra que **debe** pasar, con la salida real de cada una
5. **La evidencia** que dejaría la regla y quién la revisa

Después, escribe **un ADR** (`adr-0002.md`) con contexto, decisión, alternativas, consecuencias, dueño y fecha de revisión, para una decisión de arquitectura de tu equipo, indicando qué regla de las tres la vigila.

Finalmente, `analisis.md` (máximo una página):

- Una política que **no** pudiste convertir en regla y por qué
- Qué excepción pedirías primero, con dueño y fecha de caducidad
- Qué riesgo tendría remediar automáticamente tu regla más importante

> Usa solo recursos y datos ficticios o de laboratorio. No ejecutes nada contra una cuenta de nube real.

---

## Criterio de aceptación

- [ ] Cada política tiene política, control, regla, prueba positiva y negativa y evidencia
- [ ] La salida de las pruebas es real y menciona la versión de la herramienta
- [ ] Lo no ejecutado está marcado como fragmento
- [ ] El ADR nombra al menos una alternativa descartada y su motivo
- [ ] `analisis.md` contiene una limitación real, no una frase general

## Evaluación

| Criterio | Peso |
|---|---|
| Reglas correctas, con prueba que falla y prueba que pasa | 45 % |
| ADR claro, con alternativas y regla que lo vigila | 20 % |
| Análisis de límites, excepciones y remediación | 25 % |
| Veracidad: lo no ejecutado está rotulado | 10 % |
