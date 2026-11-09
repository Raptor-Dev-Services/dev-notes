<!-- _class: lead -->

# Linux + Nginx para Desarrolladores Back-End

## Dev Notes  -  Material extra
**Stack:** Ubuntu 22.04  -  .NET 10  -  Nginx  -  Let's Encrypt
Rogelio Arriaga González  -  Raptor Dev Services

---

## Contexto de producción

Tu API `.NET` no corre en tu laptop  -  corre en un VPS o EC2.

```
Internet -- DNS app.example.com
                |

         Nginx  :443  (reverse proxy + SSL)
                |

        .NET Kestrel  :5000  (localhost)
                |

        PostgreSQL  :5432  (localhost)
```

> Nginx actúa como **portero**: termina SSL, filtra requests y reenvía al proceso .NET.
> Kestrel nunca queda expuesto directamente a internet.

---

<!-- _class: lead -->

# Parte 1
## Comandos Linux Esenciales

---

## Navegación y archivos

```bash
pwd                          # directorio actual
ls -lah                      # listar con permisos y tamaño legible
cd /etc/nginx                # cambiar directorio
mkdir -p /app/logs           # crear directorio (y padres si no existen)

cp archivo.txt destino/      # copiar
mv origen destino            # mover o renombrar
rm -rf carpeta/              # eliminar recursivo (¡destructivo!)

# búsqueda
find /var/log -name "*.log" -mtime -1   # logs del último día
grep -rn "ERROR" /var/log/app/          # buscar texto en archivos
which nginx                              # ubicación de un ejecutable
```

> `rm -rf` no pide confirmación. En producción, piénsalo dos veces.

---

## Permisos en Linux

```bash
chmod 755 script.sh          # rwxr-xr-x
chmod 644 appsettings.json   # rw-r--r--
chown www-data:www-data /var/www/html
```

| Octal | Simbólico | Significado |
|-------|-----------|-------------|
| **7** | `rwx` | lectura + escritura + ejecución |
| **6** | `rw-` | lectura + escritura |
| **5** | `r-x` | lectura + ejecución |
| **4** | `r--` | solo lectura |

> `chmod 755` = el dueño puede todo  -  grupo y otros pueden leer y ejecutar
> Usa `644` para archivos de configuración que no deben ser modificados por otros usuarios.

---

## Procesos y monitoreo de recursos

```bash
ps aux | grep dotnet         # buscar proceso .NET
kill -9 <PID>                # terminar proceso por ID
top                          # monitor interactivo de CPU/RAM
htop                         # versión mejorada (instalar con apt)

# disco y memoria
df -h                        # espacio libre por partición
du -sh /var/log/*            # tamaño de cada carpeta
free -h                      # RAM libre / usada

# red  -  ver qué está escuchando en qué puerto
ss -tlnp                     # puertos en escucha (reemplaza netstat)
ss -tlnp | grep 5000         # ¿está Kestrel corriendo?
curl -I http://localhost:5000/api/health   # probar endpoint local
```

> `ss -tlnp` es el equivalente moderno de `netstat -tlnp`.

---

<!-- _class: lead -->

# Parte 2
## SSH  -  Conectarse al Servidor

---

## SSH con clave pública (recomendado)

```bash
# 1. Generar par de claves en tu máquina local (una sola vez)
ssh-keygen -t ed25519 -C "instructor@example.com"
#   Crea: ~/.ssh/id_ed25519      (clave privada  -  NUNCA compartas)
#          ~/.ssh/id_ed25519.pub  (clave pública  -  se copia al servidor)

# 2. Copiar la clave pública al servidor
ssh-copy-id -i ~/.ssh/id_ed25519.pub ubuntu@app.example.com

# 3. Conectarse (ya sin contraseña)
ssh ubuntu@app.example.com

# Copiar archivos entre local y servidor
scp appsettings.Production.json ubuntu@app.example.com:/app/
scp ubuntu@app.example.com:/var/log/app/app.log ./local/
```

> Deshabilita el login por contraseña en `/etc/ssh/sshd_config`:
> `PasswordAuthentication no`

---

## SSH Port Forwarding

```bash
# Tunnel local: acceder a PostgreSQL del servidor desde tu máquina
ssh -L 5433:localhost:5432 ubuntu@app.example.com
# Ahora puedes conectarte a localhost:5433 desde DBeaver

# Tunnel remoto: exponer tu app local al servidor (útil para pruebas)
ssh -R 8080:localhost:5000 ubuntu@app.example.com

# SSH con archivo de config (~/.ssh/config)
# Host crm
#   HostName app.example.com
#   User ubuntu
#   IdentityFile ~/.ssh/id_ed25519
# Uso: ssh crm   (en lugar del comando largo)
```

> El port forwarding es útil para depurar bases de datos en producción
> **sin** abrir el puerto 5432 en el firewall.

---

<!-- _class: lead -->

# Parte 3
## systemd  -  Gestión de Servicios

---

## Comandos systemctl esenciales

