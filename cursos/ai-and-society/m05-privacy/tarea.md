# Tarea - M5 - Auditoría de privacidad de un conjunto de datos

**IA, ética y criterio profesional**
**Tiempo estimado:** 90 minutos

> **Estudio individual.** Guarda tu trabajo y evalúalo tú mismo con la rúbrica del final. Si llevas el curso en grupo, el instructor te indica dónde se entrega.

---

## Parte 1 - Lecturas (30 minutos)

| Recurso | Sección | Tiempo est. |
|---|---|---|
| Material de la clase del módulo 5 | "Quitar el nombre no basta", k-anonimato y las siete preguntas | 15 min |
| *A Citizen's Guide to Artificial Intelligence* (Zerilli et al., 2021) | Cap. 6 (Privacy) | 15 min |

Recomendada: *Hands-On Differential Privacy* (Cowan et al., 2024), cap. 1 y cap. 11, solo los conceptos.

---

## Parte 2 - El entregable

Elige **un conjunto de datos que alguien de tu entorno podría compartir o usar para entrenar una herramienta**: la lista de clientes de un negocio, las calificaciones de un grupo, los registros de un sistema de tu trabajo, un formulario de inscripción. **No uses datos reales de personas:** inventa un ejemplo de diez filas con las columnas que tendría y dilo ("Ejemplo ilustrativo").

Entrega una a dos cuartillas con cinco apartados:

1. **El conjunto:** qué columnas tiene y para qué se recabó.
2. **Datos personales y sensibles:** cuáles columnas son personales y cuáles sensibles, con la definición de la LFPDPPP (art. 2, fr. V y VI).
3. **Cuasi-identificadores:** cuáles combinaciones podrían señalar a una persona y **con qué otra lista** podrían cruzarse (un padrón, una red social, un registro público).
4. **Prueba:** con tus diez filas, calcula el k (tamaño del grupo más pequeño por esas columnas) antes y después de generalizar o suprimir. Muestra la tabla.
5. **Decisión:** responde las siete preguntas de "antes de compartir o entrenar" y concluye si compartirías el conjunto, bajo qué condiciones y quién autorizaría.

Reglas:

- Nada de datos reales de personas, ni tuyos ni ajenos.
- Si usas una herramienta de IA para redactar, lo declaras. Las conclusiones son tuyas.

---

## Criterios de evaluación

| Criterio | Excelente | Bueno | Regular | Deficiente |
|---|---|---|---|---|
| Datos personales y sensibles (2 pts.) | Clasifica cada columna con la definición legal | Clasifica sin citar la definición | Clasificación incompleta | No clasifica |
| Cuasi-identificadores (3 pts.) | Combinaciones y listas externas concretas y creíbles | Combinaciones sin lista externa | Solo menciona "datos que identifican" | No los identifica |
| Prueba de k (3 pts.) | Tabla con k antes y después, bien calculado | Calcula k sin mostrar el procedimiento | Cálculo con errores | No hay prueba |
| Decisión (2 pts.) | Siete preguntas contestadas y conclusión con condiciones | Conclusión sin condiciones | Preguntas a medias | No decide |

Total: 10 puntos. Si sacas menos de 7, rehaz el apartado 4 con la tabla de la práctica como modelo.

---

## Criterio de aceptación

- [ ] El conjunto es inventado y está marcado como "Ejemplo ilustrativo".
- [ ] Cada columna está clasificada como personal, sensible o ninguna.
- [ ] Hay al menos una combinación de cuasi-identificadores y la lista con que se cruzaría.
- [ ] Muestra el k antes y después de generalizar o suprimir.
- [ ] Responde las siete preguntas y decide.
