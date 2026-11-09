# Homework - S01 - Fundamentos & Setup

**Dev Notes**
**Tiempo estimado:** 3-4 horas

---

## Parte 1 - Lecturas (2-3 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/01-fundamentos/01-solid.md` | Todo el documento, con el back-template abierto | 45 min |
| `dev-notes/01-fundamentos/02-clean-code.md` | Secciones: Nombres, Funciones, Comentarios | 30 min |
| `dev-notes/01-fundamentos/03-refactoring.md` | Secciones: Code Smells, Refactoring seguro | 20 min |

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Clean Code* (Robert C. Martin) | Ch.2 - Meaningful Names - Ch.3 - Functions |
| *Clean Architecture* (Robert C. Martin) | Ch.7 - SRP - Ch.8 - OCP |

---

## Parte 2 - Ejercicio de código (1 hora)

### Instrucciones

En tu fork del `back-template` (ya renombrado como `reddit-clone`), crea un Pull Request
en GitHub con el título:

```
homework/s01 — SOLID review · [Tu nombre]
```

El PR debe contener **comentarios en el código** (usando la función de review de GitHub)
que identifiquen los siguientes 3 puntos:

---

### Punto 1 - SRP aplicado (señalar en el código)

Encuentra una clase en el template donde el SRP esté correctamente aplicado.

**En tu comentario de PR explica:**
- ¿Cuál es la única responsabilidad de esa clase?
- ¿Qué pasaría si le agrегáramos una segunda responsabilidad?
- ¿Qué actor o stakeholder podría pedir un cambio a esa clase?

**Ejemplo de comentario esperado:**
```
GetExampleUserHandler tiene una sola responsabilidad: orquestar el caso de uso
"obtener un usuario". Solo cambia si cambia la lógica del caso de uso.
Si le agregáramos serialización a JSON, tendría dos razones para cambiar:
lógica de negocio y formato de respuesta.
```

---

### Punto 2 - DIP aplicado (señalar en el código)

Encuentra en el template el punto donde el Dependency Inversion Principle se aplica.

**En tu comentario de PR explica:**
- ¿Dónde está la abstracción (interfaz)?
- ¿Quién la define (Domain)?
- ¿Quién la implementa (Infrastructure)?
- ¿Quién la consume (Application)?
- ¿Dónde se conectan (DI root)?

---

### Punto 3 - Code Smell (proponer fix)

Encuentra un code smell en cualquier archivo del template.
No tiene que ser algo grave - puede ser un nombre mejorable, una función que hace dos cosas,
o una abstracción que no necesita existir.

**En tu comentario de PR:**
1. Nombra el smell (Long Method, Duplicated Code, Feature Envy, etc.)
2. Explica por qué es un problema
3. Propone el refactoring concreto (puede ser pseudocódigo)

---

## Criterio de aceptación

Tu homework se considera completo cuando:

- [ ] El PR existe en GitHub con el título correcto
- [ ] El PR tiene al menos 3 comentarios de review (uno por punto)
- [ ] Cada comentario cita el archivo y línea específica del código
- [ ] Los comentarios explican el **por qué**, no solo el **qué**
- [ ] No hay código nuevo roto (el proyecto sigue compilando)

---

## Preguntas frecuentes

**¿Tengo que modificar el código?**
No necesariamente. Los comentarios de review en GitHub se agregan sobre líneas existentes.
Si decides hacer un refactoring, incluirlo en el mismo PR es un plus.

**¿Qué pasa si no encuentro ningún code smell?**
El template tiene code smells intencionales para que los encuentres. Si no los ves,
releer `dev-notes/01-fundamentos/03-refactoring.md` y buscar específicamente
"Long Method" y "Duplicated Code".

**¿Puedo hacer el homework antes de leer?**
No. Las lecturas son el mapa - sin ellas no sabrás qué buscar.

---

## Preparación para S02

Para la revisión del homework en S02 (), ten listo:
- Tu PR con los comentarios de review
- Una pregunta concreta sobre algo que no quedó claro en S01
- Tu opinión sobre cuál de los 5 principios de SOLID te parece más difícil de aplicar y por qué
