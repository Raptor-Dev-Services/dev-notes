# Glosario Técnico  -  Linux  -  Nginx

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### A record (DNS)
**Categoría:** Red / DNS
**Definición:** Registro de tipo "Address" en el Domain Name System que mapea un nombre de dominio a una dirección IPv4. Es el tipo de registro más fundamental del DNS y el primer paso para que el tráfico llegue a tu servidor.
**En el proyecto:** Se crea un A record que apunta `app.example.com` a la IP pública del VPS o instancia EC2 antes de ejecutar Certbot. Sin él, Let's Encrypt no puede validar el dominio.

### `add_header`
**Categoría:** Nginx
**Definición:** Directiva de Nginx que agrega un encabezado HTTP a la respuesta enviada al cliente. Se usa para implementar políticas de seguridad a nivel de servidor sin modificar el código de la aplicación.
**En el proyecto:** El server block del CRM agrega `X-Frame-Options`, `X-Content-Type-Options` y `Strict-Transport-Security` para endurecer la seguridad del API expuesto en `app.example.com`.

### `apt` / `apt-get`
**Categoría:** Linux
**Definición:** Advanced Package Tool, el gestor de paquetes de distribuciones Debian/Ubuntu. Permite instalar, actualizar y desinstalar software del sistema operativo. `apt` es la interfaz interactiva moderna; `apt-get` es la versión clásica orientada a scripts.
**En el proyecto:** Se usa `apt update && apt install -y nginx` para instalar Nginx y `apt install -y certbot python3-certbot-nginx` para instalar Certbot en el servidor de producción del CRM.

### `ASPNETCORE_ENVIRONMENT`
**Categoría:** .NET / Variables de entorno
**Definición:** Variable de entorno que indica a ASP.NET Core el ambiente de ejecución: `Development`, `Staging` o `Production`. Determina qué `appsettings.{Environment}.json` se carga y qué middleware se activa (p. ej. Swagger solo en Development).
**En el proyecto:** Se define con valor `Production` en el `EnvironmentFile` del servicio systemd `crm-api.service` para que el sistema de mensajería cargue la configuración correcta de base de datos y JWT.

### `ASPNETCORE_HTTP_PORTS`
**Categoría:** .NET / Variables de entorno
**Definición:** Variable de entorno que indica en qué puerto(s) debe escuchar Kestrel. Reemplaza a la configuración `urls` en versiones recientes de ASP.NET Core y permite sobreescribir el puerto sin modificar `appsettings.json`.
**En el proyecto:** Se establece en `5000` en el unit de systemd del CRM para que Kestrel escuche únicamente en `localhost:5000`, sin quedar expuesto directamente a internet.

---

## B

### Bash
**Categoría:** Linux
**Definición:** Bourne Again Shell, el intérprete de comandos predeterminado en la mayoría de distribuciones Linux. Es tanto un lenguaje de scripting como la interfaz interactiva de la terminal. Los archivos `.sh` son scripts escritos en Bash.
**En el proyecto:** Todos los comandos de deploy, configuración de Nginx, gestión de systemd y diagnóstico del servidor del CRM se ejecutan en Bash sobre Ubuntu 22.04.

---

## C

### `cd`
**Categoría:** Linux
**Definición:** Comando "change directory" que cambia el directorio de trabajo actual en la terminal. Acepta rutas absolutas (empiezan con `/`) o relativas al directorio actual.
**En el proyecto:** Uso común al navegar al directorio `/etc/nginx` para editar configuraciones o a `/app` para revisar los binarios publicados del CRM.

### Certbot
**Categoría:** SSL / Let's Encrypt
**Definición:** Cliente oficial de Let's Encrypt que automatiza la obtención y renovación de certificados TLS/SSL gratuitos. El plugin `python3-certbot-nginx` detecta la configuración de Nginx y la modifica automáticamente para activar HTTPS.
**En el proyecto:** Se usa `certbot --nginx -d app.example.com` para obtener el certificado SSL del sistema de mensajería. Certbot instala además un cron job que renueva el certificado automáticamente antes de su vencimiento a los 90 días.

### `certbot certificates`
**Categoría:** SSL / Certbot
**Definición:** Subcomando de Certbot que lista todos los certificados instalados en el servidor, su dominio, fecha de expiración y ruta en disco.
**En el proyecto:** Se incluye en los comandos de diagnóstico rápido para verificar el estado del certificado de `app.example.com` sin tener que buscar los archivos `.pem` manualmente.

### `certbot renew --dry-run`
**Categoría:** SSL / Certbot
**Definición:** Simula el proceso de renovación del certificado sin aplicar ningún cambio real. Útil para verificar que el proceso de renovación automática funcionará cuando llegue la fecha sin arriesgar el certificado activo.
**En el proyecto:** Se ejecuta después de configurar Certbot para confirmar que la renovación automática del certificado de `app.example.com` está correctamente configurada.

### `chmod`
**Categoría:** Linux
**Definición:** Comando "change mode" que modifica los permisos de archivos y directorios usando notación octal (755, 644) o simbólica (u+x). Los tres dígitos representan los permisos del dueño, del grupo y de otros usuarios, respectivamente.
**En el proyecto:** Se usa `chmod 640 /etc/crm-api/env` para que solo `root` y `appuser` puedan leer el archivo de secretos del CRM (connection string, JWT secret).

### `chown`
**Categoría:** Linux
**Definición:** Comando "change owner" que cambia el dueño y/o el grupo de un archivo o directorio. El formato es `chown usuario:grupo ruta`. Con la opción `-R` aplica el cambio de forma recursiva.
**En el proyecto:** Se usa `chown root:appuser /etc/crm-api/env` para asignar el archivo de variables de entorno al grupo `appuser` y así permitir que el proceso del CRM lo lea sin darle acceso a otros usuarios del sistema.

### Client-side routing
**Categoría:** Frontend / React
**Definición:** Estrategia de navegación en aplicaciones SPA donde el cambio de URL no genera una petición al servidor sino que es manejado por el framework de frontend (React Router). La URL cambia en el navegador, pero el servidor nunca recibe la petición de página completa.
**En el proyecto:** El frontend del sistema de mensajería usa React Router para navegar entre `/conversations`, `/contacts` y `/settings` sin recargas de página. Nginx debe estar configurado con `try_files` para que estos paths funcionen al hacer refresh.

### Connection string
**Categoría:** Backend / Base de datos
**Definición:** Cadena de texto que contiene todos los parámetros necesarios para establecer una conexión a una base de datos: host, puerto, nombre de la base, usuario y contraseña. Es información sensible que nunca debe estar en código fuente.
**En el proyecto:** La variable `DATABASE_URL` en `/etc/crm-api/env` contiene la connection string de PostgreSQL del CRM: `Host=localhost;Database=crmdb;Username=crmuser;Password=s3cr3t`.

