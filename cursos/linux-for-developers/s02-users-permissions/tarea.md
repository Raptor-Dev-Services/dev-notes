# Tarea - S02 - Usuarios, permisos y paquetes

**Linux para desarrolladores**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1 a 1.5 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/03-linux-nginx.md` | Usuarios y permisos | 20 min |
| `dev-notes/10-cloud/03-linux-nginx.md` | Comandos esenciales de navegación (el bloque de permisos: `chmod` y `chown`) | 10 min |
| Las láminas de la sesión 2 | Parte 2 (sudo con criterio) y Parte 3 (permisos, umask y bits especiales) | 40 min |

La nota 03 crea un usuario de servicio con el mismo `useradd` que usamos para `api-lab`. Léela comparando: ¿qué opción usa para la shell y por qué?

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Practical Linux System Administration* (Kenneth Hess, O'Reilly, 2023) | Cap. 2 - Working with Permissions and Privileged Accounts; Cap. 4 - Managing Users; Cap. 6 - Installing and Uninstalling Software |
| *Learning Modern Linux* (Michael Hausenblas, O'Reilly, 2022) | Cap. 4 - Access Control; Cap. 6 - Applications, Package Management, and Containers |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: auditar quién puede usar sudo en tu VM (2 a 2.5 horas)

Quien tiene sudo sin límite es root con otro nombre. La tarea es saber **quién** lo tiene en tu VM, **para qué** y si se puede acotar, y dejarlo por escrito.

### Paso 1 - El inventario

Guarda este script como `~/auditar-sudo.sh`. Solo lee; no cambia nada.

```bash
#!/usr/bin/env bash
# auditar-sudo.sh - Quién puede usar sudo en esta máquina y con qué reglas. Solo lee; no cambia nada.
set -euo pipefail

