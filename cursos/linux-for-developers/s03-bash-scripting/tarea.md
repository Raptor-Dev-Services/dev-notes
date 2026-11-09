# Tarea - S03 - Bash que falla en voz alta

**Linux para desarrolladores**
**Tiempo estimado:** 4 horas

---

## Parte 1 - Lecturas (2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/19-bash-scripting.md` | Todo el documento, con su VM abierta para correr cada ejemplo | 60 min |
| `dev-notes/10-cloud/19-bash-scripting.md` | Releer: "set -Eeuo pipefail, y dónde no te protege" y "Verificar el resultado, no el código" | 20 min |
| `dev-notes/10-cloud/03-linux-nginx.md` | Sección "Scripting Bash esencial para automatización": búsquenle los huecos que vimos en clase | 20 min |

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Pro Bash*, 3.ª ed. (Jayant Varma, Apress) | Cap. 3 - Ciclos y condiciones; cap. 4 - Análisis y expansión de la línea de comandos; cap. 5 - Parámetros y variables; cap. 6 - Funciones; cap. 10 - Scripts sin errores y depuración; cap. 13 - Procesamiento de datos |
| *Efficient Linux at the Command Line* (Daniel J. Barrett, O'Reilly, 2022) | Cap. 8 - Comandos de una línea que crecen; cap. 9 - Aprovechar archivos de texto |

---

## Parte 2 - Ejercicio (2 horas)

### Reescribir un script viejo y mostrar lo que antes pasaba en silencio

Elijan **un script real** que hayan escrito o heredado: de un proyecto, del trabajo, de la escuela o de su VM. Sirve cualquiera que haga algo que importe: un despliegue, un respaldo, una limpieza, una migración, una carga de datos. Si de verdad no tienen ninguno, usen el bloque "Scripting Bash esencial" de `dev-notes/10-cloud/03-linux-nginx.md`.

1. **Encuentren el caso silencioso.** Busquen una entrada o una condición con la que el script falla pero termina con 0, o sigue de largo con un valor vacío. Pistas de la clase:
   - una tubería cuya primera pieza puede fallar;
   - una variable que puede llegar vacía, sobre todo antes de `rm`, `cp` o `mv`;
   - un `local x=$(...)` o un `|| true` que esconde un error;
   - un resultado que nadie revisa (el archivo existe, pero ¿trae algo?);
   - un `grep -c` o un conteo usado como si fuera un sí o un no.
2. **Provóquenlo** con la versión vieja y guarden la salida y el código (`echo "salida: $?"`).
3. **Reescriban** el script con lo de la sesión: `set -Eeuo pipefail` e `inherit_errexit`, parámetros obligatorios con `${1:?uso}`, comillas en toda expansión, mensajes a stderr, `trap` para limpiar y una verificación del **resultado**.
4. **Provoquen el mismo caso** con la versión nueva: debe terminar con código distinto de 0 y decir por qué.
5. Pasen ShellCheck a las dos versiones.

### Formato de la entrega

Un archivo `tarea-s03.md` en el repositorio del equipo (o un PR) con:

```markdown
## El script
Qué hace, dónde corre y quién depende de él.

## El caso que pasaba en silencio
La entrada o condición, y por qué el script viejo no lo notaba.

## Antes
(el script viejo y la salida del caso, con su código)

## Después
(el script nuevo y la salida del mismo caso, con su código)

## ShellCheck
(la salida para las dos versiones)
```

---

## Criterio de aceptación

- [ ] El script es real, no uno inventado para la tarea (o el de la nota, si no tienen ninguno).
- [ ] Muestran la salida **y el código** del caso silencioso con la versión vieja.
- [ ] La versión nueva termina con código distinto de 0 en ese mismo caso y el mensaje dice qué pasó.
- [ ] La versión nueva verifica el resultado de su trabajo, no solo el código de los comandos.
- [ ] ShellCheck no da avisos en la versión nueva (o cada aviso que queda está justificado en un comentario).
- [ ] No hay contraseñas, llaves ni IP reales en lo que entregan: usen `<IP_DE_SU_VM>` y variables de entorno.

---

## Preguntas frecuentes

**¿Y si mi script es de PowerShell o de Python?**
La tarea es de Bash. Si lo que tienen es de otro lenguaje, reescriban en Bash una parte que tenga sentido como script (la que llama a otros comandos) y apliquen la misma idea: código de salida honesto y resultado verificado.

**¿Puedo usar el respaldo de la práctica?**
No: ese ya lo vimos fallar en clase. La tarea es encontrar el caso silencioso en uno suyo.

**¿Basta con agregar `set -euo pipefail` arriba?**
No. Es el punto de partida. Muestren que el caso concreto ya no pasa en silencio, y si algún hueco de `-e` lo deja pasar todavía, ciérrenlo a mano.

---

## Preparación para la S04

- Traigan `respaldo.sh` funcionando en `/usr/local/bin` de su VM: en la S04 lo va a correr un timer de systemd.
- Lleguen con una pregunta concreta sobre algo que no les quedó claro de los huecos de `set -e`.
