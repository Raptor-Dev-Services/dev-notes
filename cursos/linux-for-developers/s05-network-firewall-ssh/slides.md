<!-- _class: lead -->

# S05 - Red, firewall y SSH

### Linux para desarrolladores

**Rogelio Arriaga - Raptor Dev Services**

<!--
Sesión 5 de 6. Hoy la API de la sesión 4 sale al mundo, pero solo por la puerta que decidimos.
Antes de empezar: cada equipo tiene su VM con api-lab.service corriendo. Si alguien no la tiene, que la levante en el primer bloque con la práctica de la S4.
-->

---

## La idea de hoy

> Lo que escucha en un puerto es superficie de ataque, se haya planeado o no.

- Nadie "abre" un puerto a propósito: lo abre el programa que instalaste
- El firewall decide quién llega; no apaga lo que escucha
- Y hay software que se brinca el firewall sin pedir permiso

<!-- Pregunta de arranque: ¿cuántos puertos tiene abiertos su VM ahora mismo? Que apuesten un número antes de medir. Casi nadie acierta. -->

---

## Agenda del día

| Bloque | Tema |
|---|---|
| 1 | Interfaces, rutas y DNS |
| 2 | Qué escucha y dónde |
| 3 | SSH con llaves |
| 4 | Firewall con UFW |
| 5 | Nginx como proxy inverso |
| 6 | Docker se salta el firewall |
| 7 | Práctica: publicar la API y cerrar lo demás |
| 8 | Cierre, tarea y adelanto |

---

## De dónde venimos

- **S2:** la API corre con su propio usuario, sin shell
- **S3:** el respaldo avisa cuando falla
- **S4:** `api-lab.service` arranca sola, se limita y deja rastro en el journal

Hoy: la API **sigue** escuchando solo en `127.0.0.1:8080`. Lo que se publica es Nginx, en el 80.

---

## Tres preguntas que no son la misma

| Pregunta | Herramienta | Lo que NO te dice |
|---|---|---|
| ¿Quién escucha? | `ss -ltnp` | Si el firewall lo deja pasar |
| ¿Quién está permitido? | `ufw status` | Si algo se brinca la regla |
| ¿Quién es alcanzable? | `curl` desde OTRA máquina | Por qué |

Solo la tercera mide el camino completo. Hoy usamos las tres y comparamos.

<!-- Esta lámina es la tesis de la sesión. Regresen a ella en el bloque de Docker: ahí las tres preguntas dan respuestas distintas. -->

---

<!-- _class: lead -->

# Parte 1
## Interfaces, rutas y DNS

---

## Interfaces y rutas

```bash
ip -br a          # cada interfaz, su estado y sus direcciones, una por línea
ip route          # por dónde sale cada paquete
```

Ejemplo de una VM (en la suya salen su interfaz y su IP):

```
lo        UNKNOWN   127.0.0.1/8 ::1/128
enp1s0    UP        203.0.113.25/24 fe80::5054:ff:fe12:3456/64

default via 203.0.113.1 dev enp1s0 proto dhcp src 203.0.113.25
203.0.113.0/24 dev enp1s0 proto kernel scope link src 203.0.113.25
```

`default via` es la puerta de salida a todo lo que no es la red local.

Fuente: *Learning Modern Linux* (Hausenblas), cap. 7.

---

## ¿Quién resuelve los nombres?

En Ubuntu 24.04 lo hace **systemd-resolved**:

```bash
ls -l /etc/resolv.conf        # enlace a /run/systemd/resolve/stub-resolv.conf
grep nameserver /etc/resolv.conf
# nameserver 127.0.0.53       <- el "stub" local, no el DNS de verdad
resolvectl status             # los DNS reales, por interfaz
```

Las aplicaciones le preguntan al stub; el stub le pregunta a los servidores que le dio el DHCP o la configuración de red.

Fuente: *systemd for Linux SysAdmins* (Both), cap. 12.

<!-- Error clásico: editar /etc/resolv.conf a mano. Se sobrescribe. El DNS se configura en netplan o en resolved.conf.d. -->

