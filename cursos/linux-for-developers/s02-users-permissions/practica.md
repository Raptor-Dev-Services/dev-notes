# Práctica - S02 - Usuarios, permisos y paquetes

**Linux para desarrolladores**
**Duración:** 40 minutos, por equipo, en su VM.

---

## Objetivo

Crear el usuario de servicio que va a correr la API de laboratorio y una carpeta compartida por grupo, y **demostrar** con intentos que fallan que cada uno puede exactamente lo que debe:

- `api-lab` no puede entrar al sistema.
- `ana` y `beto` trabajan juntos en `/srv/equipo`.
- `intruso` recibe `Permission denied` aunque los archivos digan que "otros" pueden leer.

El usuario `api-lab` **no se borra al terminar**: lo usa la sesión 4.

---

## Antes de empezar

```bash
ssh <USUARIO>@<IP_DE_SU_VM>
sudo -v                      # confirma que tu usuario tiene sudo
getent passwd api-lab ana beto intruso
getent group equipo
```

Los dos `getent` no deben imprimir nada: ninguno de esos usuarios ni el grupo existen todavía. Si alguno existe de un intento anterior, avísale al instructor antes de seguir.

Los números de UID y GID de esta guía salieron en el laboratorio; en tu VM pueden ser otros. Lo que importa es el rango: debajo de 1000 para el usuario de servicio, 1000 o más para las personas.

---

## Paso 1 - El usuario de la API

```bash
sudo useradd --system --no-create-home --shell /usr/sbin/nologin api-lab
getent passwd api-lab
sudo passwd -S api-lab
```

**Salida esperada:**

```
api-lab:x:999:997::/home/api-lab:/usr/sbin/nologin
api-lab L 2026-11-07 -1 -1 -1 -1
```

- UID debajo de 1000: es un usuario de sistema.
- El campo 6 dice `/home/api-lab`, pero esa carpeta **no existe** (`--no-create-home`).
- `L` en `passwd -S`: la cuenta está bloqueada, no tiene contraseña.

Intenta entrar como ese usuario:

```bash
sudo su - api-lab
echo "salida: $?"
```

```
su: warning: cannot change directory to /home/api-lab: No such file or directory
This account is currently not available.
salida: 1
```

Ese mensaje lo imprime `/usr/sbin/nologin`: la shell de la cuenta. Ahora corre algo **como** ese usuario, sin entrar:

```bash
sudo -u api-lab bash -c 'id'
```

```
uid=999(api-lab) gid=997(api-lab) groups=997(api-lab)
```

`nologin` impide entrar, no impide que alguien con sudo actúe como `api-lab`. Así vas a probar los permisos de la API en las sesiones siguientes.

---

## Paso 2 - Las personas y el grupo

```bash
sudo groupadd equipo
for u in ana beto intruso; do sudo useradd -m -s /bin/bash "$u"; done
sudo usermod -aG equipo ana
sudo usermod -aG equipo beto
id ana; id beto; id intruso
getent group equipo
```

**Salida esperada:**

```
uid=1001(ana) gid=1002(ana) groups=1002(ana),1001(equipo)
uid=1002(beto) gid=1003(beto) groups=1003(beto),1001(equipo)
uid=1003(intruso) gid=1004(intruso) groups=1004(intruso)
equipo:x:1001:ana,beto
```

Fíjate en dos cosas:

- Cada persona tiene su **grupo propio** (`ana` en el grupo `ana`): es lo que Ubuntu hace por defecto.
- `usermod -aG` lleva la `-a`. Sin ella, `usermod -G equipo ana` le quitaría todos sus demás grupos.

---

## Paso 3 - La carpeta del equipo

```bash
sudo mkdir -p /srv/equipo
sudo chown root:equipo /srv/equipo
sudo chmod 2770 /srv/equipo
stat -c '%a %A %U:%G %n' /srv/equipo
```

**Salida esperada:**

```
2770 drwxrws--- root:equipo /srv/equipo
```

| Parte | Qué hace |
|---|---|
| `2` | setgid: lo que se cree dentro hereda el grupo `equipo` |
| `7` | dueño (root): todo |
| `7` | grupo `equipo`: leer, crear y atravesar |
| `0` | otros: nada, ni siquiera entrar |

---

## Paso 4 - Trabajar en equipo

`beto` crea un archivo y `ana` lo edita. Se usa `su -` para que cada uno tenga una sesión completa, con su `umask` y sus grupos:

```bash
sudo su - beto -c 'umask; echo "pendiente: revisar respaldos" > /srv/equipo/notas.txt; ls -l /srv/equipo/notas.txt'
```

```
0002
-rw-rw-r-- 1 beto equipo 29 Nov  7 10:14 /srv/equipo/notas.txt
```

```bash
sudo su - ana -c 'echo "ana: hecho" >> /srv/equipo/notas.txt && cat /srv/equipo/notas.txt'
```

```
pendiente: revisar respaldos
ana: hecho
```

Funciona por dos razones que se suman:

1. **setgid:** el archivo nació con grupo `equipo` y no `beto`.
2. **umask `002`:** el grupo quedó con `rw`. Ubuntu da `002` a los usuarios con grupo propio (`USERGROUPS_ENAB yes` en `/etc/login.defs`).

---

