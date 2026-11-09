# Práctica - Hydra contra tu laboratorio, y defenderlo con fail2ban

## Objetivo

Ejecutar un ataque de fuerza bruta controlado contra el SSH de tu máquina víctima de laboratorio, observar cómo se ve desde el lado defensivo, y aplicar una defensa real.

**Regla de esta práctica:** el objetivo es siempre tu propia máquina víctima de laboratorio (módulo 3), en tu red aislada. Nunca un sistema fuera de tu control.

## 1. Prepara tus listas de prueba

En Kali, crea listas pequeñas de usuarios y contraseñas de práctica (no uses datos reales de nadie):

```bash
cat > usuarios_practica.txt << 'EOF'
practica
invitado
EOF

cat > passwords_practica.txt << 'EOF'
ClaveDePractica1!
Practica2025!
123456
EOF
```

En tu máquina víctima, crea un usuario de prueba con una de esas contraseñas (por ejemplo `practica` / `ClaveDePractica1!`), para poder confirmar que Hydra sí encuentra la combinación correcta.

```bash
sudo adduser practica
```

## 2. Primer intento: ataque lento y controlado

```bash
hydra -L usuarios_practica.txt -P passwords_practica.txt \
  -t 1 -w 3 -V ssh://<IP_DE_TU_VICTIMA>
```

Observa en la salida (`-V`) cómo Hydra prueba cada combinación. Confirma que encuentra la combinación válida que configuraste.

## 3. Revisa el lado defensivo: los logs

En tu máquina víctima (no en Kali):

```bash
sudo tail -n 50 /var/log/auth.log
```

Identifica los intentos fallidos registrados y el que tuvo éxito.

## 4. Instala y activa fail2ban

```bash
sudo apt install -y fail2ban
sudo systemctl enable --now fail2ban
sudo fail2ban-client status sshd
```

## 5. Repite el ataque con fail2ban activo

```bash
hydra -L usuarios_practica.txt -P passwords_practica.txt \
  -t 1 -w 1 -V ssh://<IP_DE_TU_VICTIMA>
```

Después de varios intentos fallidos, verifica si tu IP de Kali quedó bloqueada:

```bash
sudo fail2ban-client status sshd
```

Documenta en cuántos intentos se activó el bloqueo.

## 6. Aplica una defensa adicional

Deshabilita el login por contraseña y deja solo autenticación por llave pública en tu máquina víctima (edita `/etc/ssh/sshd_config`, cambia `PasswordAuthentication` a `no`, y reinicia el servicio SSH). Confirma que, después del cambio, Hydra ya no puede completar un login por contraseña.

```bash
sudo systemctl restart ssh
```

> Antes de deshabilitar el login por contraseña, asegúrate de que tu llave SSH pública ya está configurada en esa máquina, o perderás el acceso.

## Checklist de la práctica

- [ ] Ataque de Hydra ejecutado solo contra tu máquina víctima de laboratorio.
- [ ] Confirmaste que Hydra encuentra la combinación correcta que configuraste.
- [ ] Revisaste los intentos fallidos en los logs de la máquina víctima.
- [ ] fail2ban activo y confirmaste el bloqueo de la IP atacante.
- [ ] Autenticación por llave pública configurada como defensa adicional.
