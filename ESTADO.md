# Estado de mantenimiento

Este documento es para quien retome el mantenimiento del repositorio, no material del curso.
Se sobrescribe en cada handoff: lo que pasó en cada sesión vive en los mensajes de commit.

**Actualizado:** 19 de septiembre de 2026

---

## Dónde quedó

| | |
|---|---|
| Rama de integración | `main` |
| Último commit | empujado a `origin/main` |
| Árbol de trabajo | limpio |
| Ramas de trabajo | ninguna, ni local ni en `origin` |

---

## Las puertas al cerrar

Medidas hoy, con los números reales:

- **Documentación:** `python3 herramientas/verifica-docs.py`. Cuenta 206 documentos en esta copia y 205 en un clon limpio: la diferencia es `.claude/rules/stable-dependencies.md`, que está ignorado y no se clona. Cero enlaces internos rotos, cero títulos con número distinto al de su archivo, cero prefijos duplicados, cero nombres de clientes o productos reales, y cero guiones largos o flechas en la prosa.
- **Ejemplos de patrones en C#:** `dotnet build 02-programacion/design-patterns/referencias-csharp/RefactoringGuru.DesignPatterns.sln` compila con 0 errores y 1 aviso, el de `string.Copy` obsoleto en `Prototype.Conceptual`.
- **No se ejecutaron** esos ejemplos: apuntan a `net8.0` y la máquina solo tiene el runtime 10.

---

## Qué sigue, por orden

1. **Faltan README en cinco carpetas:** `01-fundamentos`, `02-programacion`, `03-arquitectura`, `04-backend` y `resources`. Las demás carpetas sí lo tienen y listan todos sus documentos. El `INDICE.md` de la raíz sí los cubre a todos, así que esto es comodidad, no un hueco.
2. **Doce documentos sin sección Glosario:** los cinco de `03-arquitectura/api-design` y los de `06-frontend` del 06 al 11. Los otros 170 sí la traen.
3. **Los capítulos citados de las fuentes no están comprobados.** Los autores sí: se corrigieron ocho en `resources/books/README.md`, y "Ferreira" pasó a Carl-Hugo Marcotte en sus 36 menciones. Si aparece un capítulo que no corresponde al libro, empezar por los mismos libros de esa corrección.
4. **El `.gitignore` ignora `CLAUDE.md` y `.claude/`,** al revés que los demás repositorios del workspace. Es una decisión pendiente del dueño, no un descuido detectado.

---

## Lo que NO viaja con un `git clone`

- **`.claude/` está ignorado a propósito** y contiene dos cosas que solo existen en este disco: `settings.local.json`, las reglas de permisos escritas el 12 de septiembre, y `rules/stable-dependencies.md`, que apareció el 17 de septiembre. Esas reglas **solo surten efecto si se abre Claude dentro de esta carpeta**; desde la raíz del catálogo mandan las de `agents/.claude`.
- **Los documentos del temario de la academia no están en ningún repositorio.** Viven en `My Drive/Raptor Dev services` de Google Drive: el temario palomeado, el temario conjunto y la propuesta, cada uno en `.md` y `.html`, más sus generadores `generador-temario.py`, `md2html.py` y `estilo-temario.css`. Los HTML se generan desde los `.md`, así que no se editan a mano.
- **Para ver un HTML en el navegador:** las rutas `file://` están bloqueadas para la extensión de Chrome, y la extensión puede no estar conectada. Lo que funciona es levantar `python3 -m http.server 8765` en la carpeta y renderizar con Edge en modo headless.
- **`scripts/check-prerelease-deps.py` tampoco viaja:** apareció el 17 de septiembre junto con `.claude/rules/stable-dependencies.md`, es la puerta que hace cumplir esa regla (truena si el repositorio consume un paquete en versión prerelease o flotante) y vive en la carpeta ignorada `scripts/`. No lo escribí yo en esta sesión.
- **Trampa del `.gitignore`:** trae `Scripts/` de un entorno virtual de Python que se coló una vez, y en macOS el sistema de archivos no distingue mayúsculas, así que una carpeta `scripts/` queda ignorada sin avisar. Por eso las herramientas del repositorio viven en `herramientas/`.
- **Herramientas de esta máquina:** .NET SDK 10.0.300 con runtime 10 únicamente, y el Python 3 del sistema. No hace falta base de datos, contenedores ni variables de entorno: el repositorio es documentación.

---

## Lo que NO se verificó

- No se ejecutaron los ejemplos de patrones, solo se compilaron.
- No se comprobaron los capítulos citados de cada fuente bibliográfica.
- Las reglas de permisos de `.claude/` no se probaron desde dentro de este repositorio: hay que abrir Claude aquí y pedirle un `rm -rf` sobre una ruta inexistente para ver si el bloqueo muerde.
- No hay bitácora de sesión. El porqué de los cambios está en los mensajes de los dos commits del 12 de septiembre.