### `cp`
**Categoría:** Linux
**Definición:** Comando "copy" que copia archivos o directorios de una ruta a otra. Con `-r` copia directorios de forma recursiva. No elimina el archivo original.
**En el proyecto:** Uso general al copiar archivos de configuración o binarios publicados durante el proceso de deploy manual del CRM en el VPS.

### Cron job
**Categoría:** Linux / Automatización
**Definición:** Tarea programada que el demonio `cron` ejecuta automáticamente en intervalos definidos por una expresión cron (minuto, hora, día, mes, día de semana). Los trabajos del sistema se guardan en `/etc/cron.d/`.
**En el proyecto:** Certbot instala automáticamente un cron job en `/etc/cron.d/certbot` que intenta renovar el certificado SSL del CRM dos veces al día, garantizando que nunca expire.

### `curl`
**Categoría:** Linux / Red
**Definición:** Herramienta de línea de comandos para transferir datos usando protocolos HTTP, HTTPS, FTP, etc. Con `-I` muestra solo los headers de respuesta. Es el estándar para probar endpoints desde la terminal.
**En el proyecto:** Se usa `curl -I http://localhost:5000/api/health` para verificar que Kestrel responde, y `curl https://app.example.com/api/health` como prueba end-to-end después del deploy del CRM.

---

## D

### `daemon-reload`
**Categoría:** Linux / systemd
**Definición:** Subcomando de `systemctl` que le indica al demonio de systemd que vuelva a leer los archivos `.service` del disco. Es obligatorio ejecutarlo cada vez que se crea o modifica un unit file; sin él, los cambios no tienen efecto.
**En el proyecto:** Se ejecuta `systemctl daemon-reload` después de crear o modificar `/etc/systemd/system/crm-api.service` para que systemd reconozca el servicio del sistema de mensajería.

### `df -h`
**Categoría:** Linux
**Definición:** Comando "disk free" que muestra el espacio disponible en cada partición o sistema de archivos montado. La opción `-h` presenta los tamaños en formato legible (GB, MB) en lugar de bloques de bytes.
**En el proyecto:** Herramienta de diagnóstico para verificar que el VPS del CRM tiene suficiente espacio en disco antes de un deploy o cuando se sospechan problemas por logs que crecen demasiado.

### DNS (Domain Name System)
**Categoría:** Red
**Definición:** Sistema jerárquico y distribuido que traduce nombres de dominio legibles por humanos (como `app.example.com`) a direcciones IP. La propagación de un cambio de DNS puede tardar minutos u horas dependiendo del TTL configurado.
**En el proyecto:** El A record del DNS debe apuntar `app.example.com` a la IP del servidor antes de instalar el certificado SSL con Certbot, ya que Let's Encrypt valida el dominio haciendo una petición HTTP al mismo.

### `du -sh`
**Categoría:** Linux
**Definición:** Comando "disk usage" que calcula el espacio en disco ocupado por un archivo o directorio. La opción `-s` muestra solo el total (sin desglosar subdirectorios) y `-h` usa unidades legibles.
**En el proyecto:** Se usa `du -sh /var/log/*` para identificar qué directorio de logs consume más espacio en el servidor del CRM, especialmente relevante si Serilog escribe logs de alto volumen.

---

## E

### EC2 (Amazon Elastic Compute Cloud)
**Categoría:** Cloud / AWS
**Definición:** Servicio de cómputo en la nube de AWS que provee servidores virtuales (instancias) bajo demanda. Una instancia EC2 es funcionalmente equivalente a un VPS: tiene IP pública, sistema operativo Linux y recursos configurables de CPU/RAM.
**En el proyecto:** Alternativa al VPS para alojar el sistema de mensajería. La misma configuración de Nginx, systemd y Certbot aplica tanto en un VPS como en una instancia EC2.

### `ed25519`
**Categoría:** Criptografía / SSH
**Definición:** Algoritmo de firma digital de curva elíptica recomendado para generar pares de claves SSH. Produce claves más cortas y seguras que RSA-2048 o RSA-4096, y es significativamente más rápido en la firma.
**En el proyecto:** Se usa `ssh-keygen -t ed25519` para generar el par de claves que autentica al desarrollador contra el servidor del sistema de mensajería sin usar contraseñas.

### `EnvironmentFile`
**Categoría:** Linux / systemd
**Definición:** Directiva del archivo `.service` de systemd que apunta a un archivo de texto plano con variables de entorno en formato `CLAVE=VALOR`. systemd carga ese archivo e inyecta las variables al proceso antes de iniciarlo. Es el mecanismo recomendado para pasar secretos a un servicio.
**En el proyecto:** El unit `crm-api.service` tiene `EnvironmentFile=/etc/crm-api/env` para inyectar la connection string de PostgreSQL y el JWT secret al proceso .NET sin exponerlos en el archivo `.service` visible mediante `systemctl show`.

---

## F

### `find`
**Categoría:** Linux
**Definición:** Comando que busca archivos y directorios en el sistema de archivos según criterios como nombre, tipo, fecha de modificación o tamaño. La opción `-mtime -1` filtra archivos modificados en las últimas 24 horas.
**En el proyecto:** Se usa para localizar logs recientes en `/var/log/app/` y diagnosticar problemas en el servidor de producción del CRM.

### Firewall
**Categoría:** Red / Seguridad
**Definición:** Sistema que controla el tráfico de red entrante y saliente según reglas definidas. En Linux, `ufw` (Uncomplicated Firewall) es la interfaz amigable sobre `iptables`. Un firewall mal configurado puede bloquear el tráfico legítimo o dejar puertos peligrosos abiertos.
**En el proyecto:** Se configura UFW en el servidor del CRM para permitir solo los puertos 22 (SSH), 80 (HTTP) y 443 (HTTPS). El puerto 5000 de Kestrel y 5432 de PostgreSQL no se abren hacia internet.

### `free -h`
**Categoría:** Linux
**Definición:** Comando que muestra la memoria RAM total, usada y libre del sistema, así como el uso de swap. La opción `-h` presenta los valores en unidades legibles (GB, MB).
**En el proyecto:** Herramienta de diagnóstico de recursos en el servidor del CRM, especialmente útil cuando la API .NET presenta latencia alta o el proceso es finalizado por el sistema operativo (OOM killer).

### `fullchain.pem`
**Categoría:** SSL / TLS
**Definición:** Archivo que contiene el certificado del dominio concatenado con los certificados intermedios de la cadena de confianza. Nginx necesita `fullchain.pem` (no solo el certificado del dominio) para que los navegadores confíen en el certificado sin advertencias.
**En el proyecto:** Certbot genera este archivo en `/etc/letsencrypt/live/app.example.com/fullchain.pem`. Se referencia en la directiva `ssl_certificate` del server block de Nginx del CRM.

---

## G

