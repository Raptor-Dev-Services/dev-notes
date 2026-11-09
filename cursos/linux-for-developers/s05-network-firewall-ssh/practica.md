# Práctica - S05 - Publicar la API detrás de Nginx y cerrar lo demás

**Linux para desarrolladores**
**Duración estimada:** 2 horas

---

## Objetivo

Dejar la API de laboratorio publicada **solo** a través de Nginx en el puerto 80, con SSH por llave, UFW negando todo lo demás, y demostrarlo **desde otra máquina**. Después, ver cómo un contenedor de Docker se brinca el firewall y cerrarlo.

La regla del curso: si no lo vieron fallar, no lo probaron. Aquí lo que falla es la suposición de que "UFW está activo, entonces está cerrado".

---

## Antes de empezar

- Su VM de equipo (Ubuntu Server 24.04) con `api-lab.service` de la sesión 4 corriendo:

```bash
systemctl is-active api-lab
curl -s http://127.0.0.1:8080/health
# active
# {"estado": "ok", "pid": 183}
```

- Una **segunda máquina** para probar desde fuera: su laptop o la VM de otro equipo. Todo lo que diga "desde fuera" se corre ahí, nunca en la VM misma.
- Una terminal **extra** abierta en la VM durante toda la práctica. Es su salvavidas si algo de SSH o UFW sale mal.

---

## Paso 1 - El inventario de antes

En la VM:

```bash
sudo ss -ltnp  > ~/puertos-antes.txt
sudo ss -lunp >> ~/puertos-antes.txt
sudo ufw status verbose >> ~/puertos-antes.txt
cat ~/puertos-antes.txt
```

Desde fuera:

```bash
for p in 22 80 8080; do
  printf "puerto %s: " "$p"
  curl -s -m 3 -o /dev/null -w "%{http_code}" "http://<IP_DE_SU_VM>:$p/health"; echo " (curl salió $?)"
done
```

Anoten las dos salidas en su bitácora. Para cada línea de `ss`, escriban qué proceso es y por qué existe. Si alguna no la saben explicar, investíguenla antes de seguir (`systemctl status <PID>` les dice a qué unit pertenece un proceso).

---

## Paso 2 - SSH con llave

En **su laptop**:

```bash
ssh-keygen -t ed25519 -C "<USUARIO>@laptop"
ssh-copy-id -i ~/.ssh/id_ed25519.pub <USUARIO>@<IP_DE_SU_VM>
```

Agreguen el alias en `~/.ssh/config` de la laptop:

```
Host vm-lab
    HostName <IP_DE_SU_VM>
    User <USUARIO>
    IdentityFile ~/.ssh/id_ed25519
    IdentitiesOnly yes
```

```bash
chmod 600 ~/.ssh/config
ssh vm-lab 'echo "entré como $(id -un) a $(hostname)"'
```

Salida esperada:

```
entré como <USUARIO> a vm-lab
```

Si en Titulación todavía no apagaron las contraseñas, háganlo ahora, en la VM:

```bash
sudo tee /etc/ssh/sshd_config.d/10-endurecer.conf <<'EOF'
PasswordAuthentication no
KbdInteractiveAuthentication no
PermitRootLogin no
EOF
sudo sshd -t && sudo systemctl restart ssh
sudo sshd -T | grep -E '^(passwordauthentication|permitrootlogin) '
```

Salida esperada:

```
permitrootlogin no
passwordauthentication no
```

Desde la laptop, **en una terminal nueva**, comprueben que la contraseña ya no entra:

```bash
ssh -o PubkeyAuthentication=no vm-lab true; echo "salida: $?"
# <USUARIO>@<IP_DE_SU_VM>: Permission denied (publickey).
# salida: 255
```

---

## Paso 3 - La API sigue escuchando solo en 127.0.0.1

```bash
grep API_HOST /etc/api-lab/env 2>/dev/null || echo "sin API_HOST: la API usa 127.0.0.1"
sudo ss -ltnp 'sport = :8080'
```

Salida esperada (el pid cambia):

```
State  Recv-Q Send-Q Local Address:Port Peer Address:Port Process
LISTEN 0      5          127.0.0.1:8080      0.0.0.0:*     users:(("python3",pid=183,fd=3))
```

