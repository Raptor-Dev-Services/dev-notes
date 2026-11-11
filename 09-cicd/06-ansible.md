# 06 - Ansible: configurar lo que se creó

> Fuente: *Ansible: Up and Running* (Meijer, Hochstein y Moser, 2022): caps. 3 Playbooks: A Beginning, 4 Inventory: Describing Your Servers, 5 Variables and Facts, 8 Debugging Ansible Playbooks, 9 Roles: Scaling Up Your Playbooks, 14 Quality Assurance with Molecule y 24 Best Practices. Las salidas de esta nota se obtuvieron con ansible-core 2.22.0 y ansible-lint 26.11.0; el curso instala ansible-core 2.21.6 porque corre en Python 3.12 (a noviembre de 2026); las fuentes están en Referencias.

## Problema que resuelve

[04-terraform.md](04-terraform.md) y [05-terraform-modulos-y-estado.md](05-terraform-modulos-y-estado.md) crean el servidor. Después de crearlo hay que dejarlo como se necesita: usuarios, SSH solo con llave, firewall, paquetes, configuración de la aplicación. Hacerlo a mano por SSH funciona para una máquina y falla en la segunda: nadie recuerda qué se tocó, y dos servidores "iguales" ya no lo son.

Ansible describe el estado deseado del servidor en archivos YAML y lo aplica por SSH, **sin instalar agente** en el servidor. Lo importante no es que ejecute comandos, sino que cada tarea sea **idempotente**: correrla una vez o diez deja el servidor igual. El glosario oficial lo define así: una operación es idempotente si el resultado de hacerla una vez es exactamente el mismo que el de hacerla repetidamente, sin acciones intermedias.

[10-cloud/03-linux-nginx.md](../10-cloud/03-linux-nginx.md) trae los comandos de Linux que cada tarea de esta nota automatiza.

---

## Las piezas

| Pieza | Qué es |
|---|---|
| Nodo de control | La máquina donde corre `ansible-playbook` (tu laptop o el runner de CI) |
| Inventario | La lista de servidores, con grupos y variables |
| Módulo | La unidad de trabajo: `ansible.builtin.file`, `ansible.builtin.copy`, `ansible.builtin.lineinfile`, `ansible.builtin.service`... Se llama con su nombre completo (FQCN) |
| Tarea | Una llamada a un módulo con sus argumentos |
| Play | Un conjunto de tareas aplicado a un grupo de servidores |
| Playbook | Un archivo YAML con uno o más plays |
| Handler | Tarea que solo corre si otra le avisó con `notify` y reportó cambio |
| Rol | Una carpeta con estructura fija que empaqueta tareas, variables, plantillas y handlers |
| Facts | Datos que Ansible descubre del servidor al empezar (`gather_facts`) |

El nodo de control necesita una versión de Python que acepte la versión de ansible-core. Según PyPI, ansible-core 2.22.0 (publicado el 2026-11-02) requiere Python 3.13 o superior, y la VM Ubuntu 24.04 del curso trae Python 3.12: ahí `pip install ansible-core==2.22.0` no encuentra una distribución compatible, y `ansible-core==2.21.6` sí se instala. Por eso el curso usa 2.21.6. Antes de instalar otra versión, revisa la guía oficial de instalación y la tabla de soporte de Ansible.

---

## Inventario

Un inventario en formato INI (`inventario.ini`):

```ini
[laboratorio]
vm-lab ansible_connection=local

[laboratorio:vars]
puerto_app=8080
```

`ansible_connection=local` ejecuta en la misma máquina sin SSH: sirve para aprender sin servidor. Con una VM real se escribe su dirección (`vm-lab ansible_host=203.0.113.10 ansible_user=deploy`) y Ansible entra por SSH. Las variables pueden ir en el propio inventario, o en las carpetas `group_vars/` y `host_vars/` junto a él. Los grupos permiten apuntar un play a `laboratorio` o `produccion` sin tocar las tareas.

---

## Un playbook