---

## `getent` frente a `dig`

```bash
getent hosts api.ejemplo.test   # pregunta como lo hace TU aplicación (NSS: /etc/hosts y luego DNS)
dig +short api.ejemplo.test     # pregunta directo al DNS, se salta /etc/hosts
resolvectl query example.com    # pregunta al stub y dice de dónde vino la respuesta
```

Si `getent` resuelve y `dig` no, la respuesta viene de `/etc/hosts`.

Si tu aplicación no conecta y `dig` dice que todo bien, prueba con `getent`: esa es la ruta que sigue el programa.

---

## Demostración: el nombre que solo existe aquí

```bash
echo "127.0.0.1 api.ejemplo.test" | sudo tee -a /etc/hosts
getent hosts api.ejemplo.test       # 127.0.0.1  api.ejemplo.test
dig +short api.ejemplo.test         # (vacío: el DNS no lo conoce)
curl -s http://api.ejemplo.test:8080/health   # la API contesta: el programa usa getent
```

Después: quitar la línea de `/etc/hosts`.

<!-- Demo de 5 minutos. El punto: dos herramientas, dos rutas de resolución. En un incidente de "no resuelve", la primera pregunta es cuál de las dos usa el programa. -->

---

<!-- _class: lead -->

# Parte 2
## Qué escucha y dónde

---

## `ss -ltnp`: el inventario

```bash
sudo ss -ltnp     # l: escuchando  t: TCP  n: sin traducir nombres  p: qué proceso
```

```
State  Local Address:Port   Process
LISTEN 127.0.0.53%lo:53      users:(("systemd-resolve",pid=525,fd=15))
LISTEN    127.0.0.54:53      users:(("systemd-resolve",pid=525,fd=17))
LISTEN       0.0.0.0:22      users:(("sshd",pid=1997,fd=3))
LISTEN     127.0.0.1:8080    users:(("python3",pid=183,fd=3))
LISTEN       0.0.0.0:80      users:(("nginx",pid=2277,fd=5))
LISTEN          [::]:22      users:(("sshd",pid=1997,fd=4))
LISTEN          [::]:80      users:(("nginx",pid=2277,fd=6))
```

Sin `sudo`, la columna de procesos sale vacía para lo que no es tuyo.

---

## La dirección local lo dice todo

| Dirección | Quién puede llegar |
|---|---|
| `127.0.0.1:8080` | Solo procesos de esta máquina |
| `127.0.0.53%lo:53` | Solo esta máquina, y solo por la interfaz `lo` |
| `0.0.0.0:80` | Cualquiera que llegue por cualquier interfaz IPv4 |
| `[::]:80` | Lo mismo en IPv6 (y a veces también IPv4) |
| `203.0.113.25:80` | Solo quien llegue a esa IP en particular |

**`0.0.0.0` y `[::]` son superficie de ataque.** `127.0.0.1` no lo es desde fuera.

<!-- Recuerden que en IPv6 el firewall también tiene que tener reglas. UFW las crea en pareja (v6). -->

---

## La misma API, dos decisiones

```bash
# /etc/api-lab/env
API_HOST=127.0.0.1    # solo Nginx, desde la misma máquina, puede hablarle
API_PORT=8080
```

Si alguien cambia a `API_HOST=0.0.0.0`, la API queda expuesta **aunque Nginx exista**.

Por eso el inventario se compara: lo que cambió entre ayer y hoy es lo que hay que explicar.

---

## El 22 lo escucha... systemd

En Ubuntu 24.04, sshd arranca por **activación de socket**:

```bash
systemctl status ssh.socket      # Triggers: ssh.service
sudo ss -ltnp 'sport = :22'      # recién arrancada la VM: users:(("systemd",pid=1,...))
```

systemd escucha el puerto y despierta a sshd cuando llega la primera conexión.

Consecuencia práctica: **cambiar `Port` ya no basta con reiniciar `ssh`**. Lo vemos en el bloque de SSH.

---

## UDP también cuenta

```bash
sudo ss -lunp
```

