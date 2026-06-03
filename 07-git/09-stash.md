# 09 · git stash

## Problema que resuelve

Git no permite cambiar de rama con trabajo no commiteado que conflictúe con la rama destino. `git stash` guarda temporalmente los cambios en una pila, dejando el working directory limpio para cambiar de contexto y recuperar el trabajo después.

## Operaciones básicas

```bash
# guardar cambios del working directory y staging en el stash
git stash push -m "descripcion del trabajo en progreso"

# guardar incluyendo archivos sin seguimiento (untracked)
git stash push -u -m "wip: incluye nuevos archivos"

# listar el stash
git stash list
# stash@{0}: On feature/GTM-123: wip: user validation
# stash@{1}: On develop: fix in progress

# recuperar el último stash y eliminarlo de la pila
git stash pop

# recuperar un stash específico
git stash pop stash@{1}

# aplicar sin eliminar de la pila
git stash apply stash@{0}

# eliminar un stash sin aplicar
git stash drop stash@{1}

# limpiar todos los stashes
git stash clear
```

## Mover trabajo entre ramas

Escenario: comenzaste a trabajar en `main` por error y el trabajo pertenece a una feature branch.

```bash
# 1. guardar el trabajo actual
git stash push -m "wip: auth feature"

# 2. ir a la rama correcta (o crearla)
git checkout feature/GTM-123-user-auth
# o: git checkout -b feature/GTM-123-user-auth

# 3. recuperar el trabajo
git stash pop
```

## Crear una rama desde un stash

Si el contexto cambió y el stash ya no aplica limpiamente a la rama actual:

```bash
# crear rama desde el punto donde se guardó el stash
git stash branch feature/GTM-new-branch stash@{0}
```

Esto crea la rama en el commit donde se hizo el stash, aplica los cambios y elimina la entrada del stash si no hay conflictos.

## Stash parcial

```bash
# stash interactivo — elegir qué archivos guardar
git stash push -p -m "solo el controller"

# stash de un archivo específico
git stash push -m "solo auth" -- src/Auth/AuthController.cs
```

## Ver el contenido de un stash

```bash
# ver diff completo del stash
git stash show -p stash@{0}

# ver solo los archivos modificados
git stash show stash@{0}
```

## Conflictos al aplicar un stash

Si `git stash pop` produce conflictos:

```bash
git stash pop
# CONFLICT (content): Merge conflict in src/Auth/AuthController.cs

# resolver el conflicto manualmente
# luego marcar como resuelto
git add src/Auth/AuthController.cs

# el stash NO se elimina automáticamente cuando hay conflictos
# eliminarlo manualmente después de resolver
git stash drop stash@{0}
```

## Incluir archivos sin seguimiento e ignorados

```bash
# incluir untracked (-u)
git stash push -u -m "wip con archivos nuevos"

# incluir también los archivos ignorados por .gitignore (-a)
git stash push -a -m "todo incluyendo ignorados"
```

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| cambiar de contexto sin perder trabajo no terminado | guardar trabajo por más de un día o dos (crear una rama WIP es mejor) |
| mover cambios de una rama equivocada a la correcta | como sustituto de commits frecuentes |
| pausar trabajo para atender un hotfix urgente | cuando se necesita compartir el trabajo con otro desarrollador |
| aplicar el mismo cambio exploratorio en varias ramas | — |


> Fuente: *Learning Git* (Anna Skoulikari): Ch.8 Stashing and Cleaning

---

## Glosario

| Término | Definición |
|---------|-----------|
| git stash | Comando que guarda los cambios no commiteados del working directory y staging en una pila temporal |
| Pila de stash | Estructura LIFO (último en entrar, primero en salir) donde Git almacena las entradas de stash |
| stash pop | Recupera y elimina el stash más reciente (o el especificado) de la pila aplicando sus cambios |
| stash apply | Aplica el stash sin eliminarlo de la pila; útil para reproducir el mismo cambio en varias ramas |
| stash drop | Elimina una entrada de la pila del stash sin aplicar sus cambios |
| stash clear | Elimina todas las entradas de la pila del stash de forma irreversible |
| Archivo untracked | Archivo nuevo que Git aún no rastrea; se incluye en el stash solo con la opción `-u` |
| stash branch | Comando que crea una rama nueva desde el commit donde se creó el stash y aplica sus cambios |
| Stash parcial | Técnica que guarda solo archivos o cambios seleccionados usando `-p` o especificando rutas explícitas |
| Working directory | Estado del árbol de archivos del repositorio con los cambios no commiteados del desarrollador |

---

*Rogelio Arriaga Gonzalez*
