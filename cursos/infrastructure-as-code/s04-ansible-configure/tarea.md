# Tarea - S04 - Ansible: configurar lo que se creó

**Infraestructura como código**
**Tiempo estimado:** 4 a 5 horas

---

## Parte 1 - Lecturas (1.5 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/09-cicd/06-ansible.md` | Completa | 40 min |
| `dev-notes/10-cloud/03-linux-nginx.md` | Usuarios, permisos y firewall | 20 min |
| Documentación de Ansible, "Using variables" | Variable precedence | 15 min |
| Documentación de Ansible, "Roles" | Role directory structure y Using roles | 15 min |

Lee con la VM abierta: cada ejemplo se corre, no se mira.

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Ansible: Up and Running* (Meijer, Hochstein y Moser, O'Reilly, 2022) | Cap. 3 - Playbooks: A Beginning; Cap. 4 - Inventory: Describing Your Servers; Cap. 5 - Variables and Facts; Cap. 9 - Roles: Scaling Up Your Playbooks |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: el checklist completo de la VM en Ansible (2.5 a 3 horas)

En Titulación endurecieron su VM con un checklist a mano. Conviértelo en código: **extiende el rol `endurecer`** de la práctica (o crea un rol hermano `base_vm`) para que un solo `ansible-playbook site.yml` deje una VM nueva con **todos** estos controles:

| Control | Qué debe hacer el rol |
|---|---|
| Usuario de despliegue | Usuario `deploy` sin contraseña, con tu llave pública en `authorized_keys`, sin sudo |
| SSH | Solo llave, sin root, `MaxAuthTries 3`, archivo con prefijo que gane sobre `50-cloud-init.conf` y **assert** de `sshd -T` |
| Firewall | UFW con entrada denegada, SSH permitido antes de activar y los puertos declarados en `group_vars` |
| fail2ban | Jaula `sshd` con `maxretry`, `findtime` y `bantime` en variables |
| Paquetes | Actualizaciones de seguridad automáticas (`unattended-upgrades` instalado y habilitado) |
| Hora | Zona horaria y sincronización de reloj declaradas (por ejemplo `chrony`) |
| Verificación | Una `assert` por control: el rol no termina "bien" si algo quedó a medias |

### Reglas del ejercicio

1. **Idempotente:** la segunda corrida reporta `changed=0`. Entrega la salida.
2. **Sin `command` ni `shell`** salvo para **leer** (con `changed_when: false`). Si necesitas uno, justifícalo en una línea del README.
3. **FQCN** en todos los módulos y colecciones fijadas en `requirements.yml`.
4. **Variables con prefijo** del rol, con valores por omisión en `defaults/` y las diferencias por entorno en `group_vars/` o `host_vars/`.
5. **Un "verlo fallar" por control crítico:** rompe a propósito SSH, UFW y fail2ban (una variable mal puesta, un puerto faltante) y muestra qué mensaje da el rol o cuál comprobación lo detecta.
6. **Seguridad de la prueba:** no pruebes el firewall sin una segunda terminal o la consola de la VM abierta.

### Qué entregar

Un repositorio (o carpeta) con:

```
iac-lab/ansible/
├── ansible.cfg
├── requirements.yml
├── inventario/
│   ├── hosts.yml
│   └── group_vars/vms.yml
├── site.yml
├── roles/endurecer/        (o base_vm/)
│   ├── defaults/ handlers/ meta/ tasks/ templates/
│   └── README.md
└── evidencia-s04/
    ├── corrida-1.txt       PLAY RECAP de la primera corrida
    ├── corrida-2.txt       PLAY RECAP con changed=0
    ├── verlo-fallar.md     un fallo provocado por control, con la salida real
    └── checklist.md        tabla "control -> tarea del rol -> assert que lo comprueba"
```

El `README.md` del rol lleva: qué hace, variables (tabla con valor por omisión), cómo correrlo, qué **no** hace.

---

## Criterios de evaluación

| Criterio | Peso | Qué se revisa |
|---|---|---|
| Idempotencia | 25 % | La segunda corrida reporta `changed=0` en la evidencia, y el rol no usa `command`/`shell` sin declarar cuándo cambia |
| Cobertura del checklist | 20 % | Cada control de la tabla existe en el rol y tiene su `assert` |
| Seguridad del orden | 15 % | UFW permite SSH antes de activarse; el drop-in de SSH gana sobre `50-cloud-init.conf` y se comprueba con `sshd -T` |
| Variables y estructura | 15 % | Prefijos, `defaults/`, `group_vars`, tareas partidas por tema, handlers solo donde hacen falta |
| "Verlo fallar" | 15 % | Un fallo real por control crítico, con la salida pegada y la explicación de por qué ocurrió |
| Documentación | 10 % | README con tabla de variables, cómo correr y qué no hace |

---

## Para ir pensando

- ¿Qué pasaría si le das el rol a alguien con otra versión de Ubuntu? ¿Dónde lo dirías?
- Tu rol y tu checklist de Titulación: ¿cuál de los dos cree más tu equipo cuando alguien cambia algo a mano?
- La próxima sesión: ¿cómo probarías este rol en un servidor **limpio**, sin tocar el tuyo?
