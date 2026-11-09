# Glosario - S05 - Red, firewall y SSH

> Definiciones de los términos técnicos de la sesión 5.
> Ordenado alfabéticamente.

---

## A

### Activación por socket
**Categoría:** systemd / Servicios
**Definición:** Mecanismo en el que systemd abre y escucha el puerto en nombre de un servicio, y lo arranca cuando llega la primera conexión. El servicio recibe el socket ya abierto. Una unit `.socket` declara qué escucha y una `.service` qué atiende.
**En el curso:** En Ubuntu 24.04 SSH funciona así: `ss -ltnp` puede mostrar a `systemd` en el 22, y cambiar el puerto exige `daemon-reload` y reiniciar `ssh.socket`.

---

### authorized_keys
**Categoría:** SSH
**Definición:** Archivo `~/.ssh/authorized_keys` del usuario en el servidor, con una llave pública por línea. Quien demuestre tener la privada correspondiente entra como ese usuario. sshd lo rechaza si otros usuarios pueden escribir en él, en `~/.ssh` o en el home.
**En el curso:** Con `chmod 777` en el home, la llave correcta se rechazó con "bad ownership or modes"; con 750, entró.

---

## C

### Cadena (iptables)
**Categoría:** Redes / Firewall
**Definición:** Lista ordenada de reglas que el kernel evalúa en un punto del recorrido del paquete. Las principales son PREROUTING (al llegar, antes de decidir el destino), INPUT (paquetes para esta máquina), FORWARD (paquetes que pasan hacia otro destino) y OUTPUT (los que salen de aquí).
**En el curso:** UFW pone sus reglas en INPUT; Docker reescribe en PREROUTING y acepta en FORWARD, por eso se brinca a UFW.

---

### Certbot
**Categoría:** TLS / Herramientas
**Definición:** Cliente de Let's Encrypt que obtiene y renueva certificados TLS gratuitos y puede configurar Nginx para usarlos. Valida que controlas el dominio, normalmente con un reto por el puerto 80.
**En el curso:** Necesita un dominio que apunte a la VM. El 80 se queda abierto después de activar HTTPS: lo usan la redirección y la renovación.

---

## D

### DNAT
**Categoría:** Redes / Firewall
**Definición:** Destination NAT: regla que cambia la dirección o el puerto de destino de un paquete antes de enrutarlo. Es lo que hace que un puerto de la máquina "lleve" a otro destino.
**En el curso:** `docker run -p 8081:80` crea un DNAT del 8081 de la VM hacia el puerto 80 del contenedor.

---

### DOCKER-USER
**Categoría:** Docker / Firewall
**Definición:** Cadena de iptables que Docker crea vacía y evalúa antes que sus propias reglas de FORWARD, para que el administrador ponga ahí sus filtros. Docker no la reescribe, pero sus reglas no persisten solas tras un reinicio.
**En el curso:** Una regla `DROP` con `--ctorigdstport 8081` cerró el contenedor publicado. Para empezar se prefiere publicar en 127.0.0.1.

---

## E

### ed25519
**Categoría:** SSH / Criptografía
**Definición:** Algoritmo de firma de curva elíptica usado para llaves SSH. Produce llaves cortas, es rápido y no tiene parámetros que se puedan elegir mal. Es el tipo recomendado para llaves nuevas.
**En el curso:** `ssh-keygen -t ed25519 -C "<USUARIO>@laptop"`.

---

## F

### Firewall
**Categoría:** Redes / Seguridad
**Definición:** Componente que decide qué paquetes pasan según reglas de origen, destino, puerto y protocolo. No apaga servicios: un programa puede seguir escuchando detrás de un firewall que no deja llegar a nadie.
**En el curso:** "Escuchar" (`ss`), "estar permitido" (`ufw status`) y "ser alcanzable" (`curl` desde fuera) son tres preguntas distintas.

---

## G

### getent
**Categoría:** Linux / Resolución de nombres
**Definición:** Comando que consulta las bases del sistema (hosts, passwd, group) siguiendo la configuración NSS, igual que lo hacen los programas. Para nombres revisa primero `/etc/hosts` y después el DNS.
**En el curso:** `getent hosts api.ejemplo.test` resolvió desde `/etc/hosts` mientras `dig` no devolvía nada.

---

## I

### Interfaz de loopback
**Categoría:** Redes
**Definición:** Interfaz virtual `lo`, con la dirección 127.0.0.1 en IPv4 y ::1 en IPv6, que solo comunica a la máquina consigo misma. Lo que escucha ahí no es alcanzable desde la red.
**En el curso:** La API escucha en `127.0.0.1:8080`: solo Nginx, en la misma VM, puede hablarle.

---

## L

### Llave pública y llave privada
**Categoría:** SSH / Criptografía
**Definición:** Par de llaves relacionadas matemáticamente. La privada firma y nunca sale de tu máquina; la pública se copia a los servidores y solo sirve para verificar esas firmas. Robar la pública no da acceso.
**En el curso:** La privada se queda en la laptop con permisos 600; la pública va a `authorized_keys` en la VM.

