<!-- _class: lead -->

# S02 - Usuarios, permisos y paquetes

### Linux para desarrolladores

**Rogelio Arriaga - Raptor Dev Services**

<!-- Todos entran a su VM antes de empezar. Hoy casi todo se hace con sudo: confirma que cada equipo puede correr `sudo -v`. -->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, repaso de la tarea, objetivos | 26 min |
| Parte 1 | Usuarios y grupos | 52 min |
| Parte 2 | sudo con criterio | 47 min |
| Parte 3 | Permisos, umask y bits especiales | 57 min |
| Parte 4 | Paquetes con apt | 41 min |
| Parte 5 | Práctica: usuario de servicio y carpeta de equipo | 41 min |
| Cierre | Decisiones de mínimo privilegio, tarea y quiz | 36 min |

<!-- Descanso de 10 minutos después de la Parte 2 y otro antes de la práctica. -->

---

## El lema de la sesión

> El mínimo privilegio empieza en el sistema operativo, no en la aplicación.

- Tu API puede validar cada permiso en el código y aun así correr como root.
- Si alguien encuentra un fallo en la API, hereda **todo** lo que puede hacer su usuario.
- Hoy decidimos qué usuario corre cada cosa y lo demostramos con un `Permission denied`.

---

## Repaso: ¿con qué usuario corre?

La pregunta de la tarea:

```bash
ps -eo user,pid,cmd | grep '[n]ginx'
# root          95 nginx: master process /usr/sbin/nginx -g daemon on; master_process on;
# www-data      96 nginx: worker process
# www-data      97 nginx: worker process
```

- El proceso maestro de Nginx es root solo para abrir el puerto 80; los que atienden peticiones son `www-data`.
- El `[n]` en el `grep` evita que el propio `grep` aparezca en la lista.
- ¿Qué encontraron en sus VM? ¿Algo corre como root sin necesitarlo?

<!-- 10 minutos de conversación. Lo típico: una API levantada a mano con el usuario del equipo, o un contenedor. Anótalo en el pizarrón: al final de la sesión volvemos a esa lista. -->

---

## Al terminar puedes

- Leer `/etc/passwd` y `/etc/group` y explicar cada campo.
- Crear un usuario de servicio que no puede entrar al sistema.
- Dar `sudo` para **una** tarea y auditar quién tiene sudo y para qué.
- Calcular permisos en octal y simbólico, y predecir qué deja la `umask`.
- Montar una carpeta compartida por grupo que funciona para todos los del grupo y para nadie más.
- Instalar, quitar, congelar y rastrear paquetes con `apt` y `dpkg`.

---

<!-- _class: lead -->

# Parte 1
## Usuarios y grupos

---

## Todo proceso corre como alguien

```bash
ps -eo user= | sort | uniq -c | sort -rn
#      18 www-data
#      10 root
#       1 messagebus
```

- Linux no decide por el programa: decide por el **usuario** con el que corre.
- Un archivo se puede leer si el usuario del proceso tiene permiso, sin importar qué programa sea.
- Por eso la primera pregunta de seguridad de un servidor es "¿quién corre esto?".

Fuente: Hausenblas, cap. 4.

<!-- Reutilizan la tubería de la sesión 1: ordenar, contar, ordenar. Que lo noten. -->

---

## `/etc/passwd`, campo por campo

```bash
getent passwd ana
# ana:x:1001:1002::/home/ana:/bin/bash
```

| Campo | Valor | Qué es |
|---|---|---|
| 1 | `ana` | nombre |
| 2 | `x` | la contraseña está en `/etc/shadow` |
| 3 | `1001` | UID |
| 4 | `1002` | GID del grupo principal |
| 5 | (vacío) | comentario, nombre completo |
| 6 | `/home/ana` | carpeta personal |
| 7 | `/bin/bash` | shell al entrar |

`getent` consulta igual a `/etc/passwd` que a un directorio LDAP: úsalo en lugar de `cat`.

---

## `/etc/group` y `/etc/shadow`

