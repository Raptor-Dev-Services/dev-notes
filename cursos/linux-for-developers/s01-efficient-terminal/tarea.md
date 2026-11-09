# Tarea - S01 - La terminal eficiente

**Linux para desarrolladores**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1.5 a 2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/03-linux-nginx.md` | Comandos esenciales de navegación | 15 min |
| `dev-notes/10-cloud/18-terminal-eficiente.md` | Lo que hace el shell antes de correr un comando; Entrada, salida y redirecciones | 30 min |
| `dev-notes/10-cloud/18-terminal-eficiente.md` | Tuberías: las herramientas que se encadenan; Preguntas a un log de Nginx en una línea | 30 min |
| `dev-notes/10-cloud/18-terminal-eficiente.md` | Historial: no volver a escribir; Moverse por el sistema de archivos; Errores que dan un número falso | 25 min |

Lee la nota 18 **con la VM abierta**: cada ejemplo se corre, no se mira.

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Efficient Linux at the Command Line* (Daniel J. Barrett, O'Reilly, 2022) | Cap. 1 - Combining Commands; Cap. 2 - Introducing the Shell; Cap. 3 - Rerunning Commands; Cap. 4 - Cruising the Filesystem |
| *Learning Modern Linux* (Michael Hausenblas, O'Reilly, 2022) | Cap. 3 - Shells and Scripting |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: tu hoja de atajos (1.5 a 2 horas)

Una hoja de atajos que te sirva **a ti** en un servidor a las 3 de la mañana. No una copia de la lámina: tus comandos, con ejemplos que corriste.

### Qué lleva

Un archivo `atajos-s01.md` con **al menos 25 entradas**, agrupadas en estas secciones:

| Sección | Mínimo de entradas | Ejemplos de lo que cabe |
|---------|--------------------|-------------------------|
| Expansión y comillas | 4 | `{a,b}`, `$( )`, comillas dobles frente a sencillas, `printf '[%s]\n'` |
| Redirecciones | 4 | `>`, `>>`, `2>&1` y su orden, `tee`, `sudo tee` |
| Tuberías | 8 | ordenar, contar y ordenar (`sort`, `uniq -c`, `sort -rn`), `awk` con arreglos, `cut`, `grep -c`, `sed`, `find -print0` con `xargs -0` |
| Historial | 4 | Ctrl-R, `!!`, `!$`, `^viejo^nuevo`, `HISTTIMEFORMAT` |
| Moverse | 3 | `cd -`, `pushd`/`popd`, `CDPATH`, `globstar` |
| Números falsos | 3 | un caso en que una tubería te dio un número equivocado y cómo lo detectaste |

### Formato de cada entrada

````markdown
### Contar valores de un campo
Para qué: saber qué valores trae una columna antes de filtrar por ella.

```bash
awk '{print $9}' access.log | sort | uniq -c | sort -rn
```

Salida real (VM del equipo, 2026-11-08):

```
   5165 200
      4 "-"
```

Ojo: si aparece un valor que no esperabas, los campos se corrieron.
````

Cada entrada lleva:

1. **Para qué** sirve, en una línea y con tus palabras.
2. El **comando**, tal como lo corriste.
3. La **salida real**, recortada si es larga, con dónde y cuándo la obtuviste.
4. Un **ojo** cuando haya una trampa (opcional, pero en "Números falsos" es obligatorio).

### Reglas

- Los ejemplos se corren en tu VM o sobre el `access.log` de la práctica. Ningún ejemplo inventado.
- Nada de copiar entradas de internet o de un libro: si un comando te gustó de un libro, escribe tu propio ejemplo con tus archivos.
- Los tres "Números falsos" deben ser distintos de los dos que vimos en clase (`uniq` sin `sort` y la línea `"-"`). Busca en la nota 18 la sección "Errores que dan un número falso" o encuentra uno propio.

---

## Criterio de aceptación

Tu tarea se considera completa cuando:

- [ ] `atajos-s01.md` tiene al menos 25 entradas en las seis secciones, con los mínimos de cada una.
- [ ] Cada entrada tiene para qué, comando y salida real.
- [ ] Los tres "Números falsos" son distintos de los de clase y cada uno explica cómo se detectó.
- [ ] Al menos una tubería tiene comprobación cruzada (otro comando que llega al mismo número).
- [ ] Ninguna entrada usa IP, contraseñas o nombres de servidores reales: solo la VM del laboratorio.

---

## Qué se entrega

En el repositorio del equipo, en la carpeta `linux/s01/`:

- `atajos-s01.md` (uno por persona: la hoja es personal).
- `respuestas-s01.txt` y `historial-s01.txt` de la práctica (uno por equipo).

---

## Preguntas frecuentes

**¿Puedo usar alias en mi hoja?**
Sí, pero escribe también el comando completo: en un servidor ajeno tu alias no existe.

**¿Puedo usar zsh en mi laptop?**
Sí, pero la hoja se prueba en la VM, que usa bash. Si algo funciona distinto en zsh, anótalo como "ojo".

**¿Cuenta un comando que no usamos en clase?**
Sí, si lo corriste y explicas para qué. Las mejores hojas traen dos o tres que nadie más tiene.

---

## Preparación para S02

Trae a la sesión 2:

- Tu hoja de atajos (la vas a seguir usando).
- La respuesta a esta pregunta: en tu VM, ¿con qué usuario corre hoy tu aplicación o el último servicio que instalaste? Averígualo con `ps -eo user,pid,cmd | grep <nombre>`.