### `grep`
**Categoría:** Linux
**Definición:** Herramienta de búsqueda de texto que filtra líneas que coinciden con un patrón (expresión regular o literal). Las opciones más comunes son: `-r` (recursivo), `-n` (número de línea), `-i` (insensible a mayúsculas) y `-E` (expresiones regulares extendidas).
**En el proyecto:** Se usa `grep -rn "ERROR" /var/log/app/` para buscar errores en los logs del CRM y `journalctl -u crm-api -f | grep -E "(ERROR|WARN|Exception)"` para filtrar logs relevantes en tiempo real.

### `gzip on`
**Categoría:** Nginx
**Definición:** Directiva de Nginx que habilita la compresión gzip de las respuestas HTTP. Reduce significativamente el tamaño de los archivos transferidos (CSS, JavaScript, JSON) y mejora los tiempos de carga para el usuario final.
**En el proyecto:** Se activa en el server block del frontend del CRM con `gzip_types text/css application/javascript application/json` para comprimir los assets del build de React antes de enviarlos al navegador.

---

## H

### Handshake (WebSocket)
**Categoría:** Protocolo / Red
**Definición:** Proceso de negociación inicial entre cliente y servidor para establecer una conexión WebSocket. El cliente envía una petición HTTP con el header `Upgrade: websocket`; si el servidor acepta, la conexión se eleva al protocolo WebSocket bidireccional.
**En el proyecto:** Nginx debe pasar los headers `Upgrade` y `Connection "upgrade"` al backend para que el handshake de SignalR funcione. Sin estas directivas, SignalR cae en modo long-polling.

### `Host` (header HTTP)
**Categoría:** HTTP / Nginx
**Definición:** Header HTTP que indica el nombre del servidor al que el cliente está haciendo la petición. Es especialmente importante en configuraciones con múltiples dominios en el mismo servidor (virtual hosting).
**En el proyecto:** El server block de Nginx pasa el header `Host $host` al API de .NET con `proxy_set_header Host $host` para que el CRM pueda conocer el dominio original de cada petición.

### `htop`
**Categoría:** Linux
**Definición:** Monitor interactivo de procesos y recursos del sistema, versión mejorada de `top`. Muestra CPU, RAM y swap en gráficas de barras, permite ordenar por columnas y terminar procesos con teclas de función. Debe instalarse con `apt install htop`.
**En el proyecto:** Herramienta de diagnóstico en el servidor del CRM para identificar si la API .NET o PostgreSQL están consumiendo recursos excesivos en momentos de carga.

### HTTPS (HTTP Secure)
**Categoría:** Red / Seguridad
**Definición:** Protocolo HTTP sobre TLS/SSL que cifra la comunicación entre el cliente y el servidor. Sin HTTPS, cualquier intermediario puede leer o modificar el tráfico, incluidos tokens JWT y datos de contactos del CRM.
**En el proyecto:** Nginx termina HTTPS en el puerto 443 usando el certificado de Let's Encrypt. La comunicación interna entre Nginx y Kestrel usa HTTP plano en `localhost:5000`, lo cual es seguro porque no sale de la máquina.

---

## I

### `index.html`
**Categoría:** Frontend / Nginx
**Definición:** Archivo HTML principal de una aplicación SPA. Contiene los tags `<script>` y `<link>` que cargan el JavaScript y CSS generados por el bundler (Vite). Es el único archivo HTML real en el build de producción.
**En el proyecto:** Nginx sirve `/index.html` para cualquier ruta que no corresponda a un archivo estático real gracias a la directiva `try_files`. Esto permite que React Router maneje la navegación del CRM.

### IP pública
**Categoría:** Red
**Definición:** Dirección IP enrutable en internet asignada al servidor. Es la dirección a la que apunta el A record del DNS. Puede ser estática (fija, recomendada para producción) o dinámica (cambia en cada reinicio).
**En el proyecto:** La IP pública del VPS o EC2 es el destino al que apunta el A record de `app.example.com`. Certbot valida este mapeo antes de emitir el certificado SSL.

---

## J

### journalctl
**Categoría:** Linux / systemd
**Definición:** Herramienta de systemd para consultar el journal, el sistema centralizado de logs del sistema operativo. Permite filtrar por unidad (`-u`), seguir en tiempo real (`-f`), limitar por tiempo (`--since`) o número de líneas (`-n`).
**En el proyecto:** Es el comando principal para ver los logs de la API del CRM: `journalctl -u crm-api -f` muestra los logs de Serilog en tiempo real durante y después de los deploys.

### JWT (JSON Web Token)
**Categoría:** Seguridad / Backend
**Definición:** Estándar (RFC 7519) para transmitir información entre partes como un objeto JSON firmado criptográficamente. Consta de tres partes: header, payload y firma. El servidor verifica la firma sin necesidad de consultar una base de datos en cada petición.
**En el proyecto:** El sistema de mensajería usa JWT HS256 para autenticar a agentes y administradores. El `JWT__Secret` se almacena en el `EnvironmentFile` del servidor y nunca en código fuente ni en `appsettings.json`.

---

## K

### Kestrel
**Categoría:** .NET / Servidor Web
**Definición:** Servidor web embebido y multiplataforma incluido en ASP.NET Core. Es el servidor que ejecuta directamente el código .NET y escucha peticiones HTTP. En producción se recomienda colocarlo detrás de un reverse proxy como Nginx en lugar de exponerlo directamente.
**En el proyecto:** Kestrel ejecuta la API del sistema de mensajería en `localhost:5000`. Nginx actúa como reverse proxy que recibe el tráfico de internet y lo reenvía a Kestrel, protegiendo el proceso .NET de estar expuesto directamente.

### `kill -9`
**Categoría:** Linux
**Definición:** Comando que envía la señal `SIGKILL` al proceso con el ID especificado, terminándolo inmediatamente sin darle oportunidad de limpiar recursos. Es la opción de último recurso cuando un proceso no responde a señales normales.
**En el proyecto:** Se usa solo en emergencias cuando el proceso del CRM queda colgado y no responde a `systemctl stop crm-api`. En condiciones normales se prefiere `systemctl restart`.

---

## L

### Let's Encrypt
**Categoría:** SSL / TLS
**Definición:** Autoridad de certificación (CA) gratuita, automatizada y abierta, operada por la Internet Security Research Group (ISRG). Emite certificados TLS/SSL válidos por 90 días con renovación automática. Es el estándar de facto para HTTPS en servidores sin presupuesto para certificados comerciales.
**En el proyecto:** Provee el certificado SSL gratuito para `app.example.com`. La integración con Nginx se hace a través de Certbot.

### `listen`
**Categoría:** Nginx
**Definición:** Directiva de Nginx que especifica en qué puerto (y opcionalmente en qué interfaz) debe escuchar el servidor. `listen 80` acepta HTTP; `listen 443 ssl` acepta HTTPS y requiere las directivas `ssl_certificate` y `ssl_certificate_key`.
**En el proyecto:** El server block del CRM tiene `listen 80` para redirigir a HTTPS y `listen 443 ssl` para el tráfico cifrado. Nunca se expone el puerto 5000 de Kestrel directamente.