```bash
getent group equipo
# equipo:x:1001:ana,beto

ls -l /etc/passwd /etc/shadow
# -rw-r--r-- 1 root root   1232 Nov  7 10:02 /etc/passwd
# -rw-r----- 1 root shadow  679 Nov  7 10:02 /etc/shadow
```

- `/etc/passwd` lo lee cualquiera; `/etc/shadow` solo root y el grupo `shadow`.
- En `shadow` viven los hashes de contraseña y sus fechas: **no se copian, no se muestran**.
- Para revisar una cuenta sin ver el hash:

```bash
sudo passwd -S ana
# ana L 2026-11-07 0 99999 7 -1        (L: bloqueada, no tiene contraseña)
```

---

## UID, GID y `id`

```bash
id ana
# uid=1001(ana) gid=1002(ana) groups=1002(ana),1001(equipo)
```

| Rango de UID | Quién |
|---|---|
| 0 | root: el kernel no le revisa permisos |
| 1 a 999 | usuarios de sistema: servicios, sin persona detrás |
| 1000 en adelante | personas |

- Ubuntu crea un **grupo propio** por usuario (`ana` en el grupo `ana`).
- El kernel solo conoce números: si borras un usuario, sus archivos muestran el UID suelto.

Fuente: Hess, cap. 4.

---

## Usuarios de servicio

```bash
sudo useradd --system --no-create-home --shell /usr/sbin/nologin api-lab
getent passwd api-lab
# api-lab:x:999:997::/home/api-lab:/usr/sbin/nologin

sudo su - api-lab
# su: warning: cannot change directory to /home/api-lab: No such file or directory
# This account is currently not available.
```

- `--system`: UID debajo de 1000, sin caducidad de contraseña.
- `--no-create-home`: no hay carpeta personal; el campo 6 apunta a una que no existe.
- `--shell /usr/sbin/nologin`: nadie entra con esta cuenta, ni por SSH ni con `su`.
- La cuenta no tiene contraseña: queda bloqueada.

<!-- Este usuario es el que corre la API de laboratorio en la sesión 4. Lo creamos hoy y no se borra. -->

---

## `nologin` no es una jaula

```bash
sudo -u api-lab bash -c 'id'
# uid=999(api-lab) gid=997(api-lab) groups=997(api-lab)
```

- `nologin` impide **entrar**. No impide que root, o quien tenga sudo, corra cosas como ese usuario.
- Así se prueba un permiso "como la API": `sudo -u api-lab cat /ruta/al/archivo`.
- Lo que protege de verdad es lo que el usuario **no puede** leer ni escribir.

<!-- Pregunta: si alguien con sudo puede ser api-lab cuando quiera, ¿para qué sirve? Respuesta: para que la API, si la comprometen, no pueda ser nadie más. -->

---

## Personas y grupos

```bash
sudo groupadd equipo
sudo useradd -m -s /bin/bash ana        # -m crea /home/ana con lo de /etc/skel
sudo usermod -aG equipo ana             # -a agrega; sin -a REEMPLAZA los grupos
sudo gpasswd -d ana equipo              # la saca del grupo
sudo userdel -r ana                     # borra la cuenta y su carpeta personal
```

- `usermod -G equipo ana` **sin** la `-a` le quita todos sus otros grupos, incluido `sudo`.
- Es la forma más común de quitarse el sudo a uno mismo.

Fuente: Hess, cap. 4.

---

## El grupo nuevo llega en la siguiente sesión

```bash
# en una terminal abierta antes del cambio
id -nG
# intruso

sudo usermod -aG equipo intruso

id -nG
# intruso             (la sesión vieja no se entera)
su - intruso -c 'id -nG'
# intruso equipo      (una sesión nueva sí)
```

- Los grupos se cargan **al iniciar sesión**. Hay que salir y volver a entrar.
- Un servicio también: después de cambiarle grupos, se reinicia.

<!-- Lo medimos en el laboratorio: un proceso que ya corría conservó sus grupos después del usermod. -->

---

## Demostración: el ciclo de vida de una cuenta