```
UNCONN  127.0.0.53%lo:53   users:(("systemd-resolve",pid=525,fd=14))
UNCONN     127.0.0.54:53   users:(("systemd-resolve",pid=525,fd=16))
```

En una VM real puede aparecer también el cliente DHCP en el 68 (`systemd-networkd`).

UDP no tiene "conexión": un escaneo rápido de TCP no lo ve. El inventario sí.

---

## Momento de práctica: el inventario de ANTES

```bash
sudo ss -ltnp > ~/puertos-antes.txt
sudo ss -lunp >> ~/puertos-antes.txt
sudo ufw status verbose >> ~/puertos-antes.txt
```

Para cada línea, en una tabla: puerto, dirección, proceso, **por qué existe**.

Si no saben por qué existe un puerto, todavía no lo pueden cerrar ni dejar abierto.

<!-- 10 minutos. Este archivo es la mitad de la tarea. Que no lo pierdan. -->

---

<!-- _class: lead -->

# Parte 3
## SSH con llaves

---

## Por qué llaves y no contraseña

| | Contraseña | Llave |
|---|---|---|
| Qué se puede adivinar | La contraseña, por fuerza bruta | Nada práctico: la llave privada no viaja |
| Qué ve el servidor | La contraseña misma | Una firma que solo tu llave produce |
| Si te la roban | Se usa desde cualquier lado | Se usa, pero puede llevar frase de paso |

La llave privada se queda en tu laptop. Al servidor solo va la pública.

---

## Generar la llave

```bash
ssh-keygen -t ed25519 -C "<USUARIO>@laptop"
# Enter passphrase: (pongan una frase; el agente la recuerda por ustedes)

ls -l ~/.ssh
# -rw-------  id_ed25519        <- privada: solo tú la lees
# -rw-r--r--  id_ed25519.pub    <- pública: esta sí se comparte

eval "$(ssh-agent -s)" && ssh-add ~/.ssh/id_ed25519
```

`ed25519` es el tipo recomendado hoy: llaves cortas, rápidas y sin parámetros que elegir mal.

Fuente: *Practical Linux System Administration* (Hess), caps. 5 y 13.

---

## Llevar la pública al servidor

```bash
ssh-copy-id -i ~/.ssh/id_ed25519.pub <USUARIO>@<IP_DE_SU_VM>
```

Lo que hace por ti, en el servidor:

```bash
install -d -m 700 ~/.ssh
cat id_ed25519.pub >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

`ssh-copy-id` entra con contraseña **una vez**. Si las contraseñas ya están apagadas, la llave se agrega por la consola del proveedor o desde otra sesión que ya entró.

---

## Verlo fallar: los permisos que sshd exige

```bash
sudo chmod 777 /home/<USUARIO>            # "para que no estorben los permisos"
ssh <USUARIO>@<IP_DE_SU_VM>
# Permission denied (publickey).
sudo journalctl -u ssh -n 5 -o cat
# Authentication refused: bad ownership or modes for directory /home/<USUARIO>
sudo chmod 750 /home/<USUARIO>            # y vuelve a entrar
```

Si otro usuario puede escribir en tu home o en `~/.ssh`, podría meter su llave. sshd prefiere negarte la entrada.

<!-- Lo medimos en el laboratorio del curso: con 777 en el home, la llave correcta se rechaza; con 750, entra. El cliente solo dice "Permission denied": la causa está en el journal del servidor. -->

---

## `~/.ssh/config`: deja de teclear

```
Host vm-lab
    HostName <IP_DE_SU_VM>
    User <USUARIO>
    IdentityFile ~/.ssh/id_ed25519
    IdentitiesOnly yes