### `ln -s`
**Categoría:** Linux
**Definición:** Comando que crea un enlace simbólico (symlink), que es un puntero a otro archivo o directorio. A diferencia de un enlace duro, un symlink puede apuntar a directorios y a archivos en sistemas de archivos diferentes.
**En el proyecto:** Se usa `ln -s /etc/nginx/sites-available/mensajeria-app /etc/nginx/sites-enabled/` para activar la configuración del CRM en Nginx siguiendo el patrón sites-available/sites-enabled.

### `location`
**Categoría:** Nginx
**Definición:** Bloque de configuración de Nginx que define cómo procesar peticiones que coincidan con un prefijo de URL o expresión regular. Dentro de un `server` block puede haber múltiples `location` blocks con diferentes reglas para diferentes rutas.
**En el proyecto:** El servidor del CRM tiene tres bloques `location`: `/` para el frontend, `/api/` para el proxy al .NET API, y `/hubs/` para SignalR con soporte de WebSockets.

### Long-polling
**Categoría:** Protocolo / Real-time
**Definición:** Técnica de emulación de comunicación en tiempo real donde el cliente hace una petición HTTP que el servidor mantiene abierta hasta tener datos que enviar (o hasta que se agota el tiempo). Es menos eficiente que WebSockets pero funciona en entornos donde los WebSockets están bloqueados.
**En el proyecto:** SignalR cae automáticamente en long-polling si el handshake de WebSocket falla (por ejemplo, si Nginx no tiene configurados los headers `Upgrade` y `Connection`). Es notablemente más lento para el chat en tiempo real del CRM.

### `ls -lah`
**Categoría:** Linux
**Definición:** Comando de listado de directorio con opciones combinadas: `-l` muestra formato largo (permisos, dueño, tamaño, fecha), `-a` incluye archivos ocultos (que empiezan con `.`), y `-h` usa unidades legibles para el tamaño.
**En el proyecto:** Se usa para inspeccionar permisos y contenidos de directorios críticos como `/app`, `/etc/nginx/` y `/etc/crm-api/` en el servidor de producción del CRM.

---

## M

### `mkdir -p`
**Categoría:** Linux
**Definición:** Comando que crea un directorio y, con la opción `-p`, también crea todos los directorios padre necesarios si no existen. Sin `-p`, el comando falla si el directorio padre no existe.
**En el proyecto:** Se usa para crear estructuras de directorios como `/app/logs` en el servidor del CRM sin tener que crear cada nivel de forma separada.

### `mv`
**Categoría:** Linux
**Definición:** Comando "move" que mueve o renombra archivos y directorios. Si el destino es un directorio existente, mueve el archivo dentro de él. Si el destino es un nombre diferente, renombra el archivo.
**En el proyecto:** Uso común durante el proceso de deploy cuando se renombran o reorganizan archivos de configuración o binarios publicados del CRM en el servidor.

---

## N

### `netstat`
**Categoría:** Linux / Red
**Definición:** Herramienta clásica para mostrar conexiones de red, tablas de routing y estadísticas de interfaz. Ha sido reemplazada por `ss` en sistemas Linux modernos porque es más rápida y tiene mayor funcionalidad.
**En el proyecto:** Los slides referencian `ss -tlnp` como el reemplazo moderno de `netstat -tlnp`. En Ubuntu 22.04, `ss` está disponible por defecto y `netstat` puede no estar instalado.

### `nginx -t`
**Categoría:** Nginx
**Definición:** Subcomando que valida la sintaxis de todos los archivos de configuración de Nginx sin aplicar ningún cambio. Devuelve "syntax is ok" y "test is successful" si la configuración es válida, o muestra el error y la línea problemática si no lo es.
**En el proyecto:** Es una regla obligatoria ejecutar `nginx -t` ANTES de cada `systemctl reload nginx`. Un error de sintaxis en la configuración del CRM tiraría Nginx en producción, dejando el servicio inaccesible.

### `nginx.conf`
**Categoría:** Nginx
**Definición:** Archivo de configuración global de Nginx, ubicado en `/etc/nginx/nginx.conf`. Define configuración a nivel de servidor como el número de workers, el tamaño máximo de body de peticiones y los directorios incluidos. No se edita directamente para configuraciones de sitio.
**En el proyecto:** Nginx incluye automáticamente los archivos de `sites-enabled/` desde `nginx.conf`. La configuración específica del CRM vive en `sites-available/mensajeria-app`, no en `nginx.conf`.

---

## O

### Octal (permisos)
**Categoría:** Linux
**Definición:** Sistema de notación numérica en base 8 usado para representar permisos de archivos Linux. Cada dígito representa los permisos del dueño, grupo y otros usuarios, respectivamente. Los valores son: 4 (lectura), 2 (escritura), 1 (ejecución).
**En el proyecto:** Los permisos `640` en `/etc/crm-api/env` significan: dueño (`root`) puede leer y escribir (6=4+2), grupo (`appuser`) puede leer (4), otros no tienen acceso (0). Protege los secretos del CRM.

---

## P

### `PasswordAuthentication no`
**Categoría:** SSH / Seguridad
**Definición:** Directiva del archivo `/etc/ssh/sshd_config` que deshabilita la autenticación por contraseña en SSH, obligando a usar claves criptográficas. Elimina el vector de ataque de fuerza bruta contra contraseñas débiles.
**En el proyecto:** Se recomienda configurar esta directiva en el servidor del sistema de mensajería una vez que las claves SSH estén correctamente configuradas, reduciendo drásticamente la superficie de ataque del servidor.

### PEM (Privacy Enhanced Mail)
**Categoría:** SSL / TLS
**Definición:** Formato de archivo codificado en base64 para almacenar certificados y claves criptográficas. Los archivos `.pem` pueden contener certificados (`fullchain.pem`) o claves privadas (`privkey.pem`). El nombre histórico viene del protocolo de correo, pero hoy se usa principalmente para TLS.
**En el proyecto:** Certbot genera `fullchain.pem` y `privkey.pem` en `/etc/letsencrypt/live/app.example.com/`. Nginx los referencia para terminar TLS en el servidor del CRM.

### `PID` (Process ID)
**Categoría:** Linux
**Definición:** Identificador numérico único asignado por el sistema operativo a cada proceso en ejecución. Se usa para referenciar procesos en comandos como `kill` o para verificar qué proceso ocupa un puerto con `ss -tlnp`.
**En el proyecto:** Se identifica el PID del proceso .NET del CRM con `ps aux | grep dotnet` cuando se necesita diagnosticar o terminar el proceso manualmente.

### Port forwarding (SSH)
**Categoría:** SSH / Red
**Definición:** Funcionalidad de SSH que crea un túnel encriptado para reenviar tráfico de un puerto local a un puerto remoto (o viceversa). El forwarding local (`-L`) permite acceder a servicios del servidor desde la máquina local; el remoto (`-R`) hace lo opuesto.
**En el proyecto:** Se usa `ssh -L 5433:localhost:5432 ubuntu@app.example.com` para conectar DBeaver al PostgreSQL del sistema de mensajería sin abrir el puerto 5432 en el firewall, manteniendo la base de datos inaccesible desde internet.