```bash
sudo useradd -m -s /bin/bash demo
id demo
sudo passwd -S demo
ls -la /home/demo
sudo usermod -aG equipo demo
getent group equipo
sudo userdel -r demo
id demo
# id: 'demo': no such user
```

<!-- En vivo. Que noten lo que copia /etc/skel (.bashrc, .profile) y que `passwd -S` dice L hasta que se le pone contraseña. -->

---

<!-- _class: lead -->

# Parte 2
## sudo con criterio

---

## root, `su` y `sudo`

| | Qué pide | Qué deja | Rastro |
|---|---|---|---|
| Entrar como root | la contraseña de root | todo, siempre | ninguno por persona |
| `su -` | la contraseña del otro usuario | una shell completa como él | poco |
| `sudo comando` | **tu** contraseña | un comando, según reglas | cada comando en el journal |

- Ubuntu trae root sin contraseña: no se puede entrar como root, solo usar sudo.
- `sudo -i` da una shell de root: úsala poco y sal en cuanto termines.

Fuente: Hess, cap. 2.

---

## Quién puede usar sudo en Ubuntu

```bash
sudo grep -E '^[^#]' /etc/sudoers | grep -v Defaults
# root	ALL=(ALL:ALL) ALL
# %admin ALL=(ALL) ALL
# %sudo	ALL=(ALL:ALL) ALL
# @includedir /etc/sudoers.d

getent group sudo
# sudo:x:27:ubuntu,ana
```

- `%sudo` y `%admin` son grupos: quien esté en ellos puede todo.
- `admin` es un grupo heredado que hoy casi nunca existe, pero la regla sigue ahí.
- Lo que haya en `/etc/sudoers.d/` también cuenta: hay que leerlo.

---

## El regalo de las imágenes de nube

En las VM creadas con **cloud-init** (casi todas las de nube), suele existir:

```bash
sudo ls -l /etc/sudoers.d/
sudo cat /etc/sudoers.d/90-cloud-init-users
```

- Ese archivo suele dar al usuario inicial `NOPASSWD:ALL`: sudo **sin contraseña** para todo.
- Cómodo para el primer arranque; peligroso si nadie lo vuelve a mirar.
- Si te roban la llave SSH de ese usuario, el atacante es root sin escribir nada más.

**Revísalo en tu VM.** Es la primera línea de la tarea.

<!-- No todas las VM lo traen: una instalación desde ISO no lo tiene y pide contraseña. Que cada equipo diga qué encontró. -->

---

## Escribir reglas: `sudoers.d` y `visudo`

```bash
echo 'beto ALL=(root) NOPASSWD /usr/bin/systemctl' > /tmp/roto
sudo visudo -cf /tmp/roto
# /tmp/roto:1:44: syntax error
# beto ALL=(root) NOPASSWD /usr/bin/systemctl
#                                            ^
```

- **Nunca** se edita `/etc/sudoers` directo: un error de sintaxis deja a todos sin sudo.
- Se escribe un archivo aparte, se valida con `visudo -cf` y se instala con permisos `0440`.
- `sudo visudo -f /etc/sudoers.d/archivo` edita y valida al guardar.

---

## Una regla mínima

```bash
cat > /tmp/90-api-lab <<'EOF'
# beto puede reiniciar la API y nada más
beto ALL=(root) NOPASSWD: /usr/bin/systemctl restart api-lab.service
EOF
sudo visudo -cf /tmp/90-api-lab
# /tmp/90-api-lab: parsed OK
sudo install -m 0440 /tmp/90-api-lab /etc/sudoers.d/90-api-lab
```

```bash
sudo -u beto sudo -n systemctl status api-lab
# sudo: a password is required
```

- La regla permite **ese** comando con **esos** argumentos. `status` ya no entra.
- Ruta completa siempre: `systemctl` a secas permitiría un `systemctl` falso en el `PATH`.

<!-- api-lab.service todavía no existe: lo creamos en la sesión 4. La regla queda lista para entonces. -->

---

## Auditar: `sudo -l -U`