```yaml
---
- name: Configurar la carpeta de la aplicacion
  hosts: laboratorio
  gather_facts: true
  vars:
    carpeta: /tmp/app
  tasks:
    - name: La carpeta existe
      ansible.builtin.file:
        path: "{{ carpeta }}"
        state: directory
        mode: "0750"

    - name: El archivo de configuracion esta presente
      ansible.builtin.copy:
        dest: "{{ carpeta }}/app.conf"
        content: |
          puerto={{ puerto_app }}
          sistema={{ ansible_facts['distribution'] }}
        mode: "0640"
      notify: Avisar del cambio

  handlers:
    - name: Avisar del cambio
      ansible.builtin.debug:
        msg: "La configuracion cambio"
```

Los nombres de las tareas describen el **estado** ("la carpeta existe"), no la acción ("crear carpeta"): así se leen como una lista de condiciones que el servidor debe cumplir.

```bash
ansible-playbook -i inventario.ini sitio.yml
```

Primera corrida (salida real, resumida; con la carpeta apuntando a un directorio de pruebas):

```
TASK [La carpeta existe] *******************************************************
changed: [vm-lab]

TASK [El archivo de configuracion esta presente] *******************************
changed: [vm-lab]

RUNNING HANDLER [Avisar del cambio] ********************************************
ok: [vm-lab] => {
    "msg": "La configuracion cambio"
}

PLAY RECAP *********************************************************************
vm-lab : ok=4    changed=2    unreachable=0    failed=0    skipped=0    rescued=0    ignored=0
```

Segunda corrida, sin tocar nada:

```
PLAY RECAP *********************************************************************
vm-lab : ok=3    changed=0    unreachable=0    failed=0    skipped=0    rescued=0    ignored=0
```

`changed=0` es la prueba de idempotencia. El handler no corrió: solo corre cuando una tarea que lo notifica reporta cambio, y por omisión al final del play. Si necesitas que corra antes, la tarea `meta: flush_handlers` ejecuta los notificados hasta ese punto.

### Modo de verificación

`--check` simula sin cambiar nada y `--diff` muestra qué cambiaría. Al cambiar `puerto_app` de 8080 a 9090 en el inventario:

```bash
ansible-playbook -i inventario.ini sitio.yml --check --diff
```

```
--- before: .../app.conf
+++ after: .../app.conf
@@ -1,2 +1,2 @@
-puerto=8080
+puerto=9090
 sistema=Ubuntu

changed: [vm-lab]
```

Y el archivo sigue diciendo `puerto=8080`: no se tocó. Es el equivalente de `tofu plan` para la configuración. No todos los módulos lo soportan igual; `command` y `shell` no pueden predecir su efecto.

---

## Variables y facts

Ansible junta variables de muchos lugares y tiene un orden de precedencia fijo y largo (la lista completa, de la menor a la mayor, está en la documentación oficial). Lo útil en el día a día:

- Los **defaults de un rol** (`defaults/main.yml`) son los de menor precedencia: cualquier otra fuente los sobrescribe. Ahí van los valores que el usuario del rol debe poder cambiar.
- Las `vars/` de un rol tienen precedencia alta: son constantes del rol, no se espera que se cambien desde fuera.
- Las variables pasadas con `-e` en la línea de comandos ganan sobre las demás.

Los **facts** son variables que Ansible descubre del servidor (sistema operativo, interfaces de red, memoria). Se leen con `ansible_facts['distribution']` y se pueden apagar con `gather_facts: false` cuando no se usan, para ahorrar tiempo.

---

## Idempotencia: el error típico

Los módulos como `file`, `copy`, `lineinfile`, `user` o `service` comparan el estado actual con el deseado y solo actúan si difieren. `command` y `shell` no saben qué hace tu comando: **siempre reportan changed**. Salida real de correr dos veces un playbook con `shell: echo "linea" >> archivo`:

```
changed: [vm-lab]     # primera corrida
changed: [vm-lab]     # segunda corrida: añadió otra línea y volvió a reportar cambio
```

El archivo ahora tiene la línea duplicada. Con `lineinfile` (que asegura que la línea exista una sola vez):

