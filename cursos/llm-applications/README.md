# Aplicaciones con LLM: de la API al agente

Curso de **Raptor Dev Services Academy**. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev/llm-applications/](https://academy.raptorcloud.dev/llm-applications/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio. Los libros se citan por capítulo: cada quien consigue su copia.

**Prerrequisito:** programar en C#, HTTP y JSON. Recomendable: la unidad "Vibe coding profesional" de Dev Notes.
**Objetivo:** construir una aplicación que use un LLM de forma confiable: prompts versionados, salida estructurada, RAG, herramientas, un servidor MCP, evaluación automática y defensas contra prompt injection.
**Formato:** 6 sesiones de 5 horas (30 h), con clase, quiz, práctica, tarea y glosario.
**Código:** C# y .NET 10 con los SDK oficiales (Anthropic y ModelContextProtocol), contra la versión vigente de la API. Los libros de 2023 y 2024 usan APIs anteriores: se leen por los conceptos.
**Para quién:** desarrolladores que ya usan IA para programar y ahora tienen que meterla en un producto.

---

## Sesión 1. Qué es un LLM y cómo elegirlo (5 h)

> Elegir modelo es elegir costo, latencia y calidad. Hay que medir los tres.

**Material:** [clase](s01-llm-fundamentals/slides.md), [práctica](s01-llm-fundamentals/practica.md), [tarea](s01-llm-fundamentals/tarea.md), [glosario](s01-llm-fundamentals/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** tokens, ventana de contexto, temperatura (como concepto: la API vigente ya no la acepta como parámetro), costos y latencia, y cómo comparar modelos y proveedores.
- [ ] **Lectura:** [`11-vibe-coding/02-prompts-efectivos.md`](../../11-vibe-coding/02-prompts-efectivos.md), [`11-vibe-coding/05-llmops.md`](../../11-vibe-coding/05-llmops.md).
- [ ] **Práctica:** primera llamada a la API con conteo de tokens y costo por respuesta. La misma pregunta a dos modelos, comparada con una rúbrica.
- [ ] **Tarea:** una tabla de costo mensual estimado para un caso de uso suyo.
- **Libros:** *Building LLM Powered Applications* (Alto, 2024), caps. 1 a 3.

**Al terminar puedes:** justificar con números qué modelo usar y cuánto va a costar.

---

## Sesión 2. Prompts como código y salida estructurada (5 h)

> Un prompt que no está en el repositorio ni tiene pruebas es una configuración sin control.

**Material:** [clase](s02-prompts-structured-output/slides.md), [práctica](s02-prompts-structured-output/practica.md), [tarea](s02-prompts-structured-output/tarea.md), [glosario](s02-prompts-structured-output/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** prompt de sistema, ejemplos, salida JSON con esquema, validación y reintentos.
- [ ] **Lectura:** [`11-vibe-coding/07-llm-en-produccion.md`](../../11-vibe-coding/07-llm-en-produccion.md).
- [ ] **Práctica:** extraer datos de texto libre a JSON validado. Verlo fallar: entradas vacías, en otro idioma o maliciosas, y que la validación las detenga.
- [ ] **Tarea:** prompts versionados en el repositorio, con un juego de casos de prueba.
- **Libros:** *Building LLM Powered Applications*, caps. 4 y 8. *Unlocking the Secrets of Prompt Engineering* (Mizrahi, 2023), caps. 1 y 5.

**Al terminar puedes:** tratar un prompt como código: versionado, probado y con un contrato de salida.

---

## Sesión 3. RAG: que responda con sus documentos (5 h)

> Un asistente que no sabe decir "no lo sé" inventa con la misma seguridad con que acierta.

**Material:** [clase](s03-rag/slides.md), [práctica](s03-rag/practica.md), [tarea](s03-rag/tarea.md), [glosario](s03-rag/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** ingestión, fragmentos, embeddings, índice vectorial en PostgreSQL con pgvector, recuperación y citas.
- [ ] **Lectura:** [`11-vibe-coding/08-rag.md`](../../11-vibe-coding/08-rag.md). [`05-bases-de-datos/04-postgresql-avanzado.md`](../../05-bases-de-datos/04-postgresql-avanzado.md).
- [ ] **Práctica:** un asistente que responde sobre la documentación del curso y cita la fuente. Verlo fallar: preguntas cuya respuesta no está en los documentos deben recibir "no lo sé".
- [ ] **Tarea:** medir la tasa de aciertos con 20 preguntas y mejorarla.
- **Libros:** *Building Data-Driven Applications with LlamaIndex* (Gheorghiu, 2024), caps. 4 a 7. *Building LLM Powered Applications*, caps. 5 y 7.

**Al terminar puedes:** montar una búsqueda con LLM sobre datos propios y medir si responde bien.

---

## Sesión 4. Herramientas y agentes (5 h)

> Un agente que escribe sin confirmación es un usuario con permisos que nadie revisa.

**Material:** [clase](s04-tools-agents/slides.md), [práctica](s04-tools-agents/practica.md), [tarea](s04-tools-agents/tarea.md), [glosario](s04-tools-agents/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** uso de herramientas, el bucle del agente, límites de pasos y costo, y una persona que confirma antes de cualquier escritura.
- [ ] **Lectura:** [`11-vibe-coding/06-arquitectura-agentica.md`](../../11-vibe-coding/06-arquitectura-agentica.md).
- [ ] **Práctica:** un agente con dos herramientas: consultar la base y crear un ticket. El ticket solo se crea con confirmación.
- [ ] **Tarea:** la bitácora de cada decisión del agente, legible por una persona.
- **Libros:** *Building LLM Powered Applications*, cap. 6. *Building Data-Driven Applications with LlamaIndex*, cap. 8.

**Al terminar puedes:** darle herramientas a un modelo sin perder el control de lo que hace.

---

## Sesión 5. Model Context Protocol (5 h)

> MCP separa al que sabe del dato del que conversa: el mismo servidor sirve a varios clientes.

**Material:** [clase](s05-mcp/slides.md), [práctica](s05-mcp/practica.md), [tarea](s05-mcp/tarea.md), [glosario](s05-mcp/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** qué es MCP, servidores y clientes, recursos, herramientas y elicitation, y por qué sampling quedó en desuso.
- [ ] **Lectura:** [`11-vibe-coding/09-mcp.md`](../../11-vibe-coding/09-mcp.md).
- [ ] **Práctica:** un servidor MCP que expone el catálogo de cursos en modo lectura, probado desde un cliente. Verlo fallar: una llamada sin credencial debe ser rechazada.
- [ ] **Tarea:** agregar una herramienta de escritura con su autorización.
- **Libros:** *Learn Model Context Protocol with TypeScript* (Noring, 2025), caps. 1 a 3, 7 y 9 a 11.

**Al terminar puedes:** exponer datos y acciones de su sistema a cualquier asistente compatible con MCP.

---

## Sesión 6. Seguridad y operación (5 h)

> Cada documento que entra al contexto es una entrada de usuario, aunque lo haya escrito un tercero.

**Material:** [clase](s06-security-operations/slides.md), [práctica](s06-security-operations/practica.md), [tarea](s06-security-operations/tarea.md), [glosario](s06-security-operations/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** OWASP Top 10 para aplicaciones con LLM, prompt injection directa e indirecta, fuga de datos, salida insegura y costo descontrolado. Evaluación y monitoreo.
- [ ] **Lectura:** [`11-vibe-coding/10-seguridad-llm.md`](../../11-vibe-coding/10-seguridad-llm.md).
- [ ] **Práctica:** atacar el asistente de la sesión 3 con un documento envenenado, poner la defensa y ver que el mismo ataque ya no pasa.
- [ ] **Tarea:** la suite de evaluación corriendo en CI, en rojo si baja la calidad.
- **Libros:** *The Developer's Playbook for Large Language Model Security* (Wilson, 2024), caps. 2 a 4, 7 y 8. *Essential Guide to LLMOps* (Doan, 2024), caps. 5 a 7.

**Al terminar puedes:** defender una aplicación con LLM y saber cuándo empeora.

---

## Proyecto final

Un asistente con RAG, una herramienta con confirmación y un servidor MCP, con evaluación automática. Entrega: el informe de tres defensas, cada una vista fallar sin ella.


## Código de referencia

En [`referencia/`](referencia/) está la solución del instructor, completa y compilable con .NET 10: el código de las prácticas sale de ahí. Para comprobarla: `dotnet test` dentro de esa carpeta. Úsenla para comparar después de intentarlo, no antes.

## Libros base

| Libro | Autor | Año | Para qué |
|---|---|---|---|
| Building LLM Powered Applications | Valentina Alto | 2024 | Columna del curso |
| Building Data-Driven Applications with LlamaIndex | Andrei Gheorghiu | 2024 | RAG de punta a punta |
| Learn Model Context Protocol with TypeScript | Christoffer Noring | 2025 | MCP |
| The Developer's Playbook for Large Language Model Security | Steve Wilson | 2024 | Seguridad |
| Essential Guide to LLMOps | Ryan Doan | 2024 | Evaluación y monitoreo |
| Unlocking the Secrets of Prompt Engineering | Gilbert Mizrahi | 2023 | Técnicas de prompt |

---

*Rogelio Arriaga Gonzalez*