```

```bash
ssh vm-lab                 # en lugar de ssh -i ... usuario@ip
scp respaldo.tar.gz vm-lab:/tmp/
```

`IdentitiesOnly yes` evita que el cliente pruebe todas tus llaves y el servidor te corte por intentos.

---

## Endurecer sshd: recordatorio

Lo vieron en Titulación, sesión 2. Lo mínimo:

```bash
sudo tee /etc/ssh/sshd_config.d/10-endurecer.conf <<'EOF'
PasswordAuthentication no
KbdInteractiveAuthentication no
PermitRootLogin no
EOF
sudo sshd -t                                   # sintaxis
sudo sshd -T | grep -E '^(passwordauth|permitroot)'   # lo que de verdad aplica
sudo systemctl restart ssh
```

Antes de cerrar la sesión actual: **abran otra terminal y entren**. La que está abierta no prueba nada.

---

## Cambiar el puerto en Ubuntu 24.04

```bash
echo "Port 2222" | sudo tee /etc/ssh/sshd_config.d/20-puerto.conf
sudo systemctl restart ssh         # NO basta: sigue en el 22
sudo systemctl daemon-reload       # el generador lee el Port de sshd_config
sudo systemctl restart ssh.socket  # ahora sí: 0.0.0.0:2222
```

Y el orden importa: **primero** `ufw allow 2222/tcp`, **después** el cambio. Al revés, te quedas fuera.

Cambiar el puerto quita ruido de los logs; no es una defensa. La defensa es la llave.

<!-- Medido: con solo restart ssh, ss sigue mostrando el 22. Con daemon-reload + restart ssh.socket, aparece el 2222. -->

---

<!-- _class: lead -->

# Parte 4
## Firewall con UFW

---

## El modelo de UFW

- Por omisión: **negar lo que entra, permitir lo que sale**
- Cada regla abre un puerto (o un perfil) hacia dentro
- UFW escribe reglas de iptables en la cadena **INPUT**: tráfico dirigido a esta máquina

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
```

Fuente: *Practical Linux System Administration* (Hess), cap. 13. *systemd for Linux SysAdmins* (Both), cap. 10 trata firewalld, el de otras distribuciones: los conceptos de zonas y servicios son los mismos.

---

## El orden que no te deja fuera

```bash
sudo ufw allow OpenSSH        # 1. la puerta por la que estás entrando
sudo ufw enable               # 2. encender (el aviso sale SIEMPRE que estás por SSH)
# 3. abrir OTRA terminal y entrar
sudo ufw status verbose
```

Las conexiones ya establecidas siguen vivas al activar UFW. Por eso la terminal abierta engaña: la prueba es una conexión nueva.

---

## Perfiles de aplicación

```bash
sudo ufw app list
# Available applications:
#   Nginx Full      (80 y 443)
#   Nginx HTTP      (80)
#   Nginx HTTPS     (443)
#   OpenSSH         (22)

sudo ufw allow "Nginx HTTP"
```

Los perfiles los instala cada paquete en `/etc/ufw/applications.d/`. Si mueves SSH al 2222, el perfil OpenSSH ya no te sirve.

---

## Borrar reglas sin dejar la mitad

```bash
sudo ufw status numbered
#  [ 3] 9000/tcp         ALLOW IN  Anywhere
#  [ 6] 9000/tcp (v6)    ALLOW IN  Anywhere (v6)

sudo ufw delete 3                 # borra SOLO la de IPv4: la v6 sigue abierta
sudo ufw delete allow 9000/tcp    # borra las dos
```

Lo medimos: `delete` por número dejó viva la regla de IPv6.

---

## `limit` para SSH

```bash
sudo ufw limit OpenSSH
sudo ufw status
# 22/tcp (OpenSSH)   LIMIT IN   Anywhere
```

Niega a una IP que abre 6 o más conexiones en 30 segundos. Frena la fuerza bruta tonta; no sustituye a las llaves ni a fail2ban.

---

## Probar desde fuera, y leer el resultado

```bash
# Desde su laptop o la VM de otro equipo
curl -s -m 3 -o /dev/null -w "%{http_code}\n" http://<IP_DE_SU_VM>/health ; echo "salida: $?"
```

| Resultado | Qué significa |
|---|---|
| `200`, salida 0 | Llega y contesta |
| `000`, salida 28 | Timeout: algo descarta los paquetes (firewall) |
| `000`, salida 7 | Rechazo inmediato: nadie escucha o un firewall rechaza |

Con UFW en `deny`, un puerto cerrado da **timeout**, no rechazo.