---

## N

### Nginx
**Categoría:** Servidores web
**Definición:** Servidor web y proxy inverso de alto rendimiento. Recibe conexiones de los clientes, sirve archivos o las reenvía a una aplicación, y registra cada petición en su log de acceso.
**En el curso:** Es lo único publicado en el 80; le pasa todo a la API en 127.0.0.1:8080 y escribe el log con el formato `tiempos`.

---

## P

### Perfil de aplicación (UFW)
**Categoría:** Firewall / UFW
**Definición:** Nombre que agrupa los puertos de un programa, definido por su paquete en `/etc/ufw/applications.d/`. Permite escribir `ufw allow "Nginx HTTP"` en lugar del puerto.
**En el curso:** OpenSSH (22), Nginx HTTP (80), Nginx HTTPS (443) y Nginx Full (80 y 443). Si SSH cambia de puerto, el perfil OpenSSH ya no lo cubre.

---

### Proxy inverso
**Categoría:** Arquitectura / Redes
**Definición:** Servidor que recibe las peticiones de los clientes en nombre de una o más aplicaciones internas y se las reenvía. Concentra TLS, límites y logs, y oculta cómo está organizada la parte de atrás.
**En el curso:** Nginx delante de `api-lab`. La API ve todas las peticiones como si vinieran de 127.0.0.1.

---

## S

### ss
**Categoría:** Linux / Redes
**Definición:** Herramienta que muestra los sockets del sistema. Con `-ltnp` lista los puertos TCP en escucha con su proceso; con `-lunp`, los UDP. Reemplaza a `netstat`.
**En el curso:** Es el inventario: la columna de dirección local dice quién puede llegar a cada puerto.

---

### ssh-agent
**Categoría:** SSH
**Definición:** Proceso que guarda en memoria las llaves privadas ya desbloqueadas, para no teclear la frase de paso en cada conexión. `ssh-add` le entrega una llave.
**En el curso:** Permite poner frase de paso a la llave sin volverla incómoda.

---

### sshd -T
**Categoría:** SSH / Configuración
**Definición:** Opción de sshd que imprime la configuración efectiva, ya combinados el archivo principal y los drop-ins. A diferencia de `sshd -t`, que solo revisa la sintaxis, dice lo que de verdad va a aplicar.
**En el curso:** Después del drop-in `10-endurecer.conf`, `sshd -T` debe mostrar `passwordauthentication no`.

---

### Stub resolver (127.0.0.53)
**Categoría:** Redes / DNS
**Definición:** Resolutor local de systemd-resolved que escucha en 127.0.0.53. Las aplicaciones le preguntan a él y él reenvía a los servidores DNS reales de cada interfaz, con caché.
**En el curso:** `/etc/resolv.conf` dice `nameserver 127.0.0.53`; los DNS de verdad se ven con `resolvectl status`.

---

### Superficie de ataque
**Categoría:** Seguridad
**Definición:** Conjunto de puntos por los que un atacante puede interactuar con un sistema: puertos abiertos, servicios, formularios, cuentas. Reducirla es quitar lo que no hace falta, no solo filtrarlo.
**En el curso:** Todo lo que escucha en `0.0.0.0` o `[::]` es superficie, se haya planeado o no.

---

## T

### Timeout frente a rechazo
**Categoría:** Redes / Diagnóstico
**Definición:** Dos formas de que una conexión no se logre. En el rechazo, la máquina contesta de inmediato que nadie escucha (curl sale con 7). En el timeout, nadie contesta porque algo descartó el paquete (curl sale con 28 al llegar al tope).
**En el curso:** Con UFW en `deny`, un puerto cerrado da timeout desde fuera.

---

## U

### UFW
**Categoría:** Firewall / Ubuntu
**Definición:** Uncomplicated Firewall: interfaz de Ubuntu que traduce reglas sencillas (`allow`, `deny`, `limit`) a iptables o nftables. Por omisión, niega lo entrante y permite lo saliente, y crea cada regla en IPv4 y en IPv6.
**En el curso:** `ufw delete N` borra solo una de las dos versiones de una regla; `ufw delete allow <regla>` borra ambas.

---

### ufw limit
**Categoría:** Firewall / UFW
**Definición:** Regla que permite un puerto pero niega a la dirección que intente 6 o más conexiones en 30 segundos. Frena la fuerza bruta simple.
**En el curso:** `sudo ufw limit OpenSSH`. No sustituye a las llaves ni a fail2ban.

---

## X

### X-Forwarded-For
**Categoría:** HTTP / Proxies
**Definición:** Cabecera en la que un proxy agrega la IP del cliente que le habló. La aplicación de atrás la usa para conocer la IP real, siempre que confíe en quien la escribió.
**En el curso:** Nginx la escribe con `$proxy_add_x_forwarded_for`. La API debe aceptarla solo del proxy conocido.