### PostgreSQL
**Categoría:** Base de datos
**Definición:** Sistema de gestión de bases de datos relacionales de código abierto, conocido por su robustez, conformidad con estándares SQL y soporte para tipos de datos avanzados (JSON, arrays, UUID). En Linux corre como servicio systemd en el puerto 5432 por defecto.
**En el proyecto:** Base de datos del sistema de mensajería. Corre en `localhost:5432` del servidor de producción, solo accesible desde el mismo servidor (Kestrel en puerto 5000) o mediante SSH port forwarding.

### `privkey.pem`
**Categoría:** SSL / TLS
**Definición:** Archivo que contiene la clave privada del certificado TLS. Es la parte secreta del par criptográfico: el certificado es público, la clave privada NUNCA debe compartirse ni respaldarse en lugares inseguros. Certbot la protege con permisos restrictivos.
**En el proyecto:** Nginx lo referencia con la directiva `ssl_certificate_key` para descifrar el tráfico HTTPS del CRM. Solo `root` puede leer este archivo; si se filtra, un atacante podría suplantar el servidor.

### `proxy_pass`
**Categoría:** Nginx
**Definición:** Directiva que convierte Nginx en un reverse proxy, reenviando las peticiones entrantes a la URL especificada (generalmente la aplicación backend). El cliente se comunica con Nginx; Nginx se comunica con la aplicación.
**En el proyecto:** `proxy_pass http://localhost:5000` reenvía todo el tráfico HTTPS del CRM al proceso Kestrel que corre localmente, sin que Kestrel quede expuesto a internet.

### `proxy_read_timeout`
**Categoría:** Nginx
**Definición:** Directiva que define el tiempo máximo que Nginx espera una respuesta del backend antes de cerrar la conexión con un error 504 Gateway Timeout. El valor predeterminado es 60 segundos.
**En el proyecto:** Se configura en `90` segundos para el endpoint principal del API del CRM y en `3600` segundos para el endpoint `/hubs/` de SignalR, que mantiene conexiones WebSocket persistentes.

### `proxy_set_header`
**Categoría:** Nginx
**Definición:** Directiva que agrega o modifica headers HTTP antes de reenviar la petición al backend. Es crucial para pasar información del cliente original al servidor de aplicaciones, que de otro modo solo vería la IP de Nginx.
**En el proyecto:** El server block del CRM pasa `X-Real-IP`, `X-Forwarded-For` y `X-Forwarded-Proto` para que el API .NET pueda registrar la IP real del cliente y saber si la petición original fue HTTPS.

### `ps aux`
**Categoría:** Linux
**Definición:** Comando que lista todos los procesos en ejecución del sistema con información detallada: usuario, PID, uso de CPU y memoria, y el comando que inició el proceso. La combinación `ps aux | grep dotnet` filtra solo los procesos .NET.
**En el proyecto:** Se usa para verificar que el proceso del sistema de mensajería está corriendo y obtener su PID en situaciones de diagnóstico o cuando el servicio systemd no responde.

### Puerto (port)
**Categoría:** Red
**Definición:** Número del 0 al 65535 que identifica un canal lógico de comunicación dentro de una IP. Los puertos bien conocidos son: 22 (SSH), 80 (HTTP), 443 (HTTPS), 5000 (Kestrel .NET por defecto), 5432 (PostgreSQL).
**En el proyecto:** El servidor del CRM expone externamente solo los puertos 22, 80 y 443. Kestrel (:5000) y PostgreSQL (:5432) son internos al servidor y no accesibles desde internet.

### `pwd`
**Categoría:** Linux
**Definición:** Comando "print working directory" que muestra la ruta absoluta del directorio actual de trabajo en la terminal. Útil para verificar dónde se está antes de ejecutar comandos que modifican archivos.
**En el proyecto:** Uso básico de orientación en la terminal al administrar el servidor del CRM, especialmente al navegar entre `/etc/nginx`, `/app` y `/etc/crm-api`.

---

## R

### React Router
**Categoría:** Frontend / React
**Definición:** Biblioteca de React que implementa el routing del lado del cliente en aplicaciones SPA. Intercepta los cambios de URL del navegador y renderiza el componente correspondiente sin hacer una nueva petición HTTP al servidor.
**En el proyecto:** El frontend del sistema de mensajería usa React Router para navegar entre la bandeja de conversaciones, el detalle de chat, la gestión de contactos y la configuración de canales.

### `Restart=always`
**Categoría:** Linux / systemd
**Definición:** Directiva del unit de systemd que indica al sistema que reinicie automáticamente el servicio si termina por cualquier motivo: error, señal o código de salida distinto de cero. Garantiza alta disponibilidad del proceso.
**En el proyecto:** El unit `crm-api.service` tiene `Restart=always` con `RestartSec=10` para que la API del CRM se recupere automáticamente si el proceso .NET falla, sin intervención manual.

### `return 301`
**Categoría:** HTTP / Nginx
**Definición:** Directiva de Nginx que envía una redirección permanente HTTP 301 al cliente, indicando que el recurso se ha movido definitivamente a la URL especificada. Los navegadores y buscadores siguen la redirección automáticamente y actualizan su caché.
**En el proyecto:** El server block en el puerto 80 del CRM usa `return 301 https://$host$request_uri` para redirigir todo el tráfico HTTP a HTTPS, garantizando que ninguna petición viaje sin cifrar.

### Reverse proxy
**Categoría:** Nginx / Arquitectura
**Definición:** Servidor intermediario que recibe peticiones de clientes y las reenvía a uno o más servidores backend. Desde la perspectiva del cliente, se comunica con el reverse proxy sin saber que existe el backend. Provee SSL termination, balanceo de carga, compresión y caché.
**En el proyecto:** Nginx actúa como reverse proxy entre internet y Kestrel. Termina SSL, agrega headers de seguridad y reenvía las peticiones al proceso .NET del CRM en `localhost:5000`.

### `rm -rf`
**Categoría:** Linux
**Definición:** Comando "remove" con opciones `-r` (recursivo, para directorios) y `-f` (forzado, sin pedir confirmación). Elimina permanentemente archivos y directorios sin posibilidad de recuperación. No existe papelera de reciclaje en la terminal de Linux.
**En el proyecto:** Debe usarse con extrema precaución en el servidor del CRM. Un `rm -rf /app/` accidental borraría todos los binarios publicados y requeriría un redeploy completo.

### `root` (Nginx)
**Categoría:** Nginx
**Definición:** Directiva que define el directorio del sistema de archivos desde donde Nginx sirve archivos estáticos. Las rutas en las peticiones HTTP se combinan con este directorio para encontrar el archivo correspondiente.
**En el proyecto:** Se configura `root /var/www/crm-front/dist` para que Nginx sirva el build de producción del frontend React del CRM desde el directorio `dist` generado por Vite.