Si dice `0.0.0.0:8080`, corrijan `API_HOST` en `/etc/api-lab/env` y reinicien `api-lab`.

---

## Paso 4 - Nginx delante de la API

Creen el formato de log con tiempos, el mismo de la sesión 1:

```bash
sudo tee /etc/nginx/conf.d/tiempos.conf <<'EOF'
# El formato combined de siempre, más el tiempo de respuesta al final
log_format tiempos '$remote_addr - $remote_user [$time_local] "$request" '
                   '$status $body_bytes_sent "$http_referer" "$http_user_agent" $request_time';
EOF
```

Y el sitio:

```bash
sudo tee /etc/nginx/sites-available/api-lab <<'EOF'
# Nginx recibe en el 80 y le pasa todo a la API, que solo escucha en 127.0.0.1
upstream api_lab {
    server 127.0.0.1:8080;
}

server {
    listen 80 default_server;
    listen [::]:80 default_server;
    server_name _;

    access_log /var/log/nginx/api-lab.access.log tiempos;
    error_log  /var/log/nginx/api-lab.error.log warn;

    # Nada de archivos ocultos (.env, .git) aunque alguien los deje en el servidor.
    location ~ /\. {
        deny all;
    }

    location / {
        proxy_pass         http://api_lab;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_read_timeout 30s;
    }
}
EOF
sudo ln -sf /etc/nginx/sites-available/api-lab /etc/nginx/sites-enabled/api-lab
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl reload nginx
```

Salida esperada de `nginx -t`:

```
nginx: the configuration file /etc/nginx/nginx.conf syntax is ok
nginx: configuration file /etc/nginx/nginx.conf test is successful
```

Prueben en la VM:

```bash
curl -s http://127.0.0.1/health; echo
curl -s http://127.0.0.1/lento > /dev/null
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1/.env
tail -2 /var/log/nginx/api-lab.access.log
```

Salida esperada:

```
{"estado": "ok", "pid": 183}
403
127.0.0.1 - - [09/Nov/2026:03:59:21 +0000] "GET /lento HTTP/1.1" 200 30 "-" "curl/8.5.0" 2.004
127.0.0.1 - - [09/Nov/2026:03:59:21 +0000] "GET /.env HTTP/1.1" 403 162 "-" "curl/8.5.0" 0.000
```

El `2.004` del final es el tiempo de respuesta de `/lento`.

---

## Paso 5 - UFW con el orden correcto

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw limit OpenSSH
sudo ufw allow "Nginx HTTP"
sudo ufw enable
```

**Antes de seguir**, desde la laptop y en una terminal nueva: `ssh vm-lab true && echo "sigo entrando"`. Si no entra, regresen a la terminal extra de la VM y corran `sudo ufw disable`.

```bash
sudo ufw status verbose
```

Salida esperada:

```
Status: active
Logging: on (low)
Default: deny (incoming), allow (outgoing), deny (routed)
New profiles: skip

