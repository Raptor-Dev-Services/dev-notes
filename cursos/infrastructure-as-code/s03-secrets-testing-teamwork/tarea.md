# Tarea - S03 - Secretos, pruebas y trabajo en equipo

**Infraestructura como código**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1.5 a 2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/05-secretos-produccion.md` | Completa | 30 min |
| OpenTofu, "Sensitive Data in State" (opentofu.org/docs/language/state/sensitive-data/) | Completa | 10 min |
| OpenTofu, "State and Plan Encryption" (opentofu.org/docs/language/state/encryption/) | Configuración, migración y limitaciones | 25 min |
| OpenTofu, "Ephemerality" (opentofu.org/docs/language/ephemerality/) | Qué es y dónde se puede usar | 15 min |
| OpenTofu, `tofu test` (opentofu.org/docs/cli/commands/test/) | Completa | 20 min |
| `dev-notes/09-cicd/01-checklists.md` | Checklist de seguridad en el pipeline | 15 min |

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Terraform: Up and Running* (Yevgeniy Brikman, O'Reilly, 2022) | Cap. 6 - Managing Secrets with Terraform; Cap. 9 - How to Test Terraform Code; Cap. 10 - How to Use Terraform as a Team |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: la regla de revisión de cambios de infraestructura de tu equipo (1.5 a 2 horas)

Escenario: tu equipo (real o el de práctica) va a aplicar infraestructura con OpenTofu y no tiene una regla para revisar cambios. Escríbela y pruébala.

### Qué haces

1. **La regla, en una página (`REGLA-REVISION.md`).** Usa la plantilla de la sesión: quién revisa, qué debe ver, qué bloquea, quién aplica, qué se archiva y qué pasa en una emergencia. Cada punto con una frase que se pueda comprobar ("revisión de una persona distinta de quien escribió el cambio", no "revisar con cuidado").
2. **Tres cambios simulados.** Con el módulo de la sesión 2, produce tres planes reales (con `tofu plan`) para tres cambios distintos:
   - uno **seguro** (por ejemplo, cambiar una etiqueta);
   - uno que **destruye** (por ejemplo, quitar un servicio);
   - uno que **reemplaza** (por ejemplo, cambiar un atributo que fuerza reemplazo, como se vio en la sesión 1).
   Para cada uno escribe: qué muestra el plan, si tu regla lo aprueba o lo bloquea y por qué.
3. **El workflow de plan por pull request.** Adapta el de la clase a tu repositorio (rutas, entorno, versión de OpenTofu). Valídalo con un analizador de YAML y **fija las acciones por SHA**, con su etiqueta en el comentario, obteniendo los SHA con `gh api repos/<dueño>/<repo>/git/ref/tags/<etiqueta>`. Es un fragmento: no hace falta ejecutarlo en GitHub.
4. **Una prueba `tofu test` para tu módulo**, con al menos tres `run`: uno de plan con `assert`, uno con `expect_failures` y uno con `command = apply` sobre recursos locales. Entrega la salida con las pruebas pasando y otra con una de ellas **fallando a propósito**.
5. **Mapa de secretos.** Una tabla con cada secreto de tu proyecto (real o hipotético): dónde se guarda, quién lo lee, en qué archivos o comandos puede aparecer en claro (estado, plan, JSON, salida, historial) y cómo lo proteges (cifrado, efímero, acceso mínimo, que no pase por IaC).
6. **Cifrado.** Demuestra con el módulo de tu práctica el cifrado del estado: `grep` del secreto antes y después, y el error con la frase equivocada.

> No incluyas estados, planes ni JSON de plan en la entrega, aunque estén cifrados. Las contraseñas son de laboratorio.

### Qué entregas

Una carpeta `tarea-s03/` con:

- `REGLA-REVISION.md`;
- `cambios/` con los tres planes (solo el texto del `plan`, sin archivos de plan) y su decisión;
- `plan.yml` y la salida del analizador;
- `tests/` y las dos salidas de `tofu test`;
- `mapa-de-secretos.md`;
- `diario.md` con comandos y salidas literales.

### Criterio de evaluación

| Criterio | Puntos |
|----------|--------|
| La regla cubre los seis puntos de la plantilla, cada uno comprobable | 25 |
| Los tres cambios simulados tienen un plan real y una decisión justificada | 20 |
| El workflow es YAML válido, con acciones fijadas por SHA y sin subir planes ni JSON como artefacto | 15 |
| Las pruebas incluyen `assert`, `expect_failures` y `apply`, y se muestra una fallando | 15 |
| El mapa de secretos nombra los lugares reales donde aparece cada secreto | 15 |
| El cifrado se demuestra con el antes y el después y con el error de la frase equivocada | 10 |
| **Total** | **100** |

---

## Parte 3 - Preguntas de reflexión (media hora)

Contesta en `reflexion.md`, de tres a cinco líneas cada una:

1. Un compañero dice "la variable es `sensitive`, no hay problema si subimos el estado a Git". ¿Qué le muestras?
2. ¿Por qué un archivo `plan.json` subido como artefacto de CI es un riesgo aunque el estado esté cifrado?
3. Perdiste la frase con la que cifraste el estado. ¿Qué pasa, y qué debiste haber hecho antes de activar el cifrado?
4. Tu regla bloquea toda destrucción. Un día hay que destruir un entorno de pruebas completo. ¿Cómo lo permite tu regla sin volverse un trámite inútil?

---

## Qué debes poder hacer después de la tarea

- [ ] Listar los lugares donde aparece un secreto aunque la variable sea `sensitive`.
- [ ] Cifrar el estado y los planes, y decir qué no cubre el cifrado.
- [ ] Escribir una prueba con `tofu test` y verla fallar.
- [ ] Generar el plan de un pull request y leerlo buscando destrucciones y reemplazos.
- [ ] Defender por escrito una regla de revisión de tu equipo.

---

## Siguiente sesión

### S04 - Ansible: configurar lo que se creó

Lleven la lista de controles de endurecimiento de su VM (sesión 2 de Titulación y sesión 5 de Linux): SSH sin contraseña, firewall y fail2ban. Los convertiremos en un rol.
