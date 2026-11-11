# Tarea - S02 - Módulos y entornos

**Infraestructura como código**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1.5 a 2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/09-cicd/05-terraform-modulos-y-estado.md` | Completa | 40 min |
| OpenTofu, "Refactoring" (opentofu.org/docs/language/modules/develop/refactoring/) | Bloques `moved` | 20 min |
| OpenTofu, "count" y "for_each" (opentofu.org/docs/language/meta-arguments/) | "When to use for_each instead of count" y las reglas de `for_each` | 20 min |
| OpenTofu, "Module Sources" (opentofu.org/docs/language/modules/sources/) | Git, registro y versiones | 15 min |
| OpenTofu, "Workspaces" (opentofu.org/docs/language/state/workspaces/) | Completa, con la advertencia sobre credenciales separadas | 10 min |

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Terraform: Up and Running* (Yevgeniy Brikman, O'Reilly, 2022) | Cap. 4 - How to Create Reusable Infrastructure with Terraform Modules; Cap. 5 - Terraform Tips and Tricks (secciones de ciclos, condicionales y trampas); Cap. 8 - Production-Grade Terraform Code (módulos pequeños, componibles y con versión) |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: su módulo con versión fijada y un ejemplo de uso (1.5 a 2 horas)

Escenario: tu equipo repite una misma pieza de infraestructura en dos entornos. Conviértela en un módulo versionado y úsala desde dos entornos.

Puedes partir del módulo `servicios` de la práctica o, mejor, de **tu** configuración de la sesión 1. Los recursos pueden ser locales (`local_file`, `docker_volume`); no se pide nube.

### Qué haces

1. **Módulo** en un repositorio de Git propio, con `main.tf`, `variables.tf`, `outputs.tf` y un `README.md` con: qué hace, tabla de entradas y salidas, y un ejemplo de uso. Debe tener:
   - al menos **dos** variables con `validation`, con mensajes en oraciones completas;
   - al menos un `local` y una salida construida con una expresión `for`;
   - al menos un recurso con `for_each` y otro opcional con `count = condición ? 1 : 0`.
2. **Versión 1.0.0:** una **etiqueta anotada** de Git (`git tag -a v1.0.0`) y el módulo llamado desde dos carpetas de entorno (`dev` y `staging`) con `source = "git::<ruta-o-url>//<subdirectorio>?ref=v1.0.0"`. Para no depender de un servidor puedes usar una ruta local con `git::file://`.
3. **Diferencias declaradas:** un `terraform.tfvars` por entorno y la salida de `diff` entre ellos, con una frase por cada diferencia: por qué es distinta.
4. **Verlo fallar:** un valor inválido por cada `validation` y el error literal.
5. **Versión 2.0.0 (cambio incompatible):** renombra un recurso o cambia su dirección en el módulo. Primero muestra el plan **sin** `moved` (con lo que destruye) y luego **con** `moved` (plan en cero). Publica `v2.0.0`.
6. **Migración documentada:** actualiza uno de los dos entornos a `?ref=v2.0.0`, muestra el plan (`has moved`, sin destruir) y deja el otro en `v1.0.0`. En el README explica por qué conservas el `moved`.
7. **Proveedores:** `.terraform.lock.hcl` versionado, creado con `tofu providers lock` para al menos `linux_amd64` y otra plataforma.

> No incluyas contraseñas, direcciones privadas ni rutas con tu nombre de usuario en la entrega: usa `<USUARIO>` y direcciones de documentación.

### Qué entregas

Un repositorio (o dos carpetas: `modulo/` y `usos/`) con:

- el módulo con sus dos etiquetas (`git tag -l` en `diario.md`);
- `usos/dev/` y `usos/staging/`, cada uno con su `main.tf`, `terraform.tfvars` y lock file;
- `diario.md`: comandos, planes y errores **literales** de cada paso;
- `migracion.md`: menos de 250 palabras, cómo actualizar de 1.0.0 a 2.0.0 sin destruir.

### Criterio de evaluación

| Criterio | Puntos |
|----------|--------|
| El módulo cumple los requisitos del paso 1 y se ejecuta sin errores | 20 |
| Las versiones están fijadas con etiqueta y el lock file se versiona | 15 |
| Las diferencias entre entornos están en `tfvars` y cada una se explica | 15 |
| Las validaciones rechazan los valores inválidos con un error literal | 10 |
| El cambio incompatible se muestra con y sin `moved`, con planes reales | 25 |
| La migración se demostró en un entorno y el otro conserva la versión 1.0.0 | 10 |
| README claro y entrega sin datos sensibles | 5 |
| **Total** | **100** |

---

## Parte 3 - Preguntas de reflexión (media hora)

Contesta en `reflexion.md`, de tres a cinco líneas cada una:

1. Un compañero usa `count = length(var.nombres)` y `var.nombres[count.index]`. ¿Qué le muestras en un `plan` para convencerlo de usar `for_each`?
2. Tu equipo quiere usar workspaces para dev, staging y producción. ¿Qué le preguntas sobre credenciales y sobre cómo se sabe en qué entorno se está aplicando?
3. Publicaste un módulo con `?ref=main` en todos los entornos. ¿Qué pasa cuando alguien hace un cambio incompatible en `main`?
4. ¿Cuándo **no** conviene crear un módulo todavía?

---

## Qué debes poder hacer después de la tarea

- [ ] Escribir un módulo con entradas validadas, locals y salidas.
- [ ] Llamarlo desde dos entornos con diferencias declaradas.
- [ ] Explicar por qué `for_each` es más seguro que `count` para una lista de nombres.
- [ ] Fijar la versión de un módulo con una etiqueta y los proveedores con el lock file.
- [ ] Refactorizar con `moved` y demostrar con un plan que no se destruye nada.

---

## Siguiente sesión

### S03 - Secretos, pruebas y trabajo en equipo

Lleven el módulo de esta tarea: le agregaremos una prueba con `tofu test` y un plan automático en cada pull request.
