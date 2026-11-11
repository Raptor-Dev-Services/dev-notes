# Tarea - S01 - Por qué IaC, y el estado

**Infraestructura como código**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1.5 a 2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/09-cicd/04-terraform.md` | Completa. Va con `terraform`; tradúzcanla a `tofu` mientras leen | 30 min |
| OpenTofu, "State" y "Sensitive Data in State" (opentofu.org/docs/language/state/) | Qué guarda el estado y por qué se protege | 20 min |
| OpenTofu, "State Locking" (opentofu.org/docs/language/state/locking/) | Completa | 10 min |
| OpenTofu, "Import" (opentofu.org/docs/language/import/) | Bloque `import` y generación de configuración | 20 min |
| OpenTofu, backend `pg` y backend `s3` (opentofu.org/docs/language/settings/backends/) | Argumentos, bloqueo y diferencias | 25 min |

Cada lectura se hace **con la VM abierta**: lo que no corran, no lo habrán leído.

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Terraform: Up and Running* (Yevgeniy Brikman, O'Reilly, 2022) | Cap. 1 - Why Terraform; Cap. 2 - Getting Started with Terraform; Cap. 3 - How to Manage Terraform State |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: importar un recurso que hoy existe solo a mano (1.5 a 2 horas)

Escenario (caso hipotético): tu equipo tiene un servicio con un **volumen de Docker** y una **red de Docker** que alguien creó con comandos sueltos. Nadie sabe con qué opciones. Tu trabajo: pasarlos a código **sin destruirlos**.

### Qué haces

1. Crea a mano, con la CLI de Docker, un volumen y una red con nombres que empiecen con `tarea-s01-` y **ponles una etiqueta** (`--label curso=iac`).
2. Escribe un `main.tf` con el proveedor `kreuzwerker/docker` fijado a una versión (`~> 3.0`) y un bloque `import` para cada uno. Consulta en la documentación del proveedor el formato de `id` de cada tipo de recurso y anótalo.
3. Genera la configuración con `tofu plan -generate-config-out=generado.tf`.
4. **Revisa y limpia** lo generado. Si el proceso falla o propone reemplazar algo, documenta el mensaje exacto y cómo lo resolviste.
5. Aplica y demuestra que el siguiente `plan` dice `No changes`.
6. Provoca un drift (por ejemplo, borra la red a mano), muestra el plan y decide: ¿`apply` o `-refresh-only`? Justifica.
7. Guarda el estado en un backend remoto (el PostgreSQL de la práctica, o el que tu equipo defina) y demuestra con una captura o un texto el error de bloqueo cuando dos `apply` coinciden.
8. Limpia todo con `tofu destroy` y confirma con `docker volume ls` y `docker network ls` que no quedó nada de la tarea.

> No uses subredes privadas ni direcciones reales en tu entrega: si Docker muestra una dirección de red, sustitúyela por una de documentación (por ejemplo `192.0.2.0/24`).

### Qué entregas

Un repositorio (o carpeta) `tarea-s01/` con:

- `main.tf` limpio y `.gitignore` con `*.tfstate*` y `.terraform/`, y `.terraform.lock.hcl` versionado.
- `diario.md`: los comandos que corriste, las salidas relevantes y los mensajes de error **literales**.
- `estado.md`: una página (menos de 250 palabras) que contesta: qué guarda tu estado, dónde vive, quién puede leerlo, qué pasa si se pierde y cómo lo recuperarías.

### Criterio de evaluación

| Criterio | Puntos |
|----------|--------|
| El recurso importado existe y el `plan` final dice `No changes` | 25 |
| La configuración generada fue revisada: hay evidencia de qué se quitó o se cambió y por qué | 20 |
| El bloqueo del estado se mostró con un error real, y se explica qué protege | 20 |
| El drift se leyó en el plan y la decisión (`apply` o `-refresh-only`) está justificada | 15 |
| `estado.md` contesta las cinco preguntas con claridad | 10 |
| Limpieza completa y sin secretos ni direcciones privadas en la entrega | 10 |
| **Total** | **100** |

---

## Parte 3 - Preguntas de reflexión (media hora)

Contesta en `reflexion.md`, de tres a cinco líneas cada una:

1. Un compañero propone guardar `terraform.tfstate` en el repositorio "para que todos tengan lo mismo". ¿Qué le respondes?
2. El pipeline lleva una hora colgado y bloquea el estado. ¿Qué haces antes de forzar el desbloqueo?
3. Importaste una base de datos y el plan dice `must be replaced`. ¿Aplicas? ¿Qué revisas?
4. ¿Cuándo preferirías el backend local y cuándo uno remoto?

---

## Qué debes poder hacer después de la tarea

- [ ] Explicar con tus palabras qué guarda el estado y por qué no se edita a mano.
- [ ] Leer un `plan` y encontrar los reemplazos antes de aplicar.
- [ ] Importar un recurso a código sin destruirlo.
- [ ] Distinguir entre revertir un drift y aceptarlo.
- [ ] Mostrar el bloqueo del estado y explicar por qué `-lock=false` no es una opción para escribir.

---

## Siguiente sesión

### S02 - Módulos y entornos

Traigan su `main.tf` de esta tarea: lo convertiremos en un módulo reutilizable.