```bash
systemctl status crm-api        # ver estado del servicio
systemctl start crm-api         # iniciar
systemctl stop crm-api          # detener
systemctl restart crm-api       # reiniciar (corta conexiones activas)
systemctl reload nginx          # recargar config SIN cortar conexiones

systemctl enable crm-api        # arrancar automáticamente al reiniciar el OS
systemctl disable crm-api       # quitar arranque automático
```

> Prefiere `reload` sobre `restart` para Nginx en producción  -  no corta conexiones activas.
> Para la app .NET siempre usa `restart` (no soporta reload en caliente).

---

## Definir el servicio .NET como unit de systemd

```ini
# /etc/systemd/system/crm-api.service
[Unit]
Description=Sistema de mensajería API
After=network.target

[Service]
User=appuser
WorkingDirectory=/app
ExecStart=/usr/bin/dotnet /app/Host.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_HTTP_PORTS=5000
EnvironmentFile=/etc/crm-api/env

[Install]
WantedBy=multi-user.target
```

```bash
systemctl daemon-reload     # leer los nuevos archivos .service
systemctl enable crm-api
systemctl start crm-api
```

---

## Ver logs con journalctl

```bash
journalctl -u crm-api               # todos los logs del servicio
journalctl -u crm-api -f            # en tiempo real (follow)
journalctl -u crm-api --since "1 hour ago"
journalctl -u crm-api -n 50         # últimas 50 líneas

# filtrar solo errores
journalctl -u crm-api -f | grep -E "(ERROR|WARN|Exception)"

# logs de Nginx
journalctl -u nginx -f
tail -f /var/log/nginx/access.log
tail -f /var/log/nginx/error.log | grep " 5[0-9][0-9] "   # solo 5xx
```

> Los logs de Serilog escritos a `stdout` aparecen automáticamente en `journalctl`.
> Configura `WriteTo.Console()` en tu app .NET para que funcione.

---

<!-- _class: lead -->

# Parte 4
## Nginx como Reverse Proxy

---

## Instalación de Nginx

```bash
apt update && apt install -y nginx
systemctl enable nginx && systemctl start nginx

# Verificar que funciona
curl -I http://localhost
# HTTP/1.1 200 OK
```

### Estructura de archivos de Nginx

```
/etc/nginx/
|-- nginx.conf                  # configuración global
|-- sites-available/            # configuraciones disponibles
|   `-- mensajeria-app            # tu server block
`-- sites-enabled/              # symlinks a los activos
    `-- mensajeria-app -> ../sites-available/mensajeria-app
```

> Nunca edites `nginx.conf` directamente. Crea un archivo en `sites-available` y habilítalo con un symlink.

---

## Server block: HTTP -> HTTPS redirect

```nginx
# /etc/nginx/sites-available/mensajeria-app
server {
    listen 80;
    server_name app.example.com api.example.com;

    # Redirigir todo HTTP a HTTPS
    return 301 https://$host$request_uri;
}
```

```bash
# Activar el sitio
ln -s /etc/nginx/sites-available/mensajeria-app /etc/nginx/sites-enabled/

# Validar sintaxis ANTES de aplicar
nginx -t

# Aplicar sin cortar conexiones
systemctl reload nginx
```

> Ejecuta siempre `nginx -t` antes de `reload`. Un error de sintaxis tira Nginx en producción.

---

## Server block: proxy_pass al .NET API

```nginx
server {
    listen 443 ssl;
    server_name app.example.com;

    ssl_certificate     /etc/letsencrypt/live/app.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/app.example.com/privkey.pem;

    add_header X-Frame-Options "SAMEORIGIN";
    add_header X-Content-Type-Options "nosniff";
    add_header Strict-Transport-Security "max-age=31536000" always;

    location / {
        proxy_pass         http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_read_timeout 90;
    }
}
```

---

## Nginx: soporte WebSockets para SignalR

El sistema de mensajería usa SignalR para mensajes en tiempo real.
Los WebSockets requieren una directiva especial de `Connection`:

```nginx
# Dentro del server block HTTPS
location /hubs/ {
    proxy_pass         http://localhost:5000;
    proxy_http_version 1.1;
    proxy_set_header   Upgrade    $http_upgrade;
    proxy_set_header   Connection "upgrade";
    proxy_set_header   Host       $host;
    proxy_read_timeout 3600;   # mantener abierto para SignalR
}

location / {
    proxy_pass         http://localhost:5000;
    proxy_http_version 1.1;
    proxy_set_header   Connection keep-alive;
    # ... resto de headers
}
```

> Sin `Upgrade` + `Connection "upgrade"`, el handshake de WebSocket falla
> y SignalR hace fallback a long-polling (más lento).

---

<!-- _class: lead -->

# Parte 5
## HTTPS con Certbot (Let's Encrypt)

---

## Instalar certificado SSL gratuito

```bash
# Instalar Certbot con plugin para Nginx
apt install -y certbot python3-certbot-nginx

# Obtener certificado Y configurar Nginx automáticamente
certbot --nginx -d app.example.com

# Si tienes múltiples subdominios
certbot --nginx -d app.example.com -d api.example.com