```bash
sudo -l -U beto
# User beto may run the following commands on vm-lab:
#     (root) NOPASSWD: /usr/bin/systemctl restart api-lab.service

sudo -l -U ana
# User ana may run the following commands on vm-lab:
#     (ALL : ALL) ALL

sudo -l -U intruso
# User intruso is not allowed to run sudo on vm-lab.
```

- Contesta la pregunta correcta: no "¿en qué grupo está?", sino "¿qué puede hacer?".
- Junta las reglas de los grupos y de `sudoers.d` en una sola respuesta.

---

## Cada `sudo` deja rastro

```bash
sudo journalctl _COMM=sudo --since today | grep COMMAND
# Nov 07 10:11:10 vm-lab sudo[1495]:     ana : PWD=/home/ana ; USER=root ; COMMAND=/usr/bin/systemctl restart nginx
```

- Quién, desde qué carpeta, como quién y qué comando.
- `sudo -i` deja una sola línea (`COMMAND=/bin/bash`) y después nada: otra razón para evitarlo.
- En la sesión 4 vemos el journal a fondo.

---

## Momento de práctica: ¿quién puede qué en tu VM?

En 10 minutos, por equipo:

```bash
getent group sudo admin
sudo ls -l /etc/sudoers.d/
sudo grep -rhE '^[^#]*NOPASSWD' /etc/sudoers /etc/sudoers.d/
for u in $(awk -F: '$3 >= 1000 && $3 < 65534 {print $1}' /etc/passwd); do sudo -l -U "$u"; done
```

- ¿Cuántas personas pueden todo? ¿Alguna sin contraseña?
- ¿Hay usuarios con UID 1000 o más que no reconocen?

<!-- Es el arranque de la tarea. No corrijan nada todavía: primero el inventario, después las decisiones. -->

---

<!-- _class: lead -->

# Parte 3
## Permisos, umask y bits especiales

---

## `rwx` en un archivo

```bash
ls -l notas.txt
# -rw-rw-r-- 1 beto equipo 29 Nov  7 10:02 notas.txt
#  |  |  |
#  |  |  otros: r--
#  |  grupo: rw-
#  dueño: rw-
```

| Letra | En un archivo |
|---|---|
| `r` | leer el contenido |
| `w` | cambiar el contenido |
| `x` | ejecutarlo como programa |

Linux revisa **una sola** de las tres ternas: si eres el dueño, la del dueño; si no, la del grupo; si no, la de otros.

Fuente: Hess, cap. 2.

---

## `rwx` en un directorio: la `x` es atravesar

| Letra | En un directorio |
|---|---|
| `r` | listar los nombres |
| `w` | crear, borrar y renombrar lo que hay dentro |
| `x` | entrar y alcanzar lo que hay dentro |

```bash
chmod 711 /srv/solo-x          # otros: solo x
sudo -u intruso ls /srv/solo-x
# ls: cannot open directory '/srv/solo-x': Permission denied
sudo -u intruso cat /srv/solo-x/conocido.txt
# secreto                      (sin r no lista, pero con x llega si sabe el nombre)
```

- Borrar un archivo depende de la `w` del **directorio**, no del archivo.
- Un archivo legible por todos queda protegido si su carpeta no deja pasar.

---

## Octal y simbólico

| Octal | Simbólico | Típico para |
|---|---|---|
| `644` | `rw-r--r--` | archivos de configuración públicos |
| `640` | `rw-r-----` | configuración con secretos, legible por un grupo |
| `600` | `rw-------` | llaves y `.env` |
| `755` | `rwxr-xr-x` | scripts y carpetas públicas |
| `750` | `rwxr-x---` | carpetas de una aplicación |

```bash
chmod 640 /etc/api-lab/env
chmod u=rwx,g=rx,o= respaldo.sh     # 750
chmod o+r respaldo.sh               # 754
chmod a-x respaldo.sh               # 644
```

El simbólico cambia **solo** lo que nombras; el octal fija los tres de golpe.

---

## Dueño y grupo: `chown`

