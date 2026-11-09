# Cursos

Los cursos de [Raptor Dev Services Academy](https://academy.raptorcloud.dev) en Markdown. La plataforma sirve las clases como presentación, el quiz y el avance de cada quien; aquí está el mismo material como texto, versionado y legible en cualquier editor. Es una sola fuente: la plataforma construye sus páginas desde estas carpetas.

| Curso | Formato | Para quién |
|---|---|---|
| [Linux para desarrolladores](linux-for-developers/README.md) | 6 sesiones de 5 h | Quien despliega en una VM y quiere entender cada comando |
| [Aplicaciones con LLM: de la API al agente](llm-applications/README.md) | 6 sesiones de 5 h | Quien tiene que llevar un LLM a un producto, en C# y .NET |
| [Preparación para entrevistas técnicas](technical-interviews/README.md) | 6 módulos de 2 h | Egresados que buscan su primer trabajo o el siguiente |

## Cómo está organizado cada curso

```
cursos/<curso>/
├── README.md          la ruta del curso: objetivo, unidades, lecturas, libros y proyecto final
├── <unidad>/
│   ├── slides.md      la clase (la plataforma la convierte en presentación)
│   ├── practica.md    la práctica, con su paso de "verlo fallar"
│   ├── tarea.md       la tarea (solo en las sesiones)
│   └── glosario.md    los términos de la unidad
└── referencia/        el código del instructor, cuando el curso lo tiene
```

- **Las lecturas son notas de este repositorio:** cada unidad las cita por su ruta.
- **Los quizzes no están aquí:** se resuelven en la plataforma.
- **Los libros se citan por capítulo:** cada quien consigue su copia.

---

*Rogelio Arriaga Gonzalez*
