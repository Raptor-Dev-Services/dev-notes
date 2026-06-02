# 03 · Configuración de Git

## Problema que resuelve

Git necesita saber quién hace cada commit. En equipos o en máquinas con múltiples cuentas (personal / trabajo / cliente) se requiere configurar identidades distintas por proyecto sin sobreescribir la configuración global cada vez.

## Niveles de configuración

Git aplica configuración en orden de precedencia (menor a mayor):

| Nivel | Archivo | Comando |
|-------|---------|---------|
| Sistema | `/etc/gitconfig` | `git config --system` |
| Global (usuario) | `~/.gitconfig` | `git config --global` |
| Local (repositorio) | `.git/config` | `git config` |
| Worktree | `.git/config.worktree` | `git config --worktree` |

El nivel local siempre gana sobre el global.

## Configuración global mínima

```bash
git config --global user.name  "Rogelio Arriaga"
git config --global user.email "rogelio@raptordev.io"
git config --global core.editor "code --wait"
git config --global init.defaultBranch main
git config --global pull.rebase false
```

Ver configuración activa:

```bash
git config --list --show-origin
```

## Configuración local por repositorio

Dentro del directorio del repositorio, sin `--global`:

```bash
git config user.name  "Rogelio Arriaga"
git config user.email "rogelio@cliente.com"
```

Esto escribe en `.git/config` y sobreescribe el valor global solo para ese repo.

## Multi-usuario con includeIf

Para cambiar de identidad automáticamente según la ruta del proyecto, usar `includeIf` en `~/.gitconfig`:

```ini
# ~/.gitconfig
[user]
    name  = Rogelio Arriaga
    email = rogelio@personal.com

[includeIf "gitdir:~/dev/trabajo/"]
    path = ~/.gitconfig-trabajo

[includeIf "gitdir:~/dev/cliente-a/"]
    path = ~/.gitconfig-cliente-a
```

```ini
# ~/.gitconfig-trabajo
[user]
    name  = Rogelio Arriaga
    email = rogelio@empresa.com
```

```ini
# ~/.gitconfig-cliente-a
[user]
    name  = R. Arriaga
    email = rarriaga@clientea.mx
```

La ruta en `gitdir:` debe terminar con `/`. Git usa la ruta del archivo `.git/` para decidir qué incluir.

## Variables de entorno

Para sobrescribir temporalmente sin tocar archivos de configuración:

```bash
GIT_AUTHOR_NAME="Temporal" GIT_AUTHOR_EMAIL="temp@x.com" git commit -m "fix"
```

Útil en pipelines CI donde la identidad viene de secrets del sistema.

## Configuración recomendada para equipos

```ini
# ~/.gitconfig
[core]
    autocrlf = input        # en Linux/Mac; usar true en Windows
    whitespace = fix
    longpaths = true

[merge]
    ff = false              # nunca fast-forward en merge

[pull]
    rebase = false          # pull = fetch + merge (no rebase)

[push]
    default = current       # push solo la rama actual

[alias]
    st  = status
    lg  = log --oneline --graph --decorate --all
    undo = reset HEAD~1 --soft
```

## Verificar identidad antes de un commit

```bash
git config user.name
git config user.email
```

O en el log después del commit:

```bash
git log -1 --format="%an <%ae>"
```

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| `--global` para defaults de la máquina | `--global` para configs específicas de un proyecto |
| `includeIf` para múltiples identidades por directorio | variables de entorno permanentes en `.bashrc` |
| `git config` local para overrides por repo | editar `.git/config` manualmente |


> Fuente: *Pro Git 2nd Ed* (Scott Chacon, Ben Straub) — Ch.8 Customizing Git: Git Configuration

---

## Glosario

| Término | Definición |
|---------|-----------|
| git config | Comando para leer y escribir la configuración de Git en los niveles sistema, global o local |
| Nivel global | Configuración en `~/.gitconfig` que aplica a todos los repositorios del usuario en la máquina |
| Nivel local | Configuración en `.git/config` que aplica solo al repositorio actual y sobreescribe el nivel global |
| includeIf | Directiva de `~/.gitconfig` que incluye un archivo de configuración adicional si se cumple una condición de ruta |
| gitdir | Condición usada en `includeIf` que activa la inclusión cuando el repositorio está bajo la ruta especificada |
| pull.rebase | Opción que determina si `git pull` usa rebase o merge para integrar cambios del remoto |
| init.defaultBranch | Opción que define el nombre de la rama inicial al crear un nuevo repositorio con `git init` |
| autocrlf | Opción `core.autocrlf` que controla la conversión de saltos de línea al hacer checkout o commit |
| alias | Atajo personalizado en Git que reemplaza un comando largo por uno corto definido en la configuración |
| GIT_AUTHOR_NAME | Variable de entorno que sobreescribe temporalmente el nombre del autor para el commit actual |

---

*Rogelio Arriaga Gonzalez*
