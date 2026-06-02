# 06 — Estimación de Software

La estimación de software es el proceso de predecir cuánto esfuerzo, tiempo o recursos requiere completar un trabajo. Es una de las habilidades más difíciles en ingeniería de software — y también una de las más malentendidas.

> Fuente: *Software Estimation: Demystifying the Black Art* — McConnell, *Agile Estimating and Planning* — Cohn, *The Mythical Man-Month* — Brooks

---

## Por qué la estimación es difícil

### El Cono de Incertidumbre

Al inicio de un proyecto, la incertidumbre es máxima. Solo se reduce con trabajo real:

```
Inicio del proyecto:    estimación real × 4 (puede ser 4× más o 4× menos)
Definición aprobada:    estimación real × 2
Diseño completado:      estimación real × 1.5
Primera iteración:      estimación real × 1.25
Beta del producto:      estimación real × 1.1
```

**Consecuencia:** pedir estimaciones exactas al inicio de un proyecto es pedir precisión donde no puede existir.

### Complejidad inherente vs accidental

```
Complejidad inherente:  la del problema mismo (no reducible)
  Ej: integrar con la API de WhatsApp que cambia cada 6 meses

Complejidad accidental: la que agregamos nosotros (reducible con buen diseño)
  Ej: lógica de negocio en el Controller en lugar de en el Domain
```

La estimación debe asumir que mitigaremos la complejidad accidental, pero no puede eliminar la inherente.

---

## Story Points — Qué son y qué miden

### Definición

Story points miden la **complejidad relativa** de una historia de usuario — no el tiempo absoluto.

```
Factores que influyen en los story points:
  ✓ Complejidad del trabajo (¿cuántos componentes involucra?)
  ✓ Esfuerzo requerido (¿cuántas decisiones hay que tomar?)
  ✓ Riesgo e incertidumbre (¿cuánto no sabemos?)
  ✗ Horas absolutas (NO es lo que miden)
  ✗ El mejor dev vs el dev más lento
```

### Por qué no horas

```
Estimación en horas:
  Dev A estima 4 horas — Dev B estima 8 horas — ¿cuál es correcta?
  Ninguna: depende de experiencia, interrupciones, contexto, herramientas.

Estimación en story points (relativa):
  Todos acuerdan: "esto es más complejo que la historia X (3 pts)
  pero menos complejo que la historia Y (8 pts) → 5 puntos"
  La escala calibrada es del EQUIPO, no del individuo.
```

---

## Escala Fibonacci para Story Points

Fibonacci (1, 2, 3, 5, 8, 13, 21) es la escala más común porque refleja la incertidumbre creciente:

| Puntos | Qué significa | Ejemplo (CRM Messenger) |
|--------|--------------|------------------------|
| 1 | Trivial — pocas líneas, sin decisiones | Cambiar un texto de un label |
| 2 | Simple — un componente, bien entendido | Agregar campo `apellido` a perfil de agente |
| 3 | Pequeño — un par de componentes, alguna lógica | Filtro de conversaciones por estado |
| 5 | Medio — múltiples componentes, integración | Asignación automática de conversación a agente |
| 8 | Complejo — varias capas, incertidumbre media | Integración con API de WhatsApp Cloud |
| 13 | Muy complejo — investigación necesaria | Flujo completo de billing con Stripe |
| 21 | Demasiado grande — debe dividirse en historias más pequeñas | — |

**Regla:** si el equipo no puede converger en la estimación de una historia, es señal de que la historia no está suficientemente definida.

---

## Planning Poker — Mecánica

Planning Poker es una técnica para estimar historias en equipo con votos simultáneos:

### Proceso

```
1. El PO presenta una User Story y responde preguntas del equipo (5 min max)
2. Cada dev elige una carta (1, 2, 3, 5, 8, 13, 21) en secreto
3. Todos revelan sus cartas simultáneamente
4. Si hay convergencia → la estimación es ese número
5. Si hay divergencia → los extremos explican su razonamiento (2 min)
6. Segunda ronda con las mismas cartas → normalmente converge
```

### Por qué votar en secreto

```
❌ Si el dev más senior habla primero: todos anclan en su número
✅ Votos simultáneos: cada dev revela su modelo mental independiente
   → Las divergencias revelan supuestos ocultos, no diferencias de habilidad
```

### Cuando divergen (el valor real de Planning Poker)