```bash
sudo chown beto:equipo s.sh     # dueño y grupo
sudo chown :ana s.sh            # solo el grupo
sudo chown -R api-lab:api-lab /var/lib/api-lab
ls -l s.sh
# -rw-r--r-- 1 beto ana 0 Nov  7 10:03 s.sh
```

- Solo root cambia el dueño de un archivo.
- El dueño puede cambiar el grupo, pero solo a un grupo al que él pertenece.
- `-R` es recursivo: revisa la ruta dos veces antes de presionar Enter.

---

## `umask`: lo que se quita al crear

| `umask` | Archivo nuevo | Directorio nuevo |
|---|---|---|
| `022` | `644` | `755` |
| `002` | `664` | `775` |
| `027` | `640` | `750` |
| `077` | `600` | `700` |

- Se parte de `666` para archivos y `777` para directorios, y se **quitan** los bits de la `umask`.
- Ningún archivo nace con `x`: eso siempre es un `chmod` explícito.
- Para un servicio, `027` es un buen punto de partida: el grupo lee, otros nada.

Fuente: Hess, cap. 2.

<!-- Tabla medida en el laboratorio con mkdir y touch para cada umask. -->

---

## ¿Por qué tu usuario tiene `002` y root `022`?

```bash
grep -E '^(UMASK|USERGROUPS_ENAB)' /etc/login.defs
# UMASK		022
# USERGROUPS_ENAB yes

su - ana -c umask
# 0002
umask            # como root
# 0022
```

- `pam_umask` lee la `UMASK` de `/etc/login.defs`.
- Con `USERGROUPS_ENAB yes`, si el usuario tiene su **grupo propio** con su mismo nombre y no es root, los bits del grupo se igualan a los del dueño: `022` pasa a `002`.
- Es seguro porque en ese grupo solo está esa persona. En una carpeta compartida es lo que hace que el grupo pueda escribir.

---

## setgid en un directorio

```bash
sudo mkdir -p /srv/equipo
sudo chown root:equipo /srv/equipo
sudo chmod 2770 /srv/equipo
ls -ld /srv/equipo
# drwxrws--- 2 root equipo 4096 Nov  7 10:02 /srv/equipo
```

- El `2` del principio es **setgid**: lo que se cree dentro hereda el grupo `equipo`, no el grupo propio de quien lo crea.
- Se ve como una `s` en la `x` del grupo.
- Junto con la `umask` `002`, todo archivo nuevo queda `rw` para el grupo.

---

## Sticky bit: cada quien borra lo suyo

```bash
ls -ld /tmp
# drwxrwxrwt 1 root root 4096 Nov  7 10:02 /tmp

sudo chmod +t /srv/equipo
sudo -u ana rm /srv/equipo/notas.txt
# rm: cannot remove '/srv/equipo/notas.txt': Operation not permitted
```

- La `t` al final: en un directorio donde todos escriben, solo el dueño de un archivo (o del directorio) puede borrarlo.
- Así `/tmp` es de todos y nadie borra lo ajeno.
- Para la carpeta del equipo es opcional: depende de si quieren poder limpiar lo de otros.

---

## setuid: el bit que se audita

```bash
find / -xdev -perm -4000 -type f 2>/dev/null
# /usr/bin/passwd
# /usr/bin/su
# /usr/bin/mount
# ...
ls -l /usr/bin/passwd
# -rwsr-xr-x 1 root root ... /usr/bin/passwd
```

- Un programa con **setuid** corre como su dueño, no como quien lo ejecuta.
- `passwd` lo necesita para escribir en `/etc/shadow`.
- Un setuid de root que no venga de un paquete del sistema es una alarma.

<!-- Guarden la salida de este find hoy: en la sesión 6 y en el proyecto final se compara contra ella. -->

---

## ACL: cuando un grupo no alcanza

```bash
sudo setfacl -d -m g:equipo:rwX /srv/equipo     # ACL por defecto para lo que se cree dentro
getfacl -p /srv/equipo
# default:group:equipo:rwx
# ...
sudo -u beto bash -c 'umask 022; echo acl > /srv/equipo/con-acl.txt'
ls -l /srv/equipo/con-acl.txt
# -rw-rw----+ 1 beto equipo 4 Nov  7 10:03 /srv/equipo/con-acl.txt
```

