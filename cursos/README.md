# Cursos

Los cursos de [Raptor Dev Services Academy](https://academy.raptorcloud.dev) en Markdown. La plataforma sirve las clases como presentación, el quiz y el avance de cada quien; aquí está el mismo material como texto, versionado y legible en cualquier editor. Es una sola fuente: la plataforma construye sus páginas desde estas carpetas.

| Curso | Formato | Para quién |
|---|---|---|
| [Linux para desarrolladores](linux-for-developers/README.md) | 6 sesiones de 5 h | Quien despliega en una VM y quiere entender cada comando |
| [Kubernetes: del Compose al clúster](kubernetes/README.md) | 6 sesiones de 5 h | Quien ya despliega con Compose y necesita llevar su aplicación a un clúster, o decidir que no hace falta |
| [Aplicaciones con LLM: de la API al agente](llm-applications/README.md) | 6 sesiones de 5 h | Quien tiene que llevar un LLM a un producto, en C# y .NET |
| [Preparación para entrevistas técnicas](technical-interviews/README.md) | 6 módulos de 2 h | Egresados que buscan su primer trabajo o el siguiente |
| [Dev Notes](dev-notes/README.md) | 6 sesiones de 8 h | Quien quiere fundamentos, patrones y arquitectura en .NET, sobre las notas de este repo |
| [Diseño y arquitectura de software](software-architecture/README.md) | 4 módulos de 2 h | Quien diseña software y necesita elegir arquitectura, contratos de API y proceso de equipo |
| [Backend con .NET](dotnet-backend/README.md) | 6 módulos de 2 h | Quien construye el backend de un producto en .NET |
| [Frontend con React](react-frontend/README.md) | 1 módulo de 2 h | Quien construye la interfaz de un producto con React |
| [Git, Docker, CI/CD y nube](devops-essentials/README.md) | 5 módulos de 2 h | Quien lleva su aplicación del repositorio a producción |
| [Seguridad web y OWASP](web-security/README.md) | 1 módulo de 2 h | Quien desarrolla aplicaciones web y debe protegerlas |
| [Vibe coding profesional](ai-assisted-coding/README.md) | 1 módulo de 2 h | Quien programa con un asistente de IA y quiere resultados que resistan una revisión |
| [Ciberseguridad](cybersecurity/README.md) | 6 módulos | Quien empieza en pruebas de seguridad, con autorización y ética |
| [Introducción a la ciberseguridad](cyber-intro/README.md) | 5 unidades | Quien empieza en ciberseguridad, desde cualquier carrera |
| [Riesgos actuales en ciberseguridad](cyber-risks/README.md) | 5 unidades | Quien quiere reconocer y prevenir las amenazas de hoy |
| [Informática forense](digital-forensics/README.md) | 5 unidades | Quien investiga incidentes y necesita evidencia que resista un juicio |
| [Defensa y respuesta a incidentes](blue-team/README.md) | 6 sesiones de 5 h | Quien terminó Ciberseguridad y quiere el otro lado: detectar, responder, conservar evidencia y cazar |
| [Seguridad de la información](information-security/README.md) | 5 unidades | Quien gestiona riesgos, un SGSI o la continuidad del negocio |
| [Legislación nacional e internacional en ciberseguridad](cyber-law/README.md) | 5 unidades | Quien necesita el marco legal de México y del mundo |

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
