<!-- _class: lead -->

# Módulo 6 - Fuerza Bruta a SSH con Hydra, y Cómo Defenderse

### Ciberseguridad

**Rogelio Arriaga - Raptor Dev Services**

---

## Qué es Hydra

Una herramienta modular de fuerza bruta diseñada para atacar múltiples servicios de red (SSH, FTP, HTTP, entre otros), probando combinaciones de usuario y contraseña.

**Regla del módulo:** se ejecuta únicamente contra la máquina víctima de tu propio laboratorio (módulo 3).

---

<!-- _class: lead -->

# Parte 1
## Sintaxis y ataque combinatorio

---

## Sintaxis básica

```bash
hydra [OPCIONES_DE_AUTENTICACION] [OPCIONES_DE_ATAQUE] [PROTOCOLO]://[OBJETIVO]
```

| Opción | Ejemplo | Uso |
|---|---|---|
| `-l` | `-l practica` | Un usuario único |
| `-L` | `-L usuarios.txt` | Una lista de usuarios |
| `-p` | `-p ClaveDePractica1!` | Una contraseña única |
| `-P` | `-P passwords.txt` | Una lista de contraseñas |

**Regla:** minúscula para un valor único, mayúscula para una lista.

---

## El ataque combinatorio

Con `-L` (lista de usuarios) y `-P` (lista de contraseñas), Hydra prueba **todas las combinaciones**: con 10 usuarios y 100 contraseñas, son 10 x 100 = 1,000 intentos.

Entender esto es clave para estimar cuánto tardará tu práctica.

---

<!-- _class: lead -->

# Parte 2
## Ataque controlado en laboratorio

---

## Comando de práctica contra SSH

```bash
hydra -L usuarios_practica.txt -P passwords_practica.txt \
  -t 1 -W 3 -V ssh://<IP_DE_TU_VICTIMA>
```

- `-t 1`: un solo hilo (evita saturar o bloquear el servicio).
- `-W 3`: pausa de 3 segundos entre conexiones de cada hilo (simula un ataque lento). No confundir con `-w`, que es el tiempo máximo de espera de la respuesta.
- `-V`: modo detallado, muestra cada combinación que se prueba.

---

## Por qué `-t` bajo y `-W` alto en el laboratorio

Valores de `-t` altos (muchos hilos simultáneos) saturan el servicio y disparan bloqueos tipo `fail2ban` más rápido. Para observar el comportamiento con claridad en clase, se usan pocos hilos y pausas entre conexiones (`-W`), no para "evadir detección" en un sistema real.

---

## Otros protocolos soportados

Mismo patrón de sintaxis para otros servicios: `ftp://`, `telnet://`, `http-post-form://` (este último con sintaxis adicional para el formulario).

Fuente: documentación de Hydra (THC-Hydra), github.com/vanhauser-thc/thc-hydra (`hydra -h`: "-w / -W TIME: wait time for a response / between connects per thread").

---

<!-- _class: lead -->

# Parte 3
## Defensa: cómo se detiene esto

---

## fail2ban: el primer control

`fail2ban` monitorea los logs de autenticación y bloquea temporalmente la IP de origen tras varios intentos fallidos consecutivos.

```bash
sudo apt install -y fail2ban
sudo systemctl enable --now fail2ban
```

Con `fail2ban` activo en tu máquina víctima, repite el ataque de la práctica y observa cómo se corta.

---

## Defensas adicionales en SSH

- **Autenticación por llave pública**, deshabilitando el login con contraseña (`PasswordAuthentication no` en `sshd_config`).
- **Cambiar el puerto por defecto** (no es seguridad real, pero reduce el ruido de escaneos automatizados).
- **Limitar los intentos** con `MaxAuthTries` en la configuración de SSH.
- **Restringir el acceso** por IP de origen cuando sea posible (firewall o `AllowUsers`).

---

## Qué revisar después de un ataque de fuerza bruta

Revisa `/var/log/auth.log` (Ubuntu/Debian) para ver los intentos fallidos registrados, y confirma en `fail2ban-client status sshd` qué IPs quedaron bloqueadas.

```bash
sudo fail2ban-client status sshd
```

---

<!-- _class: lead -->

# Cierre del curso

## Lo que te llevas de este módulo y del curso

- Hydra demuestra por qué una contraseña débil es una puerta abierta
- `fail2ban` y la autenticación por llave son defensas reales y medibles
- Cada técnica ofensiva vista en este curso (Nmap, Sherlock, Hydra) se practicó solo en laboratorio propio, con la autorización y la ética del módulo 1 como base de todo

**Fin del curso de Ciberseguridad.**