### `root` (usuario Linux)
**Categoría:** Linux / Seguridad
**Definición:** Usuario con privilegios máximos en Linux (UID 0). Puede leer, escribir y ejecutar cualquier archivo del sistema sin restricciones. En producción se recomienda no ejecutar servicios como root para limitar el impacto de una vulnerabilidad.
**En el proyecto:** El servicio `crm-api.service` corre como `appuser` en lugar de `root`. El archivo `/etc/crm-api/env` es propiedad de `root:appuser` para que solo estos dos puedan leerlo.

---

## S

### `scp`
**Categoría:** Linux / SSH
**Definición:** Secure Copy Protocol, herramienta que copia archivos entre máquinas usando SSH para cifrar la transferencia. La sintaxis es `scp origen destino` donde origen o destino pueden ser `usuario@host:/ruta/`.
**En el proyecto:** Se usa `scp ./publish/* ubuntu@app.example.com:/app/` para subir los binarios publicados del sistema de mensajería al servidor. También se usa para descargar logs: `scp ubuntu@app.example.com:/var/log/app/app.log ./`.

### `server_name`
**Categoría:** Nginx
**Definición:** Directiva que especifica los nombres de dominio que este server block debe manejar. Nginx usa el header `Host` de la petición para seleccionar el server block correcto cuando hay múltiples sitios en el mismo servidor.
**En el proyecto:** El server block del CRM tiene `server_name app.example.com api.example.com` para manejar tanto el dominio principal como el subdominio de API del servicio.

### Señal (Linux signal)
**Categoría:** Linux / Procesos
**Definición:** Mecanismo de comunicación entre procesos en Linux. Las señales más comunes son: `SIGTERM` (15, terminación graciosa), `SIGKILL` (9, terminación forzada) y `SIGHUP` (1, recargar configuración). Los procesos pueden interceptar `SIGTERM` para cerrar conexiones antes de terminar.
**En el proyecto:** `systemctl stop crm-api` envía `SIGTERM` al proceso .NET del CRM dándole tiempo para cerrar conexiones activas. `kill -9 <PID>` envía `SIGKILL` que termina inmediatamente sin limpieza.

### Serilog
**Categoría:** .NET / Logging
**Definición:** Biblioteca de logging estructurado para .NET que permite escribir logs en múltiples destinos (sinks) simultáneamente: consola, archivo, base de datos, servicios de APM. Los logs estructurados incluyen propiedades tipadas además del mensaje de texto.
**En el proyecto:** La API del CRM usa Serilog con `WriteTo.Console()` para que los logs aparezcan en `stdout` y sean capturados automáticamente por `journalctl`. Esto permite ver los logs del CRM con `journalctl -u crm-api -f`.

### SignalR
**Categoría:** .NET / Real-time
**Definición:** Biblioteca de ASP.NET Core que abstrae la comunicación en tiempo real entre servidor y cliente. Usa WebSockets como transporte preferido y cae automáticamente a Server-Sent Events o long-polling si WebSockets no está disponible. El servidor expone "hubs" y los clientes se conectan a ellos.
**En el proyecto:** El sistema de mensajería usa SignalR para entregar mensajes nuevos en tiempo real a los agentes en la bandeja de conversaciones. Los hubs se exponen en la ruta `/hubs/` que Nginx maneja con soporte especial de WebSockets.

### `sites-available` / `sites-enabled`
**Categoría:** Nginx
**Definición:** Convención de Nginx (adoptada de Apache) que separa las configuraciones disponibles de las activas. Los archivos en `sites-available` son las configuraciones completas; los archivos en `sites-enabled` son symlinks a los que están activos. Permite activar o desactivar sitios sin borrar su configuración.
**En el proyecto:** La configuración del CRM vive en `/etc/nginx/sites-available/mensajeria-app`. Se activa con `ln -s` hacia `sites-enabled`. Para desactivar sin borrar: `rm /etc/nginx/sites-enabled/mensajeria-app`.

### SPA (Single-Page Application)
**Categoría:** Frontend
**Definición:** Arquitectura de aplicación web donde el servidor entrega una sola página HTML y toda la navegación posterior ocurre en el navegador mediante JavaScript, sin recargas completas de página. El routing es manejado por el framework de frontend.
**En el proyecto:** El frontend del sistema de mensajería es una SPA construida con React 19 y Vite. Nginx debe configurarse con `try_files $uri /index.html` para soportar el client-side routing correctamente.

### `ss -tlnp`
**Categoría:** Linux / Red
**Definición:** Herramienta "socket statistics" que muestra los sockets TCP en escucha (`-t` TCP, `-l` listening, `-n` numérico, `-p` proceso). Es el reemplazo moderno de `netstat -tlnp` y viene preinstalada en Ubuntu 22.04.
**En el proyecto:** Se usa `ss -tlnp | grep 5000` para verificar que Kestrel está corriendo y escuchando antes de probar el endpoint del CRM. También verifica que PostgreSQL en `:5432` solo escucha en `localhost`.

### SSL (Secure Sockets Layer) / TLS (Transport Layer Security)
**Categoría:** Seguridad / Red
**Definición:** Protocolos criptográficos que proveen comunicación cifrada y autenticación sobre redes. SSL es el predecesor obsoleto; TLS (actualmente en versión 1.3) es el estándar vigente. Coloquialmente se usa "SSL" para referirse a ambos.
**En el proyecto:** Nginx termina TLS usando el certificado de Let's Encrypt para el dominio `app.example.com`. Toda la comunicación entre los navegadores de los agentes y el servidor del CRM viaja cifrada.

### `ssl_certificate` / `ssl_certificate_key`
**Categoría:** Nginx / SSL
**Definición:** Directivas de Nginx que apuntan a los archivos de certificado público (`fullchain.pem`) y clave privada (`privkey.pem`) necesarios para establecer conexiones TLS. Son obligatorias en server blocks con `listen 443 ssl`.
**En el proyecto:** Apuntan a los archivos generados por Certbot en `/etc/letsencrypt/live/app.example.com/`. Certbot los actualiza automáticamente en cada renovación; Nginx los lee en cada reload.

### `stdout` (standard output)
**Categoría:** Linux / Procesos
**Definición:** Flujo de salida estándar de un proceso, equivalente a la "pantalla" del programa. systemd captura `stdout` y `stderr` de los procesos que gestiona y los redirige automáticamente al journal, haciendo que estén disponibles via `journalctl`.
**En el proyecto:** `WriteTo.Console()` en Serilog escribe los logs del CRM a `stdout`, lo que hace que aparezcan automáticamente en `journalctl -u crm-api` sin configuración adicional de archivos de log.