- Una ACL por defecto da `rw` al grupo **aunque** quien crea traiga `umask 022`.
- El `+` al final de los permisos avisa que hay una ACL: `ls -l` ya no cuenta toda la historia.
- `setfacl -b` las quita. Úsalas cuando el modelo dueño-grupo-otros de verdad no alcance.

Fuente: Hausenblas, cap. 4.

---

<!-- _class: lead -->

# Parte 4
## Paquetes con apt

---

## `apt` y `dpkg`: dos niveles

| Herramienta | Nivel | Sabe de |
|---|---|---|
| `apt` | alto | repositorios, dependencias, versiones disponibles |
| `dpkg` | bajo | los paquetes ya instalados y sus archivos |

- `apt` descarga y le pide a `dpkg` que instale.
- Para preguntar "¿qué hay disponible?" se usa `apt`; para "¿qué tengo y de dónde salió este archivo?", `dpkg`.

Fuente: Hess, cap. 6; Hausenblas, cap. 6.

---

## Los de todos los días

```bash
sudo apt update                 # refresca la lista de versiones disponibles (no instala nada)
apt list --upgradable           # qué se puede actualizar
sudo apt upgrade                # actualiza lo instalado
sudo apt install tree           # instala con sus dependencias
sudo apt remove tree            # quita el programa, deja su configuración
sudo apt purge tree             # quita programa y configuración de /etc
sudo apt autoremove             # quita dependencias que ya nadie usa
```

- `update` y `upgrade` no son lo mismo: el primero solo actualiza el catálogo.
- Un paquete quitado con `remove` aparece como `rc` en `dpkg -l`: su configuración sigue ahí.

---

## ¿Qué versión y de dónde?

```bash
apt policy nginx
# nginx:
#   Installed: 1.24.0-2ubuntu7.19
#   Candidate: 1.24.0-2ubuntu7.19
#   Version table:
#  *** 1.24.0-2ubuntu7.19 500
#         500 ... noble-updates/main ... Packages

apt show tree | grep -E '^(Version|Installed-Size|Depends)'
# Version: 2.1.1-2ubuntu3.24.04.2
# Installed-Size: 160 kB
# Depends: libc6 (>= 2.38)
```

- `Installed` es lo que tienes; `Candidate`, lo que instalaría un `upgrade`.
- La tabla dice de qué repositorio sale cada versión.

---

## ¿Qué instaló y de quién es este archivo?

```bash
dpkg -L nginx | head -4         # archivos que instaló el paquete
# /.
# /usr
# /usr/sbin
# /usr/sbin/nginx

dpkg -S /usr/sbin/nginx         # qué paquete instaló este archivo
# nginx: /usr/sbin/nginx

dpkg -l nginx | tail -1
# ii  nginx  1.24.0-2ubuntu7.19  arm64  small, powerful, scalable web/proxy server
```

- `dpkg -S` contesta "¿quién puso esto aquí?". Si no es de ningún paquete, alguien lo copió a mano.
- `ii` es instalado; `rc`, quitado con configuración pendiente.

---

## De dónde salen los paquetes

Ubuntu 24.04 usa el formato **deb822** en `/etc/apt/sources.list.d/ubuntu.sources`:

```bash
grep -E '^(Types|Suites|Components|Signed-By)' /etc/apt/sources.list.d/ubuntu.sources
# Types: deb
# Suites: noble noble-updates noble-backports
# Components: main universe restricted multiverse
# Signed-By: /usr/share/keyrings/ubuntu-archive-keyring.gpg
# Types: deb
# Suites: noble-security
# ...
```

- `Signed-By` dice con qué llave se verifica el repositorio. Sin llave, apt se niega.
- Un repositorio de terceros va en su propio archivo con **su** llave, nunca con una llave global.
- El viejo `/etc/apt/sources.list` queda solo con un comentario.

---

## Congelar una versión

