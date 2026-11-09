# Tarea - S01 - Qué es un LLM y cómo elegirlo

**Aplicaciones con LLM: de la API al agente**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1 a 1.5 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/11-vibe-coding/02-prompts-efectivos.md` | Todo el documento | 25 min |
| `dev-notes/11-vibe-coding/05-llmops.md` | Arquitectura de una aplicación con LLM, Observabilidad de LLMs, Gestión de costos | 30 min |

**Lee con ojo crítico la nota 05.** Usa un modelo de una generación anterior y el parámetro `temperature`, que en la API vigente regresa 400. Anota en una lista cada cosa que hoy ya no aplica: la revisamos al inicio de la sesión 2.

### Recomendadas

| Recurso | Sección |
|---------|---------|
| `dev-notes/11-vibe-coding/07-llm-en-produccion.md` | El costo se mide por token, Elegir modelo con números |

### Libros (por capítulo)

| Libro | Capítulos | Para qué |
|-------|-----------|----------|
| *Building LLM Powered Applications* (Valentina Alto, 2024) | 1 a 3 | Qué es un LLM, cómo se comparan los modelos y cómo se integran en una aplicación |

El libro es de 2024 y su código usa librerías y parámetros que ya cambiaron. Léelo por los conceptos; el código del curso va contra la API vigente.

---

## Parte 2 - Ejercicio: costo mensual de un caso de uso tuyo (2 a 2.5 horas)

### Instrucciones

Elige un caso de uso real de tu trabajo o de un proyecto propio en el que un LLM tenga sentido: clasificar correos, resumir reportes, contestar preguntas sobre un manual, extraer datos de facturas. Escribe en tu repositorio el archivo `costos/costo-mensual.md` con estas cinco secciones.

### 1. El caso en tres líneas

Qué entra, qué sale y quién usa el resultado.

### 2. La medición

Prepara 5 entradas reales (sin datos personales) y una instrucción de sistema para tu caso. Mide con `count_tokens` los tokens de entrada y corre cada entrada contra al menos dos modelos para obtener los tokens de salida reales. Puedes adaptar el comando `comparar` de la práctica: cambia la pregunta y el sistema, no el mecanismo de medición.

| Entrada | Modelo | Tokens de entrada | Tokens de salida | Costo USD |
|---------|--------|-------------------|------------------|-----------|
| 1 | claude-haiku-5-5 | | | |
| 1 | claude-sonnet-5-5 | | | |
| ... | | | | |
| **Promedio** | | | | |

### 3. Los supuestos

Escríbelos explícitos, porque son lo primero que alguien va a cuestionar:

- Llamadas por día en un día normal y en el pico
- Días de operación al mes
- Qué parte del prompt se repite en todas las llamadas (candidata a caché) y cuántos tokens tiene
- Tasa de reintentos esperada (por validación fallida o error)
- Fecha en que tomaste los precios de la página oficial

### 4. La tabla de tres escenarios

Usa la fórmula de la clase. Calcula cada escenario **sin caché** y **con caché** del prefijo estable, en al menos dos modelos:

```
costo_mes = llamadas_por_día x días x costo_por_llamada x (1 + tasa_de_reintentos)
```

| Escenario | Llamadas/día | Modelo | Sin caché USD/mes | Con caché USD/mes |
|-----------|--------------|--------|-------------------|-------------------|
| Bajo | | | | |
| Esperado | | | | |
| Pico | | | | |

Si tu caso manda prompts de más de 100 mil tokens a Haiku 5.5, usa la tarifa de prompt largo. Si una parte del trabajo puede esperar, agrega una columna con el precio por lotes (50 % menos).

### 5. La recomendación

Un párrafo: qué modelo usarías, con qué `effort`, y por qué. La justificación tiene que citar números de tus tablas y decir qué dato falta para estar seguro (normalmente: la calidad medida con más casos).

---

## Entrega

- [ ] `costos/costo-mensual.md` con las cinco secciones
- [ ] `comparacion.md` de la práctica, con la rúbrica calificada a ciegas
- [ ] Un commit con mensaje `docs(costos): estimación mensual de <tu caso>`
- [ ] Ningún dato personal ni ninguna llave en el repositorio

---

## Criterio de aceptación

Tu tarea está completa cuando:

- [ ] Los tokens salen de una medición, no de una regla de dedo
- [ ] Cada supuesto está escrito y es razonable para el caso
- [ ] Hay tres escenarios, con caché y sin caché, en al menos dos modelos
- [ ] La aritmética se puede rehacer a mano a partir de tus tablas
- [ ] La recomendación usa los números y reconoce lo que falta medir

---

## Preguntas frecuentes

**¿Puedo usar un caso inventado?**
Sí, si no tienes uno real, pero que sea concreto: "clasificar los 300 correos diarios de soporte de una escuela" sirve; "un chatbot" no.

**¿Cuánto me va a costar medir?**
Con Haiku 5.5 y Sonnet 5.5, cinco entradas cortas cuestan centavos. La llave del curso tiene tope de gasto.

**¿Y si los dos modelos me dan casi el mismo costo?**
Entonces la decisión la toma la calidad o la latencia. Dilo así en la recomendación.

---

## Preparación para la sesión 2

- Tu tabla de costos, lista para presentarla en dos minutos
- La lista de lo que ya no aplica de la nota 05
- La práctica de hoy compilando sin warnings: la sesión 2 la extiende
