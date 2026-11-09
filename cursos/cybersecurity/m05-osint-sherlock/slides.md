<!-- _class: lead -->

# Módulo 5 - OSINT con Sherlock

### Ciberseguridad

**Rogelio Arriaga - Raptor Dev Services**

---

## Qué es OSINT

**Open Source Intelligence**: recopilar información disponible públicamente (redes sociales, registros públicos, metadatos) para construir un perfil de una persona o una organización.

Es una técnica **pasiva**: no interactúas directamente con el objetivo, solo consultas fuentes públicas.

---

<!-- _class: lead -->

# Parte 1
## Qué es Sherlock

---

## Sherlock: buscar un usuario en cientos de plataformas

Sherlock es una herramienta de código abierto que busca un nombre de usuario específico en más de 300 redes sociales y plataformas, para ver dónde está registrado.

Fuente: proyecto Sherlock, github.com/sherlock-project/sherlock.

---

## Instalación

```bash
cd ~/HackingTools
git clone https://github.com/sherlock-project/sherlock.git
cd sherlock
pip install -r requirements.txt
```

Se ejecuta siempre desde la carpeta donde se clonó.

---

<!-- _class: lead -->

# Parte 2
## Uso básico y avanzado

---

## Búsqueda básica

```bash
python3 sherlock mi_usuario_de_practica
```

El resultado es una lista de URLs: un hallazgo (link encontrado) indica que ese nombre de usuario existe en esa plataforma.

**En este módulo usa siempre tu propio nombre de usuario o uno inventado, nunca el de un compañero o un tercero.**

---

## Opciones útiles

| Opción | Ejemplo | Qué hace |
|---|---|---|
| `-o` / `--output` | `-o resultado.txt` | Guarda la salida en un archivo |
| `--site` | `--site GitHub` | Filtra la búsqueda a un sitio específico |
| `-L` / `--list` | `-L usuarios.txt` | Busca varios usuarios desde un archivo |
| `--print-all` | `--pa` | Muestra también los sitios donde no se encontró nada |

---

## Búsqueda masiva organizada

```bash
echo "cuenta_practica_1" > usuarios.txt
echo "cuenta_practica_2" >> usuarios.txt

python3 sherlock -L usuarios.txt --site GitHub --site Reddit --fo resultados_practica
```

Esto es mucho más rápido y específico que buscar en los 300+ sitios a la vez.

---

<!-- _class: lead -->

# Parte 3
## OSINT en el flujo de un pentest

---

## Cómo se conecta con el resto del curso

Sherlock es la primera pieza de un reconocimiento basado en identidad: confirma que un nombre de usuario existe y en dónde.

El siguiente paso típico (no de esta práctica, solo para entender el flujo completo) sería intentar autenticarse con ese nombre de usuario en un servicio específico usando **Hydra** (módulo 6), siempre con autorización.

---

## Ética en OSINT: solo cuentas propias o inventadas

OSINT es pasivo y legal cuando se usa sobre información pública y con fines legítimos (por ejemplo, que una persona audite su propia huella digital). Buscar sistemáticamente a una persona real sin su consentimiento, con fines de acoso o explotación, es otra cosa por completo.

En este curso, los ejemplos y las prácticas usan únicamente cuentas inventadas o propias del estudiante.

---

## Ejercicio: audita tu propia huella digital

Ejecuta Sherlock contra un nombre de usuario que tú mismo uses (o uno nuevo creado solo para la práctica) y documenta en cuántas plataformas aparece. Reflexiona: ¿alguna de esas coincidencias te sorprende?

---

<!-- _class: lead -->

# Cierre

## Lo que te llevas de este módulo

- OSINT es una técnica pasiva: recopila información pública, no ataca sistemas
- Sherlock confirma la existencia de un nombre de usuario en cientos de plataformas
- Usarlo de forma ética significa cuentas propias o inventadas, nunca acoso a terceros

**Siguiente módulo:** Fuerza bruta a SSH con Hydra, y cómo defenderse.
