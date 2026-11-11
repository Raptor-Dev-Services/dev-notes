# Práctica - M6 - La política de uso de IA de un equipo

**IA, ética y criterio profesional**
**Tiempo estimado:** 45 minutos en clase o 90 minutos por tu cuenta.

> **Estudio individual.** Si la haces por tu cuenta, evalúa tu política con los casos de prueba y el checklist del final. Si llevas el curso en grupo, el instructor te indica cómo se entrega.

---

## Objetivo

Escribir la política de uso de IA de un equipo: qué datos no se comparten, qué se revisa y quién firma. Después la pruebas contra situaciones difíciles, y la rompes a propósito para ver dónde falla.

---

## Antes de empezar

- Haber visto la clase del módulo 6 y los de privacidad (5) y responsabilidad (4).
- Tener a la mano la plantilla de ocho apartados de la clase y la nota `11-vibe-coding/11-etica-y-uso-responsable.md`.
- Una hoja o documento de una a dos cuartillas.

---

## El equipo (Caso hipotético)

> **Caso hipotético.** El Equipo Ejemplo son seis personas que desarrollan un sistema de agenda y expedientes para clínicas pequeñas. Trabajan con código propio y con bases de datos de clientes. Usan herramientas de IA para escribir código, redactar documentación y resumir reuniones. Nadie ha dicho todavía qué se puede hacer y qué no. Hay una persona responsable del producto (Responsable de Producto) y una persona de seguridad (Responsable de Seguridad).

Todo en este caso es inventado: no corresponde a ninguna empresa real.

---

## Paso 1 - Inventario (10 minutos)

Antes de escribir reglas, mira qué hay. Llena la tabla para el Equipo Ejemplo (inventa lo que haga falta, marcándolo como supuesto):

| Tarea | Herramienta de IA que podría usarse | Datos que tocaría | Color (rojo, amarillo, verde) |
|---|---|---|---|
| Escribir código nuevo | | | |
| Depurar un error con datos de un cliente | | | |
| Redactar documentación | | | |
| Resumir una reunión con un cliente | | | |
| Revisar un contrato | | | |

---

## Paso 2 - Escribe la política (20 minutos)

Usa los ocho apartados y escribe **una o dos frases concretas por apartado**:

1. Alcance
2. Herramientas autorizadas (inventa nombres genéricos: "Herramienta A, con contrato de empresa")
3. Datos: rojo, amarillo, verde
4. Revisión humana: qué se revisa, quién y con qué criterio
5. Propiedad intelectual: qué se puede usar, qué se declara
6. Transparencia: qué se dice a quien recibe el trabajo
7. Incidentes: qué se hace y a quién se avisa
8. Responsable y revisión: quién firma y cada cuándo se revisa

Criterios:

- Cada regla se puede **comprobar**: alguien puede mirar un caso y decir si se cumplió.
- Cada apartado nombra un **rol**, no solo "el equipo".
- Cabe en dos cuartillas.

---

## Paso 3 - Pruébala con tres situaciones (10 minutos)

Para cada situación (Casos hipotéticos), contesta: ¿qué dice tu política? ¿Se resuelve sin dudas?

| # | Situación |
|---|---|
| A | Una persona del equipo quiere que el asistente le arregle una consulta que falla en producción. Para probar, pega la consulta con tres registros de pacientes (nombre y diagnóstico). |
| B | Un asistente de código sugiere agregar una librería que nadie del equipo conoce y escribe una función con una cita de una norma que parece inventada. |
| C | Se configura un agente de IA con acceso al repositorio y al servidor de producción para "dejar listos los cambios". |

Anota, para cada una: la regla de tu política que aplica, quién decide y si hace falta agregar algo.

---

## Paso 4 - Verlo fallar: encuentra el caso que tu política deja pasar

Ahora trata de romper tu política. Escribe **una situación** que:

1. Cumple al pie de la letra lo que escribiste, **y**
2. Aun así causa un daño o una violación (de datos, de propiedad intelectual o de exactitud).

Pistas para buscarla:

- Un dato que no parece personal pero con otros sí identifica (módulo 5: cuasi-identificadores).
- Una revisión que existe pero sin tiempo ni autoridad para rechazar (módulo 4).
- Una herramienta autorizada que cambió sus términos y nadie volvió a leerlos.
- Una regla que nombra "el equipo" y no a una persona.

Luego:

1. Reescribe la regla que dejó pasar el caso.
2. Explica por qué la nueva versión sí lo detiene y cómo se comprobaría.

**Lo que debes ver:** toda política tiene una grieta la primera vez. Lo que importa es que la regla se pueda corregir y que alguien responda por hacerlo.

---

## Paso 5 - Defiéndela (5 minutos)

Prepara una respuesta de dos o tres frases para cada pregunta de la persona que firma:

1. ¿Por qué esta política y no prohibir la IA?
2. ¿Qué pasa si alguien la incumple?
3. ¿Qué cambia si mañana se publica una ley nueva?
4. ¿Cómo sabremos que funciona?

---

## Preguntas de cierre

- ¿Qué parte de tu política viene de una ley (por ejemplo, la LFPDPPP o el Reglamento de IA europeo) y cuál viene de tu criterio?
- ¿Qué regla te costó más escribir de forma comprobable?
- ¿A quién le toca revisar la política y cada cuándo?

---

## Checklist de autoevaluación

- [ ] Los ocho apartados están escritos con frases concretas.
- [ ] Los datos rojos, amarillos y verdes tienen ejemplos.
- [ ] Cada apartado nombra un rol.
- [ ] Probaste las tres situaciones y ajustaste lo que no se resolvía.
- [ ] Encontraste una situación que tu política deja pasar y reescribiste la regla.
- [ ] Hay fecha o criterio de revisión.

---

## Referencias

- Reglamento (UE) 2024/1689, arts. 4, 14, 26 y 50, texto consolidado al 27-07-2026.
- Ley Federal de Protección de Datos Personales en Posesión de los Particulares, última reforma DOF 14-11-2025.
- OWASP Top 10 for LLM Applications 2025: LLM02, LLM06 y LLM09.
- Notas del repositorio: `11-vibe-coding/03-ai-workflow.md`, `10-seguridad-llm.md` y `11-etica-y-uso-responsable.md`.