---

<!-- _class: lead -->

# Parte 5
## Nginx como proxy inverso

---

## Por qué un proxy delante

```
Internet --(80/443)--> Nginx --(127.0.0.1:8080)--> api-lab
```

- Una sola puerta pública, con TLS, límites y logs uniformes
- La API no necesita privilegios para el 80 ni saber de certificados
- Si la API se cae, Nginx responde 502 y lo deja en su log

Fuente: `dev-notes/10-cloud/03-linux-nginx.md`, sección "Nginx como reverse proxy".

---

## El sitio `api-lab`

```nginx
upstream api_lab {
    server 127.0.0.1:8080;
}

server {
    listen 80 default_server;
    listen [::]:80 default_server;
    server_name _;

    access_log /var/log/nginx/api-lab.access.log tiempos;

    location ~ /\. { deny all; }          # nada de .env ni .git

    location / {
        proxy_pass         http://api_lab;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

---

## El log con tiempos (el de la sesión 1)

```nginx
# /etc/nginx/conf.d/tiempos.conf
log_format tiempos '$remote_addr - $remote_user [$time_local] "$request" '
                   '$status $body_bytes_sent "$http_referer" "$http_user_agent" $request_time';
```

```
198.51.100.7 - - [09/Nov/2026:03:59:21 +0000] "GET /lento HTTP/1.1" 200 30 "-" "curl/8.5.0" 2.004
```

El último campo es el tiempo de respuesta: la "ruta más lenta" de la S1 sale de aquí.

---

## Probar antes de aplicar

```bash
sudo ln -s /etc/nginx/sites-available/api-lab /etc/nginx/sites-enabled/
sudo rm /etc/nginx/sites-enabled/default
sudo nginx -t
# nginx: the configuration file /etc/nginx/nginx.conf syntax is ok
# nginx: configuration file /etc/nginx/nginx.conf test is successful
sudo systemctl reload nginx
```

`reload` aplica sin cortar conexiones. Con un error de sintaxis, `reload` falla y Nginx sigue sirviendo la configuración anterior; `restart` con el mismo error **lo deja caído**. Lo medimos: tras el `reload` fallido el sitio dio 200, tras el `restart` dio conexión rechazada.

---

## Lo que la API ve detrás del proxy

```
api-lab: 127.0.0.1 "GET /health HTTP/1.1" 200 -
```

Para la API, todas las peticiones vienen de `127.0.0.1`: quien conecta es Nginx.

La IP real del cliente viaja en `X-Forwarded-For`. La aplicación la lee **solo si confía en el proxy** (en ASP.NET Core: `UseForwardedHeaders` con `KnownProxies`).

<!-- Si la API confía en X-Forwarded-For de cualquiera, un cliente puede inventarse la IP que quiera. -->

---

## HTTPS con Certbot, en una lámina

```bash
sudo apt install -y certbot python3-certbot-nginx
sudo ufw allow "Nginx Full"
sudo certbot --nginx -d <SU_DOMINIO>
sudo certbot renew --dry-run
```

- Necesita un **dominio** que apunte a la IP de la VM; con solo la IP no hay certificado
- La renovación la hace un timer de systemd: `systemctl list-timers | grep certbot`
- `Nginx Full` ya incluye el 80: borren la regla `Nginx HTTP`, que sobra. El 80 se queda abierto: lo usan la redirección a HTTPS y la renovación

Detalle completo: `dev-notes/10-cloud/03-linux-nginx.md`, sección "SSL con Certbot (Let's Encrypt)".

---

<!-- _class: lead -->

# Parte 6
## Docker se salta el firewall

---

## El caso

```bash
sudo ufw status            # 22 y 80. Nada más.
docker run -d --name fuga -p 8081:80 nginx:1.27-alpine
sudo ufw status            # sigue diciendo 22 y 80
```

Y desde otra máquina:

```bash
curl -s -m 3 -o /dev/null -w "%{http_code}\n" http://<IP_DE_SU_VM>:8081/
# 200
```

`ss` dice que escucha, `ufw` dice que no está permitido, `curl` dice que llega. Las tres preguntas, tres respuestas.

<!-- Medido en el laboratorio del curso con Docker 29 y UFW activo: el 8081 contestó 200 desde otro contenedor, mientras un python en 0.0.0.0:9000 daba timeout. -->

---

## Por qué pasa

```
Paquete al puerto 8081 de la VM
  |
  PREROUTING (nat) --- regla de Docker: DNAT hacia <IP_DEL_CONTENEDOR>:80
  |
  ¿el destino es esta máquina?  ya no: ahora es el contenedor
  |
  FORWARD --- DOCKER-USER, DOCKER-FORWARD --- ACCEPT
  |
  (INPUT, donde viven las reglas de UFW, nunca se consulta)