### `Strict-Transport-Security` (HSTS)
**Categoría:** HTTP / Seguridad
**Definición:** Header de respuesta HTTP que instruye a los navegadores a acceder siempre al sitio usando HTTPS, nunca HTTP, durante el tiempo especificado en `max-age`. Previene ataques de downgrade y man-in-the-middle.
**En el proyecto:** El server block de Nginx del CRM agrega `Strict-Transport-Security "max-age=31536000" always`, indicando a los navegadores que siempre usen HTTPS para `app.example.com` durante un año.

### Symlink (enlace simbólico)
**Categoría:** Linux
**Definición:** Archivo especial del sistema de archivos que apunta a otro archivo o directorio. Si se elimina el symlink, el archivo original no se afecta. Si se elimina el archivo original, el symlink queda "roto". Se crea con `ln -s`.
**En el proyecto:** El patrón `sites-available/sites-enabled` de Nginx usa symlinks. El archivo de configuración real vive en `sites-available/mensajeria-app`; el symlink en `sites-enabled/` activa el sitio.

### systemctl
**Categoría:** Linux / systemd
**Definición:** Herramienta principal de línea de comandos para controlar systemd: iniciar, detener, reiniciar, habilitar y deshabilitar servicios, así como consultar su estado. Es la interfaz de usuario para el gestor de servicios del sistema.
**En el proyecto:** Todos los ciclos de vida del servicio del CRM (`crm-api`) se gestionan con `systemctl`: `start`, `stop`, `restart`, `enable`, `status`. También gestiona Nginx y PostgreSQL.

### systemd
**Categoría:** Linux
**Definición:** Sistema de inicialización y gestor de servicios que es el estándar en la mayoría de distribuciones Linux modernas (Ubuntu, Debian, RHEL, etc.). Reemplazó a SysV init y Upstart. Gestiona el arranque del sistema, los servicios (units), los logs (journal) y las dependencias entre servicios.
**En el proyecto:** systemd gestiona el proceso de la API del sistema de mensajería como un "unit" (`crm-api.service`), garantizando que arranque al iniciar el servidor, se reinicie si falla y sus logs sean accesibles via `journalctl`.

---

## T

### `tail -f`
**Categoría:** Linux
**Definición:** Comando que muestra las últimas líneas de un archivo y continúa mostrando las nuevas líneas conforme se agregan (`-f` de "follow"). Es la forma clásica de seguir logs en tiempo real en archivos de texto.
**En el proyecto:** Se usa `tail -f /var/log/nginx/error.log` para monitorear errores de Nginx en tiempo real y `tail -f /var/log/nginx/error.log | grep " 5[0-9][0-9] "` para filtrar solo errores 5xx del CRM.

### TLS termination (SSL termination)
**Categoría:** Nginx / Seguridad
**Definición:** Proceso por el cual un servidor intermediario (Nginx) descifra el tráfico HTTPS entrante antes de reenviarlo al backend en HTTP plano. El backend no necesita manejar SSL, simplificando su configuración. La conexión interna entre Nginx y el backend es segura porque es local.
**En el proyecto:** Nginx descifra el tráfico HTTPS del CRM en el puerto 443 y lo reenvía a Kestrel en `localhost:5000` como HTTP. Kestrel nunca ve TLS; solo Nginx tiene la clave privada del certificado.

### `top`
**Categoría:** Linux
**Definición:** Monitor interactivo de procesos que muestra en tiempo real el uso de CPU, memoria, y los procesos más activos del sistema. Viene preinstalado en prácticamente todas las distribuciones Linux. Se sale con `q`.
**En el proyecto:** Herramienta de diagnóstico básica en el servidor del CRM para identificar procesos con alto consumo de CPU o RAM. `htop` es preferible cuando está disponible por su interfaz más amigable.

### `try_files`
**Categoría:** Nginx
**Definición:** Directiva de Nginx que prueba una lista de rutas de archivo en orden y sirve la primera que exista. Si ninguna existe, puede servir un archivo por defecto o devolver un código de error. Es esencial para el soporte de SPAs con client-side routing.
**En el proyecto:** `try_files $uri $uri/ /index.html` permite que el frontend React del CRM responda correctamente cuando un usuario accede directamente a `/conversations/42` o recarga la página en esa ruta.

### Tunnel SSH
**Categoría:** SSH / Red
**Definición:** Canal cifrado creado a través de una conexión SSH que permite reenviar tráfico de red de forma segura. Permite acceder a servicios que escuchan en la red privada del servidor como si estuvieran en la máquina local del desarrollador.
**En el proyecto:** Se usa para acceder a PostgreSQL del CRM (`localhost:5432` del servidor) desde DBeaver en la máquina local, mapeándolo al puerto `5433` local, sin abrir el puerto en el firewall.

---

## U

### Ubuntu 22.04 LTS
**Categoría:** Linux
**Definición:** Distribución Linux basada en Debian, en su versión 22.04 "Jammy Jellyfish" con soporte de largo plazo (LTS) hasta abril 2027. Es una de las distribuciones más usadas para servidores en la nube por su estabilidad, amplio soporte y compatibilidad con la mayoría de herramientas DevOps.
**En el proyecto:** Sistema operativo del servidor de producción del sistema de mensajería, tanto en VPS como en EC2. Incluye systemd, `ss`, `ufw` y `apt` preinstalados.

### UFW (Uncomplicated Firewall)
**Categoría:** Linux / Seguridad
**Definición:** Interfaz simplificada sobre `iptables` que facilita la gestión del firewall en Ubuntu/Debian. Permite definir reglas por nombre de servicio (`ssh`, `http`) o por número de puerto (`443/tcp`). Se activa con `ufw enable`.
**En el proyecto:** Se configura UFW en el servidor del CRM para permitir solo SSH (22), HTTP (80) y HTTPS (443). Los puertos 5000 y 5432 permanecen cerrados hacia internet, accesibles solo localmente.

### Unit file (systemd)
**Categoría:** Linux / systemd
**Definición:** Archivo de configuración de systemd que define cómo gestionar un servicio, socket, dispositivo u otro recurso. Los archivos `.service` son el tipo más común. Se almacenan en `/etc/systemd/system/` para servicios definidos por el administrador.
**En el proyecto:** `/etc/systemd/system/crm-api.service` es el unit file que define cómo ejecutar la API del CRM: usuario, directorio, comando de inicio, variables de entorno y política de reinicio automático.

### `ufw status`
**Categoría:** Linux / UFW
**Definición:** Subcomando que muestra el estado actual del firewall: si está activo o inactivo, y la lista de reglas configuradas con los puertos y protocolos permitidos o denegados.
**En el proyecto:** Incluido en la lista de comandos de diagnóstico rápido del CRM. Permite verificar rápidamente que solo los puertos necesarios (22, 80, 443) están abiertos en el servidor.

### `Upgrade` (WebSocket header)
**Categoría:** HTTP / WebSockets
**Definición:** Header HTTP enviado por el cliente para solicitar un cambio de protocolo de HTTP a WebSocket. El servidor responde con `101 Switching Protocols` si acepta la actualización. Nginx debe configurarse explícitamente para pasar este header al backend.
**En el proyecto:** La directiva `proxy_set_header Upgrade $http_upgrade` en el bloque `/hubs/` de Nginx permite que el handshake de WebSocket de SignalR llegue correctamente al backend del CRM.