# Probar renovación sin aplicar cambios
certbot renew --dry-run
```

> Certbot instala un **cron job** en `/etc/cron.d/certbot` que renueva el certificado
> automáticamente antes de que expire (los certificados duran 90 días).

---

## Flujo completo: primer deploy del CRM

```bash
# 1. Apuntar el DNS: A record app.example.com -> IP del servidor
# 2. Configurar Nginx con solo el bloque HTTP (sin SSL todavía)
vim /etc/nginx/sites-available/mensajeria-app
ln -s /etc/nginx/sites-available/mensajeria-app /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx

# 3. Certbot obtiene el certificado y modifica el nginx.conf automáticamente
certbot --nginx -d app.example.com

# 4. Abrir puertos en UFW (firewall)
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable
ufw status
```

> El DNS debe estar propagado antes de correr Certbot.
> Let's Encrypt valida que `app.example.com` apunte a tu IP.

---

<!-- _class: lead -->

# Parte 6
## Nginx para React SPA

---

## Servir el build de React + client-side routing

```nginx
# /etc/nginx/sites-available/mensajeria-app-front
server {
    listen 443 ssl;
    server_name app.example.com;

    ssl_certificate     /etc/letsencrypt/live/app.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/app.example.com/privkey.pem;

    root /var/www/crm-front/dist;
    index index.html;

    # Gzip para archivos estáticos
    gzip on;
    gzip_types text/css application/javascript application/json;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://localhost:5000;
        proxy_set_header Host              $host;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## ¿Por qué `try_files $uri /index.html`?

```
Sin try_files:
  Usuario navega a app.example.com/conversations/42
  -> Nginx busca el archivo /var/www/crm-front/dist/conversations/42
  -> No existe -> 404

Con try_files $uri $uri/ /index.html:
  Usuario navega a app.example.com/conversations/42
  -> Nginx busca el archivo -> no existe
  -> Devuelve /index.html
  -> React Router toma control y renderiza la vista correcta
```

> Sin `try_files`, cualquier refresh o link directo a una ruta de React devuelve 404.
> Es el error más común al hacer el primer deploy de una SPA.

---

<!-- _class: lead -->

# Parte 7
## Variables de Entorno en Producción

---

## Formas de pasar variables de entorno al servicio

```bash
# Opción A: directamente en el .service (solo para valores no secretos)
# /etc/systemd/system/crm-api.service
[Service]
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_HTTP_PORTS=5000
```

```bash
# Opción B: EnvironmentFile (recomendado para secretos)
[Service]
EnvironmentFile=/etc/crm-api/env
```

```bash
# Contenido de /etc/crm-api/env
DATABASE_URL=Host=localhost;Database=crmdb;Username=crmuser;Password=s3cr3t
JWT__Secret=super-long-random-secret-256-bits
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=5000
```

```bash
# Permisos: solo root y appuser pueden leer el archivo
chown root:appuser /etc/crm-api/env
chmod 640 /etc/crm-api/env
```

---

## Variables de entorno: ¿dónde NO ponerlas?

| Lugar | Uso | ¿Secretos? |
|-------|-----|-----------|
| `EnvironmentFile` en systemd | **Recomendado** para producción |  Sí (con permisos 640) |
| Sección `[Service] Environment=` | Valores no sensibles (puertos, env name) |  No  -  visible en `systemctl show` |
| `/etc/environment` | Variables globales del sistema |  No  -  visible para todos |
| `~/.profile` o `~/.bashrc` | Variables de sesión interactiva |  No aplica a servicios |
| `appsettings.Production.json` | Valores no secretos |  Nunca passwords/tokens |

> En producción real, prefiere **AWS Secrets Manager** o **Azure Key Vault**
> sobre archivos en disco. El `EnvironmentFile` es el mínimo viable para un VPS.

---

## Resumen: flujo completo de deploy en VPS

```
1. ssh ubuntu@app.example.com
2. scp ./publish/* ubuntu@app.example.com:/app/
3. systemctl restart crm-api
4. journalctl -u crm-api -f          <- ver que arrancó sin errores
5. curl http://localhost:5000/api/health
6. nginx -t && systemctl reload nginx
7. curl https://app.example.com/api/health   <- prueba end-to-end
```

### Comandos de diagnóstico rápido

```bash
ss -tlnp | grep 5000          # ¿está corriendo .NET?
journalctl -u crm-api -n 20  # últimos logs de la app
tail -f /var/log/nginx/error.log   # errores de Nginx
certbot certificates           # estado de los certificados SSL
ufw status                     # estado del firewall
```

---

<!-- _class: lead -->

## Referencia rápida

| Tarea | Comando clave |
|-------|--------------|
| Logs en tiempo real | `journalctl -u crm-api -f` |
| Reiniciar app | `systemctl restart crm-api` |
| Recargar Nginx | `nginx -t && systemctl reload nginx` |
| Ver puertos | `ss -tlnp` |
| Copiar archivo al server | `scp archivo ubuntu@app.example.com:/app/` |
| Renovar SSL | `certbot renew --dry-run` |
| Ver estado firewall | `ufw status` |
| Logs de errores Nginx | `tail -f /var/log/nginx/error.log` |

**Fuentes:** *Learning Modern Linux* (O'Reilly)  -  *Efficient Linux at the Command Line* (Barrett)