echo "== $(hostname) - $(date '+%F %T')"
echo
echo "== Reglas activas (sin comentarios ni líneas vacías)"
grep -HvE '^\s*(#|$)' /etc/sudoers /etc/sudoers.d/* 2>/dev/null | grep -v ':Defaults' || true
echo
echo "== Reglas sin contraseña"
grep -HE '^[^#]*NOPASSWD' /etc/sudoers /etc/sudoers.d/* 2>/dev/null || echo "(ninguna)"
echo
echo "== Miembros de los grupos sudo y admin"
getent group sudo admin || true
echo
echo "== Lo que puede cada cuenta con shell de entrada"
awk -F: '$7 !~ /(nologin|false|sync)$/ {print $1}' /etc/passwd | while read -r u; do
  printf -- '--- %s\n' "$u"
  sudo -l -U "$u" | sed -n '/may run/,$p; /not allowed/p'
done
echo
echo "== Últimos usos de sudo (journal)"
journalctl _COMM=sudo --since '-30 days' --no-pager 2>/dev/null | grep 'COMMAND=' | tail -n 15 || true
```

Córrelo con sudo y guarda la salida:

```bash
sudo bash ~/auditar-sudo.sh | tee ~/sudo-antes.txt
```

Así se ve un fragmento en la VM del laboratorio, después de la práctica de la sesión 2:

```
== Reglas sin contraseña
/etc/sudoers.d/90-api-lab:beto ALL=(root) NOPASSWD: /usr/bin/systemctl restart api-lab.service

== Miembros de los grupos sudo y admin
sudo:x:27:ubuntu,ana

== Lo que puede cada cuenta con shell de entrada
--- ana
User ana may run the following commands on vm-lab:
    (ALL : ALL) ALL
--- beto
User beto may run the following commands on vm-lab:
    (root) NOPASSWD: /usr/bin/systemctl restart api-lab.service
--- intruso
User intruso is not allowed to run sudo on vm-lab.
```

Busca en especial:

- El archivo de **cloud-init** en `/etc/sudoers.d/` (en VM de nube suele llamarse `90-cloud-init-users`) y si da `NOPASSWD:ALL`.
- Cuentas con shell de entrada que nadie del equipo reconoce.
- Reglas con comandos sin ruta completa o con comodines (`*`).

### Paso 2 - La justificación

Un archivo `auditoria-sudo.md` con una tabla, una fila por cada cuenta que **puede** usar sudo:

| Cuenta | Quién es | Qué puede hoy | Para qué lo necesita | Decisión |
|--------|----------|---------------|----------------------|----------|
| `ubuntu` | usuario inicial de la imagen | todo, sin contraseña | arranque de la VM | quitar NOPASSWD, pedir contraseña |
| `beto` | integrante del equipo | reiniciar `api-lab` | operar la API en la sesión 4 | se queda así |

La columna **Decisión** solo admite tres respuestas: *se queda así*, *se acota* (y a qué) o *se quita*. Cada una con su porqué en una línea.

### Paso 3 - Un cambio aplicado

Aplica **al menos un** cambio de tu tabla. Reglas para no quedarse fuera de la VM:

1. Antes de tocar nada, abre una **segunda terminal** con sudo y déjala abierta hasta el final.
2. Toda regla nueva se valida con `sudo visudo -cf <archivo>` antes de instalarla con `sudo install -m 0440`.
3. Para editar un archivo existente de `/etc/sudoers.d/`: `sudo visudo -f /etc/sudoers.d/<archivo>`.
4. Después del cambio, entra con una **tercera** terminal nueva y comprueba que sigues teniendo el sudo que necesitas.

Vuelve a correr el inventario:

```bash
sudo bash ~/auditar-sudo.sh | tee ~/sudo-despues.txt
diff ~/sudo-antes.txt ~/sudo-despues.txt
```

### Paso 4 - Verlo fallar

Demuestra el cambio con un intento que **falla**. Por ejemplo, si quitaste el `NOPASSWD`:

```bash
sudo -k                     # olvida la contraseña guardada en caché
sudo -n true; echo "salida: $?"
```

```
sudo: a password is required
salida: 1
```

O, si acotaste una regla, corre como esa cuenta un comando que ya no debe permitirse y muestra el rechazo.

---

## Criterio de aceptación

Tu tarea se considera completa cuando:

- [ ] `sudo-antes.txt` y `sudo-despues.txt` existen y el `diff` muestra tu cambio.
- [ ] `auditoria-sudo.md` tiene una fila por cada cuenta con sudo, con quién es, qué puede, para qué y la decisión.
- [ ] Revisaste `/etc/sudoers.d/` y dijiste qué encontraste del archivo de cloud-init (o que tu VM no lo tiene).
- [ ] Aplicaste al menos un cambio validado con `visudo -cf`.
- [ ] Hay evidencia de un intento que falla después del cambio.
- [ ] Nadie del equipo perdió el acceso a la VM.

---

## Qué se entrega

En el repositorio del equipo, en la carpeta `linux/s02/`:

- `auditar-sudo.sh`, `sudo-antes.txt`, `sudo-despues.txt` y el `diff`.
- `auditoria-sudo.md` con la tabla y la evidencia del paso 4.
- `practica-s02.txt` y el historial de la práctica.

Antes de subir los `.txt`, revisa que no traigan nada que no deba salir de la VM: estos archivos no llevan hashes ni contraseñas, pero sí nombres de cuentas.

---

## Preguntas frecuentes

**¿Puedo quitarle el sudo al usuario `ubuntu`?**
Solo si otra cuenta del equipo ya tiene sudo **probado** en una terminal nueva. Si te quedas sin sudo en una VM de nube, la recuperación depende de la consola del proveedor y lleva tiempo.

**¿Es malo `NOPASSWD`?**
No siempre. Una regla `NOPASSWD` para **un comando exacto** (como la de `beto`) es razonable para automatizar. `NOPASSWD:ALL` en una cuenta con llave SSH equivale a entregar root a quien robe la llave.

**¿Qué hago si encuentro una cuenta que nadie reconoce?**
No la borres todavía. Bloquéala (`sudo usermod -L <cuenta>` y `sudo usermod -s /usr/sbin/nologin <cuenta>`), anótala en la tabla y avisa al instructor.

---

## Preparación para S03

Trae a la sesión 3 un script de Bash que tú o tu equipo usen de verdad (un respaldo, un despliegue, una limpieza). En la tarea de la sesión 3 lo vas a reescribir para que falle en voz alta.