```bash
sudo apt-mark hold nginx
# nginx set on hold.
apt-mark showhold
# nginx
sudo apt-mark unhold nginx
# Canceled hold on nginx.
```

- Útil cuando una actualización rompe algo y necesitas tiempo para probar la nueva versión.
- Un `hold` olvidado deja el paquete **sin parches de seguridad**: anótalo con fecha y motivo.

---

## Actualizaciones automáticas

```bash
cat /etc/apt/apt.conf.d/20auto-upgrades
# APT::Periodic::Update-Package-Lists "1";
# APT::Periodic::Unattended-Upgrade "1";

systemctl list-timers 'apt-daily*'
# ... apt-daily-upgrade.timer   apt-daily-upgrade.service
# ... apt-daily.timer           apt-daily.service
```

- Ubuntu Server trae `unattended-upgrades`: instala solo los parches de seguridad, sin preguntar.
- Lo que instaló queda en `/var/log/unattended-upgrades/`.
- Lo disparan **timers de systemd**: en la sesión 4 hacemos uno propio.

<!-- En el contenedor del laboratorio hubo que instalarlo; en Ubuntu Server viene por defecto. Que lo confirmen en su VM con dpkg -l unattended-upgrades. -->

---

<!-- _class: lead -->

# Parte 5
## Práctica: usuario de servicio y carpeta de equipo

---

## Paso 1: el usuario de la API

```bash
sudo useradd --system --no-create-home --shell /usr/sbin/nologin api-lab
getent passwd api-lab
sudo passwd -S api-lab
sudo su - api-lab
sudo -u api-lab bash -c 'id'
```

- Anoten el mensaje exacto de `su`.
- Anoten el UID: debe estar debajo de 1000.

<!-- 5 minutos. -->

---

## Paso 2: las personas y el grupo

```bash
sudo groupadd equipo
for u in ana beto intruso; do sudo useradd -m -s /bin/bash "$u"; done
sudo usermod -aG equipo ana
sudo usermod -aG equipo beto
id ana; id beto; id intruso
getent group equipo
# equipo:x:1001:ana,beto
```

`intruso` no entra al grupo: es el que vamos a ver fallar.

---

## Paso 3: la carpeta del equipo

```bash
sudo mkdir -p /srv/equipo
sudo chown root:equipo /srv/equipo
sudo chmod 2770 /srv/equipo
stat -c '%a %A %U:%G %n' /srv/equipo
# 2770 drwxrws--- root:equipo /srv/equipo
```

- `2`: setgid, todo lo nuevo hereda el grupo `equipo`.
- `770`: dueño y grupo todo; otros, **nada**, ni siquiera atravesar.

---

## Paso 4: trabajar en equipo

```bash
sudo su - beto -c 'umask; echo "pendiente: revisar respaldos" > /srv/equipo/notas.txt; ls -l /srv/equipo/notas.txt'
# 0002
# -rw-rw-r-- 1 beto equipo 29 Nov  7 10:02 /srv/equipo/notas.txt

sudo su - ana -c 'echo "ana: hecho" >> /srv/equipo/notas.txt && cat /srv/equipo/notas.txt'
# pendiente: revisar respaldos
# ana: hecho
```

El archivo es de `beto`, su grupo es `equipo` por el setgid y es escribible por el grupo por la `umask` `002`.

---

## Verlo fallar 1: `intruso`

```bash
sudo su - intruso -c 'cat /srv/equipo/notas.txt'
# cat: /srv/equipo/notas.txt: Permission denied
sudo su - intruso -c 'ls /srv/equipo'
# ls: cannot open directory '/srv/equipo': Permission denied
sudo su - intruso -c 'cd /srv/equipo'
# -bash: line 1: cd: /srv/equipo: Permission denied
```

- `notas.txt` es `rw-rw-r--`: "otros" **sí** podrían leerlo.
- Lo que detiene a `intruso` es la carpeta: sin `x` para otros, no llega al archivo.

---

## Verlo fallar 2: sin setgid