```
Dev A estima 3 → "Solo es agregar un campo a la DB"
Dev B estima 13 → "Hay que migrar datos históricos, eso es riesgoso"

Resultado: Dev B descubrió un riesgo que Dev A no había considerado.
La estimación final es 8 (el riesgo existe pero es manejable con cuidado).
Sin Planning Poker, el riesgo habría aparecido en producción.
```

---

## T-Shirt Sizing — Para epics y backlog inicial

Cuando las historias son aún muy grandes para Planning Poker, usar T-Shirt sizing:

| Talla | Story Points equiv. | Criterio |
|-------|---------------------|---------|
| XS | 1-2 | Unas horas de trabajo, sin incertidumbre |
| S | 3-5 | Un par de días, bien entendido |
| M | 8-13 | Una semana, alguna investigación |
| L | 21-34 | Dos semanas, muchos componentes |
| XL | > 34 | Demasiado grande — dividir antes de estimar |

**Cuándo usar T-Shirt sizing:** en el backlog grooming inicial (Quarter Planning), cuando las historias aún no están refinadas.

---

## Velocity y Capacity

### Velocity

Cuántos story points completa el equipo por sprint, en promedio:

```
Sprint 1: 18 puntos completados
Sprint 2: 22 puntos completados
Sprint 3: 20 puntos completados
Sprint 4: 19 puntos completados

Velocity promedio (últimos 3 sprints): (22 + 20 + 19) / 3 = 20.3 ≈ 20 puntos/sprint
```

**Regla:** usar los últimos 3 sprints para calcular velocity, ignorar el primero (calibración).

### Capacity

Disponibilidad real del equipo en el sprint:

```
Equipo de 4 devs, sprint de 2 semanas (10 días hábiles):
  Dev A: disponible 8 días (2 días de PTO)
  Dev B: disponible 10 días
  Dev C: disponible 9 días (1 día de reuniones externas)
  Dev D: disponible 10 días

Capacity total: 37 dev-días × (velocity / días de sprint)
```

**Planificación conservadora:** no usar el 100% de la capacity — dejar 20% para bugs, reuniones e imprevistos.

---

## Errores comunes en estimación

### 1. Estimar en horas trabajo complejo

```
❌ "Esta historia toma 8 horas."
   → Las horas son precisas para trabajo repetitivo, no para trabajo creativo/exploratorio.
   
✅ "Esta historia es un 5 (media complejidad)."
   → El equipo tiene contexto de qué significa un 5 basado en historias anteriores.
```

### 2. El dev más rápido estima por todos

```
❌ El tech lead dice "eso es fácil, 2 puntos" y todos asienten.
   → Se perdió el riesgo que el dev junior habría detectado (él desconoce esa parte del código).
   
✅ Planning Poker con votos simultáneos.
```

### 3. Presión del PO altera los puntos

```
❌ PO: "¿No puede ser 3 en lugar de 8? Necesitamos que entre en este sprint."
   → Los story points no son negociables. El scope sí.
   
✅ "La historia tal como está es un 8. Podemos entregar solo la parte de X (3 puntos)
    y dejar Y para el siguiente sprint."
```

### 4. Sin calibración histórica

```
❌ El equipo estima todo en la primera semana sin haber completado ninguna historia.
✅ Primero completar 3-5 historias pequeñas, luego calibrar la escala.
   → Usar esas historias como "historias de referencia" para calibrar las siguientes.
```

---

## Estimación técnica — Tareas no-feature

Deuda técnica, refactors y migraciones también se estiman:

| Tipo de tarea | Cómo estimar |
|---------------|-------------|
| Refactoring claro (extract method) | Story points como cualquier historia |
| Migración de DB | Spike primero (investigación timeboxed 1-2 días), luego estimar |
| Actualización de dependencias | Estimar por separado por librería (riesgo varía) |
| Setup de CI/CD | Spike + estimación — las primeras veces toman el doble de lo esperado |
| Investigación / PoC | Timeboxed spike (no en story points, sino en días fijos) |

---

## Triangulación — Calibrar estimaciones nuevas

Antes de estimar una historia nueva, buscar historias ya completadas similares:

```
Historia nueva: "Filtro de conversaciones por etiqueta"

Historias de referencia:
  "Filtro por estado (Activo/Cerrado)" → 3 puntos (completada, tardó 1.5 días)
  "Búsqueda por nombre de contacto" → 5 puntos (completada, tardó 2.5 días)

Nueva historia: tiene lógica de multi-select (más compleja que filtro por estado)
               pero menos que búsqueda FTS → estimación: 5 puntos
```

La triangulación convierte las estimaciones de "adivinar" a "comparar con evidencia real del equipo".
