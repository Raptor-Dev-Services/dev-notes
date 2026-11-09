# Tarea - S05 - Red, firewall y SSH

**Linux para desarrolladores**
**Tiempo estimado:** 3 horas

---

## Parte 1 - Lecturas (1.5 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/03-linux-nginx.md` | Nginx como reverse proxy, SSL con Certbot (Let's Encrypt), Firewall con UFW | 30 min |
| `dev-notes/10-cloud/08-zero-trust.md` | El modelo perimetral vs Zero Trust, Least Privilege, Segmentación de red | 40 min |

Mientras leen la nota de Zero Trust, piensen en su VM: ¿qué de lo que hicieron hoy es "perímetro" y qué es "no confiar ni en la red interna"?

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Learning Modern Linux* (Michael Hausenblas, O'Reilly, 2022) | Cap. 7 - Networking: la pila TCP/IP, puertos, sockets, DNS y SSH |
| *Practical Linux System Administration* (Kenneth Hess, O'Reilly, 2023) | Cap. 5 - Connecting to a Network. Cap. 13 - Securing Your System |
| *systemd for Linux SysAdmins* (David Both, Apress) | Cap. 10 - firewalld (Ubuntu usa UFW: lean el capítulo por los conceptos de zonas y servicios). Cap. 12 - systemd-resolved |

---

## Parte 2 - Ejercicio: inventario de puertos antes y después (1.5 horas)

Con los archivos `puertos-antes.txt` y `puertos-despues.txt` de la práctica, escriban `docs/inventario-puertos.md` en el repo de su equipo con:

### 1. Una tabla por momento (antes y después)

| Protocolo | Dirección:puerto | Proceso (unit) | ¿Alcanzable desde fuera? | Por qué existe |
|---|---|---|---|---|
| TCP | `0.0.0.0:22` | sshd (`ssh.socket`) | Sí, con llave | Administración remota |
| TCP | `127.0.0.1:8080` | python3 (`api-lab.service`) | No | La API; solo Nginx le habla |

La columna "¿Alcanzable desde fuera?" **no se llena mirando `ss`**: se llena con un `curl` o un `ssh` desde otra máquina. Pongan el comando y su salida debajo de la tabla.

### 2. El porqué de cada cambio

Para cada línea que aparece en el `diff`, un párrafo corto: qué cambió, quién lo cambió (ustedes, un paquete, Docker) y si lo dejan así o lo cierran.

### 3. Lo que se quedó abierto a propósito

Para cada puerto que sigue alcanzable desde fuera, digan qué lo protege además del firewall (llave de SSH, `limit`, el proxy, el `deny` a archivos ocultos...). Si la única protección es UFW, díganlo y propongan una segunda capa.

### 4. La prueba de Docker

La salida del contenedor publicado en `0.0.0.0` contestando desde fuera, y la del arreglo en `127.0.0.1`. Tres líneas que expliquen por qué UFW no lo vio.

---

## Criterio de aceptación

Su tarea se considera completa cuando:

- [ ] Las tablas de antes y después incluyen TCP **y** UDP.
- [ ] Cada fila tiene el proceso y la unit de systemd que lo levanta.
- [ ] La columna "alcanzable" está respaldada por pruebas desde otra máquina, con la salida pegada.
- [ ] Cada línea del `diff` tiene su explicación.
- [ ] Ningún puerto en `0.0.0.0` o `[::]` se quedó sin un porqué.
- [ ] La prueba de Docker está antes y después del arreglo.
- [ ] No hay IP reales en el documento: usen `<IP_DE_SU_VM>` al publicarlo.

---

## Preguntas frecuentes

**¿Cuento los puertos de 127.0.0.1?**
Sí. No son alcanzables desde fuera, pero son parte del inventario: el día que alguien cambie la dirección a `0.0.0.0`, el `diff` lo va a mostrar.

**Me apareció un puerto que no sé qué es.**
`sudo ss -ltnp 'sport = :<PUERTO>'` les da el PID, y `systemctl status <PID>` la unit. Si no tiene unit, `ps -o user,pid,cmd -p <PID>`. Un puerto sin explicación es un hallazgo, no un detalle.

**¿Tengo que dejar Docker instalado?**
No. Si lo desinstalan, que quede en el inventario de después y en el `diff`.

---

## Preparación para S06

En la sesión 6 otro equipo va a romper su VM a escondidas. Lleguen con:
- La VM con todo lo de hoy funcionando (API, Nginx, UFW).
- Su inventario de puertos a la mano: les va a servir para notar lo que no estaba.
- Una pregunta concreta sobre algo de red que no les quedó claro.