## Paso 5 - Verlo fallar

### 5.1 `intruso` no llega

```bash
sudo su - intruso -c 'cat /srv/equipo/notas.txt'
sudo su - intruso -c 'ls /srv/equipo'
sudo su - intruso -c 'cd /srv/equipo'
```

```
cat: /srv/equipo/notas.txt: Permission denied
ls: cannot open directory '/srv/equipo': Permission denied
-bash: line 1: cd: /srv/equipo: Permission denied
```

Mira otra vez los permisos de `notas.txt`: `-rw-rw-r--`. Los "otros" **sí** tienen `r`. Lo que detiene a `intruso` es la carpeta: sin `x` para otros, no puede atravesarla para llegar al archivo.

Y la API tampoco entra:

```bash
sudo -u api-lab ls /srv/equipo
```

```
ls: cannot open directory '/srv/equipo': Permission denied
```

### 5.2 Sin setgid, el equipo se rompe

```bash
sudo chmod g-s /srv/equipo
ls -ld /srv/equipo
sudo su - beto -c 'echo borrador > /srv/equipo/borrador.txt; ls -l /srv/equipo/borrador.txt'
sudo su - ana -c 'echo cambio >> /srv/equipo/borrador.txt'
```

```
drwxrwx--- 2 root equipo 4096 Nov  7 10:14 /srv/equipo
-rw-rw-r-- 1 beto beto 9 Nov  7 10:14 /srv/equipo/borrador.txt
-bash: line 1: /srv/equipo/borrador.txt: Permission denied
```

El archivo nació con el grupo propio de `beto`, y `ana` no está en ese grupo. Regresa el bit y arregla el archivo que quedó mal:

```bash
sudo chmod g+s /srv/equipo
sudo chgrp equipo /srv/equipo/borrador.txt
sudo su - ana -c 'echo cambio >> /srv/equipo/borrador.txt'; echo "salida: $?"
```

```
salida: 0
```

Regresar el setgid **no** corrige los archivos ya creados: eso siempre es un `chgrp` a mano.

### 5.3 Con `umask 022`, tampoco

```bash
sudo su - beto -c 'umask 022; echo otro > /srv/equipo/otro.txt; ls -l /srv/equipo/otro.txt'
sudo su - ana -c 'echo cambio >> /srv/equipo/otro.txt'
```

```
-rw-r--r-- 1 beto equipo 5 Nov  7 10:14 /srv/equipo/otro.txt
-bash: line 1: /srv/equipo/otro.txt: Permission denied
```

El grupo es el correcto, pero la `umask` le quitó la `w`. Una ACL por defecto en la carpeta lo resuelve sin depender de la `umask` de cada quien:

```bash
sudo setfacl -d -m g:equipo:rwX /srv/equipo
sudo su - beto -c 'umask 022; echo acl > /srv/equipo/con-acl.txt; ls -l /srv/equipo/con-acl.txt'
sudo su - ana -c 'echo cambio >> /srv/equipo/con-acl.txt'; echo "salida: $?"
```

```
-rw-rw----+ 1 beto equipo 4 Nov  7 10:14 /srv/equipo/con-acl.txt
salida: 0
```

El `+` al final de los permisos avisa que hay una ACL. `getfacl -p /srv/equipo/con-acl.txt` muestra el detalle.

---

## Paso 6 - Paquetes: ¿de dónde salió esto?

Tres preguntas que se contestan con `apt` y `dpkg`:

```bash
dpkg -S /usr/sbin/nologin
apt policy acl
dpkg -L acl | grep bin/
```

**Salida esperada (las versiones pueden variar):**

```
login: /usr/sbin/nologin
acl:
  Installed: 2.3.2-1build1.1
  ...
/usr/bin/chacl
/usr/bin/getfacl
/usr/bin/setfacl
```

Si `setfacl` no existe en tu VM, instala el paquete con `sudo apt install acl` y repite el paso 5.3.

---

## Evidencia que entregas

Un archivo `practica-s02.txt` por equipo con:

1. La salida de `getent passwd api-lab`, `sudo passwd -S api-lab` y el mensaje de `sudo su - api-lab`.
2. `stat -c '%a %A %U:%G %n' /srv/equipo` y `ls -l /srv/equipo` al final.
3. Los tres `Permission denied` de `intruso` y el de `api-lab`.
4. El caso sin setgid (5.2) y el de `umask 022` (5.3), cada uno explicado en dos líneas con tus palabras.
5. La salida de `dpkg -S /usr/sbin/nologin`.

```bash
history | tail -60 > ~/practica-s02-historial.txt
```

---

## Checklist

- [ ] `api-lab` existe, tiene UID debajo de 1000, shell `/usr/sbin/nologin` y no tiene carpeta personal.
- [ ] `sudo su - api-lab` responde `This account is currently not available.`
- [ ] `/srv/equipo` está en `2770`, dueño root, grupo `equipo`.
- [ ] `ana` edita lo que crea `beto`.
- [ ] `intruso` y `api-lab` reciben `Permission denied` en `/srv/equipo`.
- [ ] Viste fallar el equipo sin setgid y con `umask 022`, y sabes por qué en cada caso.
- [ ] El usuario `api-lab` sigue existiendo al terminar (lo usa la sesión 4).