To                         Action      From
--                         ------      ----
22/tcp (OpenSSH)           LIMIT IN    Anywhere
80/tcp (Nginx HTTP)        ALLOW IN    Anywhere
22/tcp (OpenSSH (v6))      LIMIT IN    Anywhere (v6)
80/tcp (Nginx HTTP (v6))   ALLOW IN    Anywhere (v6)
```

---

## Paso 6 - Probar desde fuera

Desde la laptop o la VM del otro equipo:

```bash
curl -s -m 3 -w "  [%{http_code}]\n" http://<IP_DE_SU_VM>/health
curl -s -m 3 -o /dev/null -w "/.env: %{http_code}\n" http://<IP_DE_SU_VM>/.env
curl -s -m 3 -o /dev/null -w "8080: %{http_code}" http://<IP_DE_SU_VM>:8080/health; echo " (curl salió $?)"
```

Salida esperada:

```
{"estado": "ok", "pid": 183}  [200]
/.env: 403
8080: 000 (curl salió 28)
```

| Puerto | Resultado | Por qué |
|---|---|---|
| 22 | entra con llave, no con contraseña | regla `limit OpenSSH` y sshd endurecido |
| 80 | `200` | Nginx y la regla `Nginx HTTP` |
| 80 `/.env` | `403` | `deny` a archivos ocultos |
| 8080 | `000`, salida 28 | la API solo escucha en 127.0.0.1 y UFW descarta el paquete |

---

## Paso 7 - Verlo fallar: Docker se salta UFW

Si la VM no tiene Docker: `sudo apt install -y docker.io`. Revisen `sudo ss -ltnp` antes y después de instalarlo: anoten lo que apareció.

En la VM:

```bash
sudo docker run -d --name fuga -p 8081:80 nginx:1.27-alpine
sudo docker ps --format '{{.Names}} {{.Ports}}'
sudo ufw status | grep 8081 || echo "UFW no tiene ninguna regla para el 8081"
```

Salida esperada:

```
fuga 0.0.0.0:8081->80/tcp, [::]:8081->80/tcp
UFW no tiene ninguna regla para el 8081
```

Desde fuera:

```bash
curl -s -m 3 -o /dev/null -w "8081: %{http_code}" http://<IP_DE_SU_VM>:8081/; echo " (curl salió $?)"
```

Salida esperada:

```
8081: 200 (curl salió 0)
```

Con UFW activo y negando por omisión, el contenedor contesta desde fuera. Ahora miren por qué, en la VM:

```bash
sudo iptables -t nat -S DOCKER
sudo iptables -S FORWARD | head -3
```

Salida esperada (la IP del contenedor cambia):

```
-N DOCKER
-A DOCKER ! -i docker0 -p tcp -m tcp --dport 8081 -j DNAT --to-destination <IP_DEL_CONTENEDOR>:80
-P FORWARD DROP
-A FORWARD -j DOCKER-USER
-A FORWARD -j DOCKER-FORWARD
```

Docker reescribe el destino en PREROUTING y acepta en FORWARD, antes que las cadenas de UFW. UFW filtra INPUT, y este paquete nunca pasa por ahí.

---

## Paso 8 - Arreglarlo y volver a probar

```bash
sudo docker rm -f fuga
sudo docker run -d --name fuga -p 127.0.0.1:8081:80 nginx:1.27-alpine
sudo ss -ltn 'sport = :8081'
curl -s -o /dev/null -w "desde la VM: %{http_code}\n" http://127.0.0.1:8081/
```

Salida esperada:

```
State  Recv-Q Send-Q Local Address:Port Peer Address:Port Process
LISTEN 0      4096       127.0.0.1:8081      0.0.0.0:*
desde la VM: 200
```

Desde fuera:

```bash
curl -s -m 3 -o /dev/null -w "8081: %{http_code}" http://<IP_DE_SU_VM>:8081/; echo " (curl salió $?)"
# 8081: 000 (curl salió 28)
```

Al terminar: `sudo docker rm -f fuga`.

---

## Paso 9 - El inventario de después

```bash
sudo ss -ltnp  > ~/puertos-despues.txt
sudo ss -lunp >> ~/puertos-despues.txt
sudo ufw status verbose >> ~/puertos-despues.txt
diff ~/puertos-antes.txt ~/puertos-despues.txt
```

Cada línea del `diff` es algo que cambió en su superficie de ataque. La tarea es explicarlas todas.

---

## Evidencia que entregan

Un archivo `s05-evidencia.md` en el repo de su equipo con:

1. `puertos-antes.txt` y `puertos-despues.txt`, y la salida del `diff`.
2. La salida de los `curl` desde fuera del paso 6, con la máquina desde la que se corrieron.
3. La salida del paso 7 (el 200 del 8081 con UFW activo) y la del paso 8 (el 000 después del arreglo).
4. Tres líneas que expliquen, con sus palabras, por qué Docker se brincó UFW.

---

## Checklist

- [ ] Entran por SSH con llave y la contraseña ya no entra (paso 2).
- [ ] La API escucha en `127.0.0.1:8080` y no en `0.0.0.0` (paso 3).
- [ ] `nginx -t` pasa y `/health` contesta 200 por el 80 (paso 4).
- [ ] UFW está activo con `deny (incoming)` y solo 22 y 80 permitidos (paso 5).
- [ ] Desde fuera: 80 da 200, `/.env` da 403 y 8080 da timeout (paso 6).
- [ ] Vieron al contenedor contestar desde fuera con UFW activo (paso 7).
- [ ] Después del arreglo, el 8081 ya no contesta desde fuera (paso 8).
- [ ] Tienen el inventario de antes y después, con el `diff` (paso 9).
