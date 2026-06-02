# 03 · Linux y Nginx

> Fuente: *Learning Modern Linux* (O'Reilly) — Ch.3 Shells y scripting, Ch.4 Control de acceso, Ch.6 systemd, Ch.7 Networking

## Problema que resuelve

Los servidores de producción en AWS/Azure corren Linux. El desarrollador necesita navegar el sistema, gestionar procesos y servicios, y configurar Nginx como reverse proxy para exponer la aplicación .NET.

## Comandos esenciales de navegación

```bash
# archivos y directorios
ls -lah                    # listar con tamaño legible
pwd                        # directorio actual
cd /etc/nginx              # cambiar directorio
mkdir -p /app/logs         # crear directorio (y padres)
cp archivo.txt destino/    # copiar
mv origen destino          # mover o renombrar
rm -rf carpeta/            # eliminar recursivo (destructivo)

# búsqueda
find /var/log -name "*.log" -mtime -1   # logs del último día
grep -rn "ERROR" /var/log/app/          # buscar texto en archivos
which nginx                              # ubicación de un comando

# permisos
chmod 755 script.sh        # rwxr-xr-x
chown www-data:www-data /var/www/html
```

## Usuarios y permisos

```bash
# crear usuario de sistema (sin shell de login) para un servicio
useradd --system --no-create-home --shell /usr/sbin/nologin appuser

# agregar usuario a un grupo
usermod -aG sudo rogelio

# ver grupos del usuario actual
groups

# ejecutar como otro usuario
sudo -u appuser comando
```

Permisos en formato octal:

| Octal | Simbólico | Significado |
|-------|-----------|-------------|
| 7 | rwx | lectura, escritura, ejecución |
| 6 | rw- | lectura y escritura |
| 5 | r-x | lectura y ejecución |
| 4 | r-- | solo lectura |

## Gestión de procesos y servicios (systemd)

```bash
# servicios
systemctl status nginx          # ver estado
systemctl start nginx           # iniciar
systemctl stop nginx            # detener
systemctl restart nginx         # reiniciar
systemctl reload nginx          # recargar config sin cortar conexiones
systemctl enable nginx          # arrancar con el sistema
systemctl disable nginx         # deshabilitar arranque automático

# logs del servicio
journalctl -u nginx             # todos los logs
journalctl -u nginx -f          # en tiempo real (follow)
journalctl -u nginx --since "1 hour ago"

# procesos
ps aux | grep nginx             # buscar proceso
kill -9 <PID>                   # terminar proceso
top                             # monitor interactivo
htop                            # monitor mejorado
```

## Monitoreo de recursos

```bash
# disco
df -h                           # espacio por partición
du -sh /var/log/*               # tamaño de carpetas
ncdu /var/log                   # navegador interactivo de disco

# memoria
free -h                         # RAM libre/usada
cat /proc/meminfo

# red
ss -tlnp                        # puertos en escucha
netstat -tlnp                   # alternativa (deprecated)
curl -I http://localhost:5000/health  # probar endpoint local
```

## Nginx como reverse proxy

Nginx recibe las peticiones HTTPS en el puerto 443 y las reenvía a la app .NET escuchando en localhost.

### Instalación

```bash
apt update && apt install -y nginx
systemctl enable nginx && systemctl start nginx
```

### Configuración básica

```nginx
# /etc/nginx/sites-available/gtm-suite
server {
    listen 80;
    server_name api.raptordev.io;

    # redirigir HTTP → HTTPS
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    server_name api.raptordev.io;

    ssl_certificate     /etc/letsencrypt/live/api.raptordev.io/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.raptordev.io/privkey.pem;

    # headers de seguridad
    add_header X-Frame-Options "SAMEORIGIN";
    add_header X-Content-Type-Options "nosniff";
    add_header Strict-Transport-Security "max-age=31536000" always;

    location / {
        proxy_pass         http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 90;
    }
}
```

```bash
# activar el sitio
ln -s /etc/nginx/sites-available/gtm-suite /etc/nginx/sites-enabled/

# validar configuración
nginx -t

# aplicar sin cortar conexiones
systemctl reload nginx
```

### WebSockets (SignalR)

Si la app usa SignalR, el reverse proxy necesita soporte de WebSockets:

```nginx
location /hubs/ {
    proxy_pass         http://localhost:5000;
    proxy_http_version 1.1;
    proxy_set_header   Upgrade $http_upgrade;
    proxy_set_header   Connection "upgrade";
    proxy_set_header   Host $host;
}
```

## SSL con Certbot (Let's Encrypt)

```bash
# instalar certbot
apt install -y certbot python3-certbot-nginx

# obtener certificado y configurar nginx automáticamente
certbot --nginx -d api.raptordev.io

# renovación automática (cron incluido por Certbot)
certbot renew --dry-run   # probar renovación sin aplicar
```

Certbot agrega un cron job en `/etc/cron.d/certbot` para renovar antes del vencimiento (cada 90 días).

## Firewall con UFW

```bash
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable
ufw status
```

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Nginx como reverse proxy cuando la app corre en EC2 directamente | Nginx si la app corre en ECS Fargate (el load balancer de AWS reemplaza a Nginx) |
| `systemctl reload` para aplicar cambios sin downtime | `systemctl restart` si se puede evitar (corta conexiones activas) |
| Certbot para SSL en instancias EC2 accesibles directamente | Certbot si hay un ALB/CloudFront por delante (usar ACM en su lugar) |

---

## Scripting Bash esencial para automatización
> Fuente: *Efficient Linux at the Command Line* (Barrett) — Ch.4 Shell Scripting

```bash
#!/usr/bin/env bash
set -euo pipefail
# set -e: salir si cualquier comando falla
# set -u: error si se usa una variable no definida
# set -o pipefail: el pipe falla si cualquier parte falla

# Variables y tipos básicos
APP_NAME="gtm-suite"
VERSION="${1:-latest}"              # primer argumento o "latest" como default
LOG_DIR="/var/log/${APP_NAME}"

# Condicionales
if [[ -d "$LOG_DIR" ]]; then
    echo "Directorio de logs existe"
else
    mkdir -p "$LOG_DIR"
    echo "Directorio creado: $LOG_DIR"
fi

if [[ -z "$VERSION" ]]; then        # -z: string vacío
    echo "Error: versión requerida" >&2
    exit 1
fi

if [[ "$VERSION" == "latest" ]]; then
    VERSION=$(git describe --tags --abbrev=0)
fi

# Loops
for file in /etc/nginx/sites-available/*.conf; do
    echo "Procesando: $file"
    nginx -t -c "$file"
done

# Funciones
log() {
    local level="$1"
    local message="$2"
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] [$level] $message"
}

deploy() {
    local version="$1"
    log "INFO" "Iniciando deploy versión $version"

    docker pull "${ECR_REGISTRY}/${APP_NAME}:${version}"
    docker stop "${APP_NAME}" 2>/dev/null || true   # no falla si no existe
    docker run -d \
        --name "${APP_NAME}" \
        --restart unless-stopped \
        -p 5000:8080 \
        "${ECR_REGISTRY}/${APP_NAME}:${version}"

    log "INFO" "Deploy completado"
}

# Llamar la función
deploy "$VERSION"
```

### Comandos útiles para administración de servidor

```bash
# Ver qué proceso usa un puerto
lsof -i :5000
ss -tlnp | grep 5000

# Logs en tiempo real con filtro
journalctl -u gtm-suite -f | grep -E "(ERROR|WARN)"
tail -f /var/log/nginx/access.log | grep " 5[0-9][0-9] "   # solo 5xx

# Monitorear un proceso hasta que arranque
until curl -s http://localhost:5000/api/health > /dev/null; do
    echo "Esperando que arranque..."
    sleep 2
done
echo "Servicio disponible"

# Crear servicio systemd para la app .NET
cat > /etc/systemd/system/gtm-suite.service << 'EOF'
[Unit]
Description=GTM Suite API
After=network.target

[Service]
User=appuser
WorkingDirectory=/app
ExecStart=/usr/bin/dotnet /app/Host.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_HTTP_PORTS=5000
EnvironmentFile=/etc/gtm-suite/env

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable gtm-suite
systemctl start gtm-suite
```


---

## Glosario

| Término | Definición |
|---------|-----------|
| Nginx | Servidor web de alto rendimiento utilizado como reverse proxy, load balancer y servidor de contenido estático |
| Reverse proxy | Intermediario que recibe peticiones externas y las reenvía al servidor de aplicación interno |
| systemd | Sistema de inicio y gestor de servicios estándar en Linux moderno; reemplaza a SysVinit |
| journalctl | Herramienta de systemd para consultar el diario de logs del sistema y de servicios individuales |
| Certbot | Cliente oficial de Let's Encrypt que automatiza la obtención y renovación de certificados TLS |
| Let's Encrypt | Autoridad certificadora gratuita y automatizada que emite certificados TLS/HTTPS válidos por 90 días |
| UFW | Uncomplicated Firewall; interfaz simplificada de iptables para gestionar reglas de red en Ubuntu/Debian |
| HSTS | HTTP Strict Transport Security; cabecera que instruye al navegador a usar solo HTTPS por un período determinado |
| WebSocket | Protocolo de comunicación bidireccional y persistente sobre TCP, necesario para SignalR |
| SSL/TLS | Protocolos criptográficos que cifran la comunicación entre cliente y servidor (HTTPS) |
| proxy_pass | Directiva de Nginx que reenvía la petición al servidor de aplicación especificado por URL |
| Bash shebang | Línea `#!/usr/bin/env bash` al inicio de un script que define el intérprete a utilizar |
| set -euo pipefail | Opciones de Bash para detener el script ante errores, variables no definidas o fallos en pipes |

---

*Rogelio Arriaga Gonzalez*
