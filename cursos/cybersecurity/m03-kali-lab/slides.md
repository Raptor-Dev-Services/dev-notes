<!-- _class: lead -->

# Módulo 3 - Preparar el Laboratorio Kali

### Ciberseguridad

**Rogelio Arriaga - Raptor Dev Services**

---

## Qué es Kali Linux

Una distribución de Linux basada en Debian, mantenida por Offensive Security, con cientos de herramientas preinstaladas o fácilmente instalables para pruebas de seguridad.

En este curso la usamos **solo dentro de tu laboratorio**: máquinas virtuales propias, nunca contra sistemas de terceros.

---

<!-- _class: lead -->

# Parte 1
## El laboratorio: qué necesitas

---

## Arquitectura mínima del laboratorio

- Una máquina virtual con **Kali Linux** (el "atacante").
- Una o más máquinas virtuales "víctima" que tú mismo configuras (por ejemplo, una Ubuntu Server con SSH habilitado).
- Una red virtual aislada entre ellas, sin salida a tu red doméstica ni a internet pública durante las prácticas ofensivas.

---

## Por qué una red aislada

Si tu laboratorio comparte red con tu router doméstico o con otros dispositivos, un escaneo o un ataque de fuerza bruta puede alcanzar equipos que no son tuyos, incluso sin intención.

Usa un adaptador de red tipo "solo anfitrión" o una red interna de tu hipervisor, nunca modo puente hacia tu red real.

---

<!-- _class: lead -->

# Parte 2
## Preparar Kali: actualización y herramientas

---

## Paso 1: actualizar el sistema

```bash
sudo apt update
sudo apt upgrade -y
sudo apt autoremove -y
```

Siempre se actualiza antes de instalar herramientas nuevas, para evitar conflictos de versiones.

---

## Paso 2: metapaquetes de herramientas

```bash
sudo apt install -y \
  kali-tools-information-gathering \
  kali-tools-passwords \
  kali-tools-web \
  seclists
```

- `kali-tools-information-gathering` trae Nmap y herramientas de reconocimiento.
- `kali-tools-passwords` trae Hydra, John, ncrack.
- `seclists` trae la colección de diccionarios más usada del gremio.

---

## Paso 3: preparar los diccionarios (wordlists)

```bash
cd /usr/share/wordlists/
sudo gunzip rockyou.txt.gz
ln -s /usr/share/wordlists/rockyou.txt ~/rockyou.txt
```

`rockyou.txt` es una de las listas de contraseñas más usadas en pruebas de fuerza bruta de laboratorio: viene comprimida para ahorrar espacio.

---

## Mapa de dónde quedó todo

| Tipo de lista | Ubicación | Uso principal |
|---|---|---|
| Diccionarios generales | `/usr/share/seclists/` | Usuarios, credenciales, fuzzing |
| RockYou | `~/rockyou.txt` (enlace) | Fuerza bruta de contraseñas |

---

<!-- _class: lead -->

# Parte 3
## Buenas prácticas antes de empezar

---

## Snapshots antes de cada práctica

Toma una instantánea (*snapshot*) de tus máquinas virtuales antes de empezar cada práctica. Si algo sale mal (una herramienta corrompe el sistema, una configuración se rompe), restauras en segundos.

---

## Checklist antes de la primera práctica

- [ ] Kali actualizado (`apt update && apt upgrade`).
- [ ] Metapaquetes de reconocimiento, contraseñas y web instalados.
- [ ] `rockyou.txt` descomprimido y accesible.
- [ ] Máquina(s) víctima configuradas, en red aislada.
- [ ] Snapshot tomado de todas las máquinas virtuales.

---

<!-- _class: lead -->

# Cierre

## Lo que te llevas de este módulo

- Un laboratorio aislado, con atacante y víctima bajo tu control
- Kali actualizado con las herramientas de reconocimiento y contraseñas instaladas
- El hábito de tomar snapshots antes de cada práctica

**Siguiente módulo:** Reconocimiento con Nmap.