```yaml
- name: Asegura la linea con lineinfile
  ansible.builtin.lineinfile:
    path: /tmp/app/bitacora.txt
    line: linea
    create: true
    mode: "0640"
```

```
changed: [vm-lab]     # primera corrida: la creó
ok: [vm-lab]          # segunda corrida: ya estaba, sin cambio
```

Si de verdad necesitas `command` o `shell`, dile cuándo hay cambio (`changed_when`) o cuándo ya no hace falta correrlo (`creates:`/`removes:`).

---

## Roles

Cuando un playbook crece, las tareas se empaquetan en un **rol**. Un rol es una carpeta con subcarpetas de nombre fijo, y puedes omitir las que no uses:

```text
roles/endurecer/
├── tasks/main.yml        las tareas
├── handlers/main.yml     los handlers
├── defaults/main.yml     variables con la menor precedencia (las que el usuario cambia)
├── vars/main.yml         variables de alta precedencia (constantes del rol)
├── files/                archivos que se copian tal cual
├── templates/            plantillas Jinja2 (.j2)
└── meta/main.yml         metadatos y dependencias
```

`ansible-galaxy role init endurecer` crea el esqueleto. El playbook queda corto:

```yaml
- hosts: laboratorio
  become: true
  roles:
    - endurecer
```

Un rol bien hecho se prueba aparte, sin el resto del playbook: de eso trata la sesión 5 (Molecule y CI).

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Módulos específicos (`file`, `user`, `lineinfile`, `service`) | `shell` para todo lo que ya tiene módulo |
| Nombres de tareas que describen el estado | Tareas sin nombre o con verbos de acción |
| `--check --diff` antes de aplicar a un servidor que importa | Aplicar sin ver qué cambiaría |
| Un rol por responsabilidad, con `defaults/` documentados | Un playbook de 500 líneas con todo mezclado |
| Comprobar `changed=0` en la segunda corrida | Dar por bueno un playbook que solo corrió una vez |
| Secretos con `ansible-vault` o un gestor externo | Contraseñas en texto plano en el inventario |

---

## Referencias

Consultadas a noviembre de 2026.

- Ansible, "Roles": https://docs.ansible.com/ansible/latest/playbook_guide/playbooks_reuse_roles.html
- Ansible, "Handlers: running operations on change": https://docs.ansible.com/ansible/latest/playbook_guide/playbooks_handlers.html
- Ansible, "Using variables" (precedencia): https://docs.ansible.com/ansible/latest/playbook_guide/playbooks_variables.html
- Ansible, glosario (Idempotency): https://docs.ansible.com/ansible/latest/reference_appendices/glossary.html
- PyPI, ansible-core 2.22.0 (requiere Python >= 3.13, publicado el 2026-11-02): https://pypi.org/project/ansible-core/
- Meijer, B., Hochstein, L. y Moser, R. *Ansible: Up and Running*, 3.ª ed. (O'Reilly, 2022), caps. 3 a 5, 8, 9, 14 y 24.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Ansible | Herramienta que configura servidores a partir de archivos YAML, por SSH y sin agente |
| Nodo de control | Máquina desde la que se ejecuta Ansible |
| Inventario | Lista de servidores, con grupos y variables |
| Módulo | Unidad de trabajo que compara el estado actual con el deseado y actúa solo si difieren |
| FQCN | Nombre completo de un módulo, como `ansible.builtin.copy` |
| Playbook | Archivo YAML con uno o más plays |
| Handler | Tarea que corre al final del play solo si fue notificada por un cambio |
| Idempotente | Que correrlo una vez o muchas deja el mismo resultado |
| Facts | Datos del servidor que Ansible descubre al empezar |
| Rol | Carpeta con estructura fija que empaqueta tareas, variables, plantillas y handlers |
| `--check` / `--diff` | Simular sin cambiar, y mostrar qué cambiaría |
| `changed_when` | Directiva que decide cuándo una tarea cuenta como cambio |

---

*Rogelio Arriaga Gonzalez*
