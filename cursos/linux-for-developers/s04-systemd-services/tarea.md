# Tarea - S04 - Servicios con systemd

**Linux para desarrolladores**
**Tiempo estimado:** 4 horas

---

## Parte 1 - Lecturas (2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/20-systemd.md` | Todo el documento, con su VM abierta | 60 min |
| `dev-notes/10-cloud/20-systemd.md` | Releer: "Timers en lugar de cron", "Caso real: la limpieza nocturna de los runners" y "El journal" | 25 min |
| `dev-notes/10-cloud/03-linux-nginx.md` | Sección "Gestión de procesos y servicios (systemd)": compárenla con la unit de la práctica y anoten qué le falta | 15 min |

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *systemd for Linux SysAdmins* (David Both, Apress) | Cap. 2 - El arranque de Linux; cap. 3 - El arranque con systemd; cap. 4 - Administrar el arranque; cap. 5 - Units con systemctl; cap. 8 - Timers; cap. 9 - El journal; cap. 11 - Recursos con cgroups; cap. 14 - Sacarle más al journal |

---

## Parte 2 - Ejercicio (2 horas)

### Un timer que corra su respaldo de la S03 y deje rastro en el journal

El `respaldo.sh` de la S03 ya avisa cuando falla. Ahora tiene que correr **solo**, todos los días, y su resultado tiene que quedar donde cualquiera del equipo lo pueda consultar.

#### 1. El servicio

Escriban `/etc/systemd/system/respaldo-api-lab.service`:

- `Type=oneshot`: corre, termina y su código de salida es el resultado.
- `ExecStart=/usr/local/bin/respaldo.sh /var/lib/api-lab /var/backups/api-lab`.
- `SyslogIdentifier=respaldo-api-lab`, para que sus líneas se encuentren con `journalctl -t` o con `-u`.
- `Nice=` e `IOSchedulingClass=` para que el respaldo no le quite CPU ni disco a la API.
- Endurecimiento: `ProtectSystem=strict` y `ReadWritePaths=` **solo** con la carpeta de respaldos.

**Trampa conocida:** con `ProtectSystem=strict`, la carpeta de `ReadWritePaths=` tiene que existir **antes** de arrancar. Si no existe, la unit no llega ni a correr el script: el journal dice `Failed to set up mount namespacing` y el código es `226/NAMESPACE`. Créenla una vez con `sudo install -d -m 750 /var/backups/api-lab`.

#### 2. El timer

Escriban `/etc/systemd/system/respaldo-api-lab.timer`:

- Una hora fija con `OnCalendar=`, comprobada con `systemd-analyze calendar`.
- `RandomizedDelaySec=` de unos minutos.
- `Persistent=` con el valor que ustedes decidan, y **un comentario en la unit que diga por qué**. Recuerden el caso de los runners: allá `Persistent=false` era lo correcto. ¿Y para un respaldo?
- `[Install]` con `WantedBy=timers.target`. Se habilita el **timer**.

#### 3. Comprobarlo

```bash
sudo systemd-analyze verify /etc/systemd/system/respaldo-api-lab.service /etc/systemd/system/respaldo-api-lab.timer
sudo systemctl daemon-reload
sudo systemctl enable --now respaldo-api-lab.timer
systemctl list-timers respaldo-api-lab.timer --no-pager
sudo systemctl start respaldo-api-lab.service          # una corrida ahora, sin esperar
systemctl status respaldo-api-lab.service --no-pager
sudo journalctl -u respaldo-api-lab --no-pager
sudo ls -l /var/backups/api-lab
```

#### 4. Verlo fallar

Sin tocar la unit original, provoquen una corrida que falle con un drop-in temporal que cambie el origen por una carpeta vacía:

```bash
sudo mkdir -p /var/lib/vacio /etc/systemd/system/respaldo-api-lab.service.d
printf '[Service]\nExecStart=\nExecStart=/usr/local/bin/respaldo.sh /var/lib/vacio /var/backups/api-lab\n' \
  | sudo tee /etc/systemd/system/respaldo-api-lab.service.d/prueba-falla.conf > /dev/null
sudo systemctl daemon-reload
sudo systemctl start respaldo-api-lab.service; echo "salida: $?"
systemctl status respaldo-api-lab.service --no-pager
systemctl --failed --no-pager
sudo journalctl -u respaldo-api-lab -n 6 --no-pager
```

El `ExecStart=` vacío borra el comando de la unit original antes de poner el nuevo. Deben ver `Active: failed (Result: exit-code)`, `status=1/FAILURE`, la línea `ERROR: el origen /var/lib/vacio está vacío` del script, y la unit en `systemctl --failed`.

Al terminar, **quiten la prueba** y dejen todo limpio:

```bash
sudo rm /etc/systemd/system/respaldo-api-lab.service.d/prueba-falla.conf
sudo systemctl daemon-reload
sudo systemctl reset-failed respaldo-api-lab.service
```

#### 5. Dos preguntas que contestan por escrito

1. `systemctl list-timers` les muestra `LAST` de la corrida fallida. ¿Les dice que falló? ¿Qué comando sí lo dice?
2. `sudo journalctl -u respaldo-api-lab -p err` muestra la línea de systemd, pero no el `ERROR` del script. ¿Por qué? (Pista: con qué prioridad llega al journal lo que un servicio escribe en stderr.)

### Formato de la entrega

Un archivo `tarea-s04.md` en el repositorio del equipo (o un PR) con:

```markdown
## Las units
(respaldo-api-lab.service y respaldo-api-lab.timer completos, con el comentario de Persistent)

## verify y list-timers
(salidas)

## Corrida buena
(status y journal)

## Corrida fallida
(status, --failed y journal)

## Respuestas
1. ...
2. ...
```

---

## Criterio de aceptación

- [ ] `systemd-analyze verify` no reporta nada en las dos units.
- [ ] El timer está habilitado y aparece en `list-timers` con su próximo `NEXT`.
- [ ] La corrida buena deja un `.tar.gz` nuevo en `/var/backups/api-lab` y su rastro en el journal.
- [ ] La corrida fallida queda como `failed` en `systemctl status` y en `systemctl --failed`, con el mensaje del script en el journal.
- [ ] El comentario sobre `Persistent=` explica la decisión para **este** respaldo, no repite la definición.
- [ ] El drop-in de prueba se quitó y la unit quedó como antes (`systemctl cat` ya no lo muestra).
- [ ] Las dos preguntas están contestadas con la salida que las demuestra.

---

## Preguntas frecuentes

**¿Puedo usar cron en lugar del timer?**
No en esta tarea. Si quieren compararlos, agreguen una nota con lo que perderían: dónde queda la salida, qué pasa si la corrida anterior sigue y qué pasa si la VM estaba apagada.

**¿El servicio necesita `[Install]`?**
No. Lo activa el timer; lo que se habilita es el `.timer`. Por eso `systemctl status` del servicio dice `static`.

**El respaldo corre como root. ¿No es mucho?**
Es una buena pregunta para la evidencia. Necesita leer `/var/lib/api-lab` y escribir en `/var/backups/api-lab`. Si proponen otro usuario, demuestren que puede hacer las dos cosas y nada más.

---

## Preparación para la S05

- Traigan la API como servicio, sobreviviendo al reinicio, y el timer de respaldo funcionando.
- Corran `sudo ss -ltnp` en su VM y guarden la salida: es el "antes" del inventario de puertos de la S05.