```

Docker reescribe el destino **antes** de que el kernel decida si el paquete es para la VM. UFW filtra INPUT; el paquete va por FORWARD.

Fuente: Docker Docs, "Packet filtering and firewalls".

---

## Lo que se ve en iptables

```bash
sudo iptables -t nat -S DOCKER
# -A DOCKER ! -i docker0 -p tcp -m tcp --dport 8081 -j DNAT --to-destination <IP_DEL_CONTENEDOR>:80

sudo iptables -S FORWARD | head -3
# -P FORWARD DROP
# -A FORWARD -j DOCKER-USER
# -A FORWARD -j DOCKER-FORWARD
```

Las cadenas de Docker van **antes** que las de UFW en FORWARD.

---

## Arreglo 1: publicar solo en 127.0.0.1

```bash
docker rm -f fuga
docker run -d --name fuga -p 127.0.0.1:8081:80 nginx:1.27-alpine
```

```yaml
# compose.yaml
services:
  web:
    image: nginx:1.27-alpine
    ports:
      - "127.0.0.1:8081:80"     # Nginx del host le hace proxy si hace falta
```

Desde fuera: timeout. Desde la VM: 200. Es el mismo patrón que la API: escuchar solo donde hace falta.

---

## Arreglo 2: reglas en `DOCKER-USER`

```bash
sudo iptables -I DOCKER-USER -i <INTERFAZ_PUBLICA> -p tcp \
     -m conntrack --ctorigdstport 8081 --ctdir ORIGINAL -j DROP
```

- `DOCKER-USER` es la cadena que Docker reserva para ti y no reescribe
- Se filtra por el puerto **original** (antes del DNAT), por eso `--ctorigdstport`
- Esta regla **no sobrevive un reinicio** por sí sola: hay que persistirla

Para un equipo que empieza: el arreglo 1. Es más difícil de olvidar.

---

## Defensa en capas

- La red interna no es de confianza: `dev-notes/10-cloud/08-zero-trust.md`
- Cada capa asume que la anterior puede fallar:
  1. El servicio escucha solo donde debe (`127.0.0.1`)
  2. El firewall niega por omisión
  3. Se prueba desde fuera, no se supone
- Si una capa se brinca (Docker), las otras siguen en pie

---

<!-- _class: lead -->

# Parte 7
## Práctica: publicar la API y cerrar lo demás

---

## Paso 1: el inventario de antes

Ya lo tienen en `~/puertos-antes.txt`.

Agreguen una prueba desde fuera, de su laptop a su VM:

```bash
for p in 22 80 8080; do
  printf "%s: " $p; curl -s -m 3 -o /dev/null -w "%{http_code}" http://<IP_DE_SU_VM>:$p/health; echo " ($?)"
done
```

Anoten el resultado. Lo comparamos al final.

---

## Paso 2: Nginx delante de la API

```bash
grep API_HOST /etc/api-lab/env 2>/dev/null || echo "por omisión: 127.0.0.1"
sudo cp tiempos.conf /etc/nginx/conf.d/
sudo cp api-lab /etc/nginx/sites-available/
sudo ln -s /etc/nginx/sites-available/api-lab /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl reload nginx
curl -s http://127.0.0.1/health
```

Esperado: `{"estado": "ok", "pid": ...}` por el puerto 80.

---

## Paso 3: UFW con el orden correcto

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw limit OpenSSH
sudo ufw allow "Nginx HTTP"
sudo ufw enable
```