---

## V

### Variables de entorno
**Categoría:** Linux / .NET
**Definición:** Pares clave-valor disponibles para un proceso durante su ejecución, inyectadas por el sistema operativo o el process manager (systemd). Son el mecanismo preferido para configurar aplicaciones en producción sin hardcodear valores en el código.
**En el proyecto:** El sistema de mensajería recibe la connection string de PostgreSQL, el JWT secret y la configuración de entorno mediante variables inyectadas por systemd desde el `EnvironmentFile`. ASP.NET Core lee estas variables automáticamente.

### VPS (Virtual Private Server)
**Categoría:** Cloud / Infraestructura
**Definición:** Servidor virtual dedicado alojado en infraestructura de un proveedor de hosting (DigitalOcean, Linode, Vultr, etc.). A diferencia de la nube pública (AWS/Azure), un VPS tiene recursos fijos y precio predecible. Equivalente funcional a una instancia EC2.
**En el proyecto:** Opción de alojamiento del sistema de mensajería como alternativa a EC2, especialmente para etapas iniciales donde el costo predecible es prioritario. La configuración de Linux, Nginx, systemd y Certbot es idéntica.

### Virtual hosting
**Categoría:** Nginx / Red
**Definición:** Capacidad de un servidor web para alojar múltiples sitios o aplicaciones en la misma IP y puerto, diferenciándolos por el header `Host`. En Nginx se implementa con múltiples `server` blocks con diferentes `server_name`.
**En el proyecto:** Permite que el mismo servidor del CRM sirva tanto el frontend (`app.example.com`) como el API (`api.example.com`) con configuraciones de Nginx diferentes, usando la directiva `server_name` para diferenciarlos.

---

## W

### `WantedBy=multi-user.target`
**Categoría:** Linux / systemd
**Definición:** Directiva de la sección `[Install]` del unit file que especifica en qué target de systemd debe habilitarse el servicio. `multi-user.target` corresponde al modo normal de operación del servidor (sin interfaz gráfica), por lo que el servicio arranca cuando el sistema está listo para recibir conexiones de red.
**En el proyecto:** El unit `crm-api.service` usa `WantedBy=multi-user.target` para que la API del CRM arranque automáticamente cuando el VPS o EC2 se reinicia, sin intervención manual.

### WebSocket
**Categoría:** Protocolo / Red
**Definición:** Protocolo de comunicación bidireccional y persistente sobre TCP, establecido con un handshake HTTP inicial. A diferencia de HTTP, una vez establecida la conexión, tanto el servidor como el cliente pueden enviar mensajes en cualquier momento sin una solicitud previa.
**En el proyecto:** SignalR usa WebSockets para enviar mensajes nuevos del CRM a los agentes en tiempo real. Nginx requiere configuración especial (`Upgrade`, `Connection "upgrade"`, `proxy_read_timeout 3600`) para soportar estas conexiones persistentes.

### `which`
**Categoría:** Linux
**Definición:** Comando que muestra la ruta absoluta del ejecutable que se usaría al escribir un comando en la terminal. Busca en los directorios listados en la variable de entorno `PATH`.
**En el proyecto:** Se usa `which nginx` para verificar la ruta de instalación de Nginx en el servidor. También es útil para confirmar que `dotnet` está disponible en el PATH del sistema antes de configurar el unit de systemd.

### `WorkingDirectory`
**Categoría:** Linux / systemd
**Definición:** Directiva del unit file de systemd que establece el directorio de trabajo del proceso antes de ejecutarlo. Es importante para aplicaciones que leen archivos relativos a su ubicación o que escriben logs en rutas relativas.
**En el proyecto:** En `crm-api.service` se configura `WorkingDirectory=/app` para que el proceso .NET del CRM encuentre sus archivos de configuración y assets relativos correctamente desde el directorio de publicación.

---

## X

### `X-Content-Type-Options`
**Categoría:** HTTP / Seguridad
**Definición:** Header de respuesta HTTP con valor `nosniff` que previene que el navegador "adivine" el tipo MIME de un archivo ignorando el `Content-Type` declarado. Mitiga ataques de MIME sniffing donde contenido malicioso se disfraza de un tipo de archivo benigno.
**En el proyecto:** Nginx agrega este header en el server block del CRM con `add_header X-Content-Type-Options "nosniff"` para endurecer la seguridad del servicio sin cambiar el código de la API.

### `X-Forwarded-For`
**Categoría:** HTTP / Nginx
**Definición:** Header HTTP que contiene la lista de IPs por las que ha pasado la petición, separadas por comas. El primer valor es la IP del cliente original. Nginx lo propaga al backend con `$proxy_add_x_forwarded_for` que agrega la IP del cliente actual al encabezado existente.
**En el proyecto:** Permite que el API del CRM registre en logs y en la base de datos la IP real del agente o usuario que hace la petición, en lugar de la IP de Nginx (`127.0.0.1`).

### `X-Forwarded-Proto`
**Categoría:** HTTP / Nginx
**Definición:** Header HTTP que indica el protocolo original de la petición (`http` o `https`) antes de que el reverse proxy la procese. Permite al backend saber si la conexión original del cliente era segura, incluso cuando el backend recibe HTTP plano.
**En el proyecto:** El API del CRM usa este header para generar URLs absolutas con el esquema correcto (https) en responses y para redirigir a HTTPS cuando sea necesario. Se pasa con `proxy_set_header X-Forwarded-Proto $scheme`.

### `X-Frame-Options`
**Categoría:** HTTP / Seguridad
**Definición:** Header de respuesta HTTP que controla si la página puede ser embebida en un `<iframe>`. El valor `SAMEORIGIN` permite iframes solo desde el mismo dominio; `DENY` los prohíbe completamente. Mitiga ataques de clickjacking.
**En el proyecto:** Nginx agrega `X-Frame-Options "SAMEORIGIN"` al server block del CRM para prevenir que la interfaz del sistema sea embebida en sitios maliciosos de terceros que intenten robar credenciales de agentes.

### `X-Real-IP`
**Categoría:** HTTP / Nginx
**Definición:** Header HTTP simplificado que contiene solo la IP del cliente que hizo la petición al reverse proxy, a diferencia de `X-Forwarded-For` que puede contener múltiples IPs en cadena. Es más sencillo de parsear en el backend.
**En el proyecto:** Nginx pasa este header al API del CRM con `proxy_set_header X-Real-IP $remote_addr` para que la aplicación tenga acceso directo a la IP del cliente sin necesidad de parsear la cadena de `X-Forwarded-For`.

---

*Fuentes: Learning Modern Linux (O'Reilly)  -  Efficient Linux at the Command Line (Barrett)  -  Documentación oficial de Nginx  -  Documentación oficial de systemd*