```bash
sudo chmod g-s /srv/equipo
sudo su - beto -c 'echo borrador > /srv/equipo/borrador.txt; ls -l /srv/equipo/borrador.txt'
# -rw-rw-r-- 1 beto beto 9 Nov  7 10:02 /srv/equipo/borrador.txt
sudo su - ana -c 'echo cambio >> /srv/equipo/borrador.txt'
# -bash: line 1: /srv/equipo/borrador.txt: Permission denied
sudo chmod g+s /srv/equipo
```

- Sin setgid, el archivo nace con el grupo **propio** de `beto` y `ana` queda fuera.
- Regresar el bit no arregla los archivos ya creados: hay que hacer `chgrp equipo` a mano.
- Pregunta: ¿qué pasa si `beto` trae `umask 022`? Pruébenlo.

<!-- Con umask 022 el archivo nace equipo pero rw-r--r--: ana tampoco escribe. La ACL por defecto de la Parte 3 resuelve ese caso. -->

---

## Entrega de la práctica

Un archivo `practica-s02.txt` con:

1. `getent passwd api-lab`, `passwd -S api-lab` y el mensaje de `su - api-lab`.
2. `stat` de `/srv/equipo` y `ls -l` de los archivos creados.
3. Los tres `Permission denied` de `intruso`.
4. El caso sin setgid y su explicación con tus palabras.
5. Qué pasó con `umask 022` y cómo lo resolverías.

<!-- Unos 40 minutos en total. Revisa en voz alta el punto 5 con dos equipos. -->

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Decisiones de mínimo privilegio

| Qué corre | Con qué usuario | Puede escribir en |
|---|---|---|
| Nginx (atiende) | `www-data` | sus logs |
| La API de laboratorio | `api-lab`, sin shell | `/var/lib/api-lab` |
| El respaldo (sesión 3) | root o un usuario propio | `/var/backups/api-lab` |
| Las personas del equipo | su usuario, con sudo acotado | su carpeta y `/srv/equipo` |

Cada fila se **demuestra** con un intento que falla, no con un "debería".

---

## Lo que te llevas

- La pregunta de seguridad es "¿quién corre esto?": `ps -eo user,pid,cmd`.
- Un usuario de servicio no entra al sistema, pero `nologin` no es una jaula.
- `sudo -l -U` contesta qué puede hacer cada quien; `visudo -cf` antes de instalar una regla.
- La `x` de un directorio decide quién llega a lo que hay dentro.
- `umask` quita, setgid hereda el grupo, sticky protege lo ajeno.
- `dpkg -S` dice de qué paquete salió un archivo; lo que no salió de ninguno, alguien lo puso.

---

## Tarea

**Auditar quién puede usar sudo en tu VM y justificar cada caso.**

- Inventario con los comandos de la Parte 2: grupos, `sudoers.d`, `NOPASSWD` y `sudo -l -U` por usuario.
- Para cada usuario con sudo: quién es, para qué lo necesita y si puede ser más acotado.
- Al menos un cambio aplicado, con evidencia de antes y después, sin dejar a nadie fuera.

Detalle completo en la guía de tarea.

---

## Lecturas y libros

**Obligatorias**

- `dev-notes/10-cloud/03-linux-nginx.md`, sección "Usuarios y permisos".

**Recomendadas**

- *Practical Linux System Administration* (Hess, 2023), caps. 2, 4 y 6.
- *Learning Modern Linux* (Hausenblas, 2022), caps. 4 y 6.

---

## Siguiente sesión

### S03 - Bash que falla en voz alta

- Códigos de salida: el contrato de todo comando.
- `set -euo pipefail` y los huecos por donde se cuela un error.
- Un respaldo que **verifica su propio resultado** y no deja pasar un archivo vacío.

> Un script que sale con 0 después de fallar es peor que no tener script.

---

## Quiz: usuarios, permisos y paquetes

- 10 preguntas por intento, sacadas de un banco de 12.
- Individual. Lo que falles, búscalo en la nota `03-linux-nginx` o en esta presentación.

<!-- Últimos 15 minutos. Revisa en voz alta la pregunta que más se falle. -->