Terminal nueva: `ssh vm-lab`. Si entra, sigan.

---

## Paso 4: probar desde la otra máquina

| Puerto | Esperado | Por qué |
|---|---|---|
| 22 | conecta con llave | regla `limit OpenSSH` |
| 80 `/health` | `200` | Nginx, regla `Nginx HTTP` |
| 80 `/.env` | `403` | `deny` a archivos ocultos |
| 8080 | `000`, salida 28 | la API solo escucha en 127.0.0.1 |

Si el 8080 contesta desde fuera, la API está en `0.0.0.0`. Encuentren por qué antes de seguir.

---

## Paso 5: verlo fallar con Docker

```bash
docker run -d --name fuga -p 8081:80 nginx:1.27-alpine
sudo ufw status | grep 8081 || echo "UFW no sabe nada del 8081"
```

Desde la otra máquina:

```bash
curl -s -m 3 -o /dev/null -w "%{http_code}\n" http://<IP_DE_SU_VM>:8081/
```

Esperado: `200`. Con UFW activo y sin ninguna regla para el 8081.

<!-- Si la VM no tiene Docker: sudo apt install -y docker.io. El que lo instala es parte del aprendizaje: revisen ss antes y después. -->

---

## Paso 6: arreglarlo y volver a probar

```bash
docker rm -f fuga
docker run -d --name fuga -p 127.0.0.1:8081:80 nginx:1.27-alpine
sudo ss -ltn 'sport = :8081'     # 127.0.0.1:8081
```

Desde fuera: `000`, salida 28. Desde la VM: `curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:8081/` da `200`.

Al final: `docker rm -f fuga`.

---

## Paso 7: el inventario de después

```bash
sudo ss -ltnp > ~/puertos-despues.txt
sudo ss -lunp >> ~/puertos-despues.txt
sudo ufw status verbose >> ~/puertos-despues.txt
diff ~/puertos-antes.txt ~/puertos-despues.txt
```

Cada línea del `diff` necesita una explicación. Eso es la tarea.

---

<!-- _class: lead -->

# Parte 8
## Cierre

---

## Errores que vimos hoy

- Confiar en `ss` o en `ufw status` sin probar desde fuera
- Activar UFW antes de permitir SSH
- Cambiar el puerto de SSH sin `daemon-reload` y `ssh.socket`
- `ufw delete N` y olvidar la regla de IPv6
- Publicar un contenedor en `0.0.0.0` "porque así viene el ejemplo"
- Una API en `0.0.0.0` detrás de Nginx: el proxy no tapa nada

---

## Al terminar puedes

Explicar cada puerto abierto de su servidor y probar desde fuera que los demás están cerrados.

- Leer `ss -ltnp` y decir quién puede llegar a cada línea
- Entrar por SSH con llave y saber por qué te rechaza cuando te rechaza
- Encender UFW sin quedarte fuera
- Poner Nginx delante de la API
- Explicar por qué Docker se brinca UFW y cerrarlo

---

## Tarea

**Inventario de puertos antes y después, con el porqué de cada uno.**

- Lecturas: `dev-notes/10-cloud/03-linux-nginx.md` (Nginx, Certbot y UFW) y `dev-notes/10-cloud/08-zero-trust.md`
- Libros: *Learning Modern Linux*, cap. 7; *Practical Linux System Administration*, caps. 5 y 13; *systemd for Linux SysAdmins*, caps. 10 y 12

Detalle y criterio de aceptación en la tarea de la sesión.

---

## Adelanto: sesión 6

**Observar y diagnosticar.**

- La lista de diagnóstico de cinco minutos
- Disco lleno, servicio que no arranca, puerto ocupado
- `fstab` y el disco que no llegó
- Otro equipo les va a romper la VM a escondidas

Traigan la VM con todo lo de hoy funcionando: la vamos a romper.

---

<!-- _class: lead -->

# Gracias
## Quiz de la sesión 5 en la plataforma
