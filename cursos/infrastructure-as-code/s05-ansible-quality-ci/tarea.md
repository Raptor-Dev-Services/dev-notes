# Tarea - S05 - Calidad y CI para Ansible

**Infraestructura como código**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1 a 1.5 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/09-cicd/06-ansible.md` | Pruebas, lint y CI | 25 min |
| `dev-notes/09-cicd/02-github-actions.md` | Workflows en pull requests y permisos | 25 min |
| Documentación de ansible-lint | Profiles y Using ansible-lint | 20 min |
| Documentación de Molecule | Getting started: roles | 20 min |

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Ansible: Up and Running* (Meijer, Hochstein y Moser, O'Reilly, 2022) | Cap. 8 - Debugging Ansible Playbooks; Cap. 14 - Quality Assurance with Molecule; Cap. 22 - CI/CD and Ansible; Cap. 24 - Best Practices |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: un cambio que el CI detenga, y su corrección (2 a 2.5 horas)

Tu repositorio (el de la práctica) ya tiene el rol `endurecer`, su escenario de Molecule y el workflow `rol-endurecer.yml`. Ahora demuestra que **el CI protege el rol**.

### Qué hacer

1. **Un cambio malo por cada puerta.** Abre tres pull requests (o tres ramas si no usas GitHub, corriendo los mismos comandos del workflow). Cada uno introduce **un** problema distinto:
   1. Uno que **ansible-lint** detenga (por ejemplo, un `command` sin `changed_when` o un nombre en minúscula).
   2. Uno que detenga la prueba de **idempotencia** de Molecule.
   3. Uno que detenga el **verify** de Molecule (un control de seguridad que el rol ya no cumple).
2. **Evidencia de cada falla:** la salida real del CI (o de los comandos), con el nombre de la regla, la tarea o la `assert` que lo detuvo.
3. **La corrección** de cada uno en un commit aparte, y la salida con el CI en verde.
4. **Un cuarto cambio, el tuyo:** agrega al rol **un control nuevo** (por ejemplo, `PermitEmptyPasswords no`, `X11Forwarding no` o un límite de `MaxSessions`) con su `assert` en `verify.yml` y comprueba que **ese** control falla si lo quitas.
5. **Un secreto:** agrega una variable cifrada con `ansible-vault` (con valor ficticio) a un archivo de `group_vars`, úsala en un playbook de ejemplo con `no_log: true` y muestra que sin la clave el playbook falla.

### Reglas del ejercicio

- Versiones fijadas en el workflow (`ansible-core`, `ansible-lint`, `molecule`) y acciones fijadas por SHA con el tag en comentario.
- **No** subas la clave del vault ni un secreto real. La clave va en `.gitignore`.
- Cada excepción a una regla de lint (`# noqa`) lleva un comentario con el motivo. Lo ideal es no tener ninguna.
- Si el CI de tu repo no permite Docker o Molecule, di exactamente qué parte no pudiste correr, y corre en tu VM los mismos comandos.

### Qué entregar

```
evidencia-s05/
├── pr-lint.md             el cambio, la salida del lint (regla y línea) y su corrección
├── pr-idempotencia.md     el cambio, el "Idempotence test failed" con la tarea y su corrección
├── pr-verify.md           el cambio, la assert que falló y su corrección
├── control-nuevo.md       el control, su assert y la prueba de que falla si se quita
├── vault.md               el comando usado (con valor ficticio) y la salida sin la clave
└── workflow-validado.txt  la salida de validar el YAML con un parser
```

---

## Criterios de evaluación

| Criterio | Peso | Qué se revisa |
|---|---|---|
| Tres puertas distintas | 25 % | Cada cambio malo lo detiene **la puerta correcta** (lint, idempotencia, verify), con salida real |
| Correcciones limpias | 15 % | Cada corrección es mínima y deja el CI en verde sin `# noqa` injustificados |
| Control nuevo | 20 % | El control está en el rol, tiene `assert` en `verify.yml` y se demuestra que la prueba falla sin él |
| Workflow | 15 % | Versiones y SHA fijados, `permissions` mínimos, `needs: lint`, YAML validado |
| Secretos | 15 % | Vault bien usado, clave fuera del repo, `no_log` donde toca, sin secretos reales |
| Explicación | 10 % | Cada falla dice **por qué** ocurrió, no solo qué mensaje salió |

---

## Para ir pensando

- Tu CI prueba en un contenedor sin systemd. ¿Qué podría fallar en la VM real que el CI no ve?
- ¿Qué control de tu checklist de Titulación no sabrías probar con `verify.yml`? ¿Cómo lo probarías?
- El lint, la idempotencia y el verify detienen **errores de código**. ¿Quién detiene un rol correcto que configura algo **inseguro** en infraestructura nueva? La próxima sesión.
