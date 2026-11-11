# Tarea - U1 - Identificar y fijar un equipo de cómputo encendido

**Informática forense**
**Tiempo estimado:** 4 horas

---

## Parte 1 - Lecturas (1.5 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| [`12-seguridad/03-forense/02-criminalistica-tic.md`](../../../12-seguridad/03-forense/02-criminalistica-tic.md) | "Protección del lugar de los hechos", "Fijación del lugar de los hechos" y "Levantamiento y embalaje" | 45 min |
| [`12-seguridad/03-forense/04-tic-investigacion-ciberdelitos.md`](../../../12-seguridad/03-forense/04-tic-investigacion-ciberdelitos.md) | "Del SGSI al equipo encendido" | 15 min |
| [`12-seguridad/03-forense/03-metodologia-investigacion.md`](../../../12-seguridad/03-forense/03-metodologia-investigacion.md) | "El planteamiento del problema" | 30 min |

### Recomendadas

| Recurso | Para qué |
|---------|----------|
| [`12-seguridad/03-forense/15-identificacion-fijacion-preservacion.md`](../../../12-seguridad/03-forense/15-identificacion-fijacion-preservacion.md) | Adelanta el procedimiento completo que se estudia en la unidad 5 |
| RFC 3227, *Guidelines for evidence collection and archiving* | El orden de volatilidad, en su fuente original |

---

## Parte 2 - Ejercicio (2.5 horas)

### Consigna

Aplica los procedimientos criminalísticos de la unidad para **identificar y fijar un equipo de cómputo que encuentras encendido** en el lugar de los hechos, sin alterar la evidencia más de lo indispensable y dejando registro de cada acción.

Trabaja con una computadora propia o de laboratorio que puedas usar sin riesgo (nunca con un equipo real de un caso ni de tu empleo). Imagina que pertenece a una persona investigada por un fraude electrónico y que la autoridad te pidió determinar qué programas y conexiones estaban activos al momento de la intervención.

### 1. El cuadro sinóptico

Elabora un cuadro sinóptico con las cuatro etapas del manejo de indicios digitales y, para cada una, su definición y sus actividades clave:

| Etapa | Qué debe incluir tu cuadro |
|---|---|
| Procesamiento | Detección, preservación y recolección en el lugar de intervención; identificación de fuentes de información; extracción; verificación con hash |
| Traslado | Embalaje, sellos y etiquetas; registro en la cadena de custodia; condiciones de conservación |
| Análisis | Copias forenses bit a bit; herramientas de análisis; informes; verificación de integridad |
| Almacenamiento | Revisión al ingresar a la bodega de indicios; registro; salida temporal o definitiva |

### 2. La fijación del equipo encendido

Antes de tocar nada, escribe en una bitácora (fecha, hora con zona horaria y responsable en cada línea):

1. **Protección.** Qué harías para asegurar el lugar y quién podría entrar.
2. **Fotografía.** Toma la serie completa: vista general, vista media, acercamiento y gran acercamiento del equipo, más una fotografía de la pantalla tal como la encontraste y una de cada conexión (corriente, red, USB, periféricos). Numera las fotos.
3. **Descripción.** Describe el equipo de lo general a lo particular: marca, modelo y número de serie visibles, estado de la pantalla, luces, cables, periféricos y cualquier medio extraíble.
4. **Croquis.** Dibuja la ubicación del equipo, sus periféricos y conexiones, con escala aproximada, norte y posición de la cámara en cada toma.
5. **Lo volátil.** Registra, sin instalar nada en el equipo, la fecha y hora del sistema comparada con una hora de referencia, los procesos en ejecución, las conexiones de red activas y los usuarios con sesión abierta. Usa las herramientas que ya trae el sistema operativo y guarda cada salida en un medio externo.
6. **Integridad.** Calcula el hash SHA-256 de cada archivo que generaste (fotografías y salidas de comandos) y anótalo en la bitácora.
7. **Decisión.** Justifica por escrito qué harías después: ¿obtener la memoria RAM completa?, ¿apagar o no?, ¿desconectar de la red?, ¿qué embalaje usarías?

### 3. Una pregunta que contestas por escrito

El encargado de sistemas te dice: "si quieres, lo apago y te lo llevas, así nadie lo toca". ¿Qué le contestas y en qué principios y artículos te apoyas?

### Formato de la entrega

Un documento (PDF o Markdown) con:

```markdown
## Cuadro sinóptico
(las cuatro etapas, con definición y actividades)

## Bitácora de fijación
(cada acción con fecha, hora, zona horaria y responsable)

## Evidencia fotográfica
(fotografías numeradas, con su hash SHA-256)

## Croquis

## Salidas de lo volátil
(salidas de comandos, con su hash)

## Decisión y respuesta
1. Qué harías después y por qué
2. Respuesta al encargado de sistemas
```

---

## Rúbrica

| Criterio | Excelente (100 %) | Suficiente (70 %) | Insuficiente (40 %) | Peso |
|---|---|---|---|---|
| Cuadro sinóptico | Las cuatro etapas con definición propia y actividades completas | Las cuatro etapas, con actividades incompletas | Faltan etapas o copia definiciones sin entenderlas | 20 % |
| Fijación fotográfica, descripción y croquis | Serie completa y numerada, descripción ordenada, croquis con escala, norte y posiciones de cámara | Falta alguno de los elementos | Fotos sueltas, sin orden ni croquis | 25 % |
| Preservación de lo volátil | Registra hora, procesos, conexiones y sesiones sin instalar nada en el equipo | Registra parte de lo volátil | Apagó el equipo o instaló herramientas en él | 25 % |
| Integridad y bitácora | Cada archivo con su SHA-256 y cada acción con hora, zona y responsable | Hash o bitácora incompletos | Sin hash o sin bitácora | 15 % |
| Justificación | Decisiones y respuesta apoyadas en principios y artículos vigentes | Justificación correcta pero sin fundamento | Sin justificación | 15 % |

---

## Preguntas frecuentes

**¿Puedo usar una máquina virtual?**
Sí, siempre que la trates como si fuera física: fotografía la pantalla del anfitrión, documenta que es virtual y explica qué cambiaría con un equipo real.

**¿Tengo que obtener la memoria RAM completa?**
No es obligatorio en esta tarea. Sí lo es decidir si la obtendrías y por qué; en la unidad 5 se hace con herramientas forenses.

**¿Por qué SHA-256 y no MD5?**
MD5 tiene colisiones conocidas desde hace años. Puedes calcular ambos, pero el que sostiene la integridad es SHA-256.
