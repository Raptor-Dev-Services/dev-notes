# 01 — Archivos de Proyecto y Tooling de Equipo

Archivos de configuración que todo proyecto debe incluir en el repositorio para garantizar consistencia entre desarrolladores, IDEs y entornos de CI.

> Fuente: *Pro Git 2nd Ed* (Scott Chacon, Ben Straub) — Ch.8 Customizing Git

---

## `.gitignore`

```gitignore
# .NET
bin/
obj/
*.user
*.suo
.vs/
[Tt]est[Rr]esult*/
*.nupkg
.nuget/

# Node / React
node_modules/
dist/
build/
.vite/
coverage/
.eslintcache

# Variables de entorno
.env
.env.local
.env.*.local

# IDE
.idea/
.vscode/*
!.vscode/settings.json
!.vscode/extensions.json
!.vscode/tasks.json

# OS
.DS_Store
Thumbs.db
desktop.ini

# Secretos (nunca al repo)
*.pfx
*.pem
*.key
secrets.json
local.settings.json
```

---

## `.dockerignore`

Excluye archivos que no necesita la imagen Docker — reduce el contexto del build significativamente (de GB a MB):

```dockerignore
**/.git
**/.gitignore
**/.vs
**/.vscode
**/.idea
**/bin
**/obj
**/node_modules
**/dist
**/build
**/.env
**/.env.*
**/coverage
**/*.md
**/Dockerfile*
**/docker-compose*
**/.dockerignore
**/README.md
**/LICENSE
**/tests
```

---

## `.editorconfig`

Estandariza indentación, line endings y charset entre todos los editores del equipo. El archivo se hereda — una regla en el root aplica a todo el proyecto:

```ini
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.{js,jsx,ts,tsx,json,yml,yaml,html,css}]
indent_size = 2

[*.cs]
indent_size = 4
csharp_new_line_before_open_brace = all
csharp_indent_case_contents = true
csharp_space_after_cast = false

[*.{md,txt}]
trim_trailing_whitespace = false
```

---

## Husky + lint-staged (Frontend)

Ejecuta lint y format automáticamente en cada commit — el desarrollador no necesita recordarlo:

```bash
# instalación
npm install --save-dev husky lint-staged
npx husky init
```

```json
// package.json
{
  "scripts": {
    "prepare": "husky",
    "lint": "eslint .",
    "format": "prettier --write ."
  },
  "lint-staged": {
    "*.{js,jsx,ts,tsx}": ["eslint --fix", "prettier --write"],
    "*.{json,css,md}": ["prettier --write"]
  }
}
```

```bash
# .husky/pre-commit
npx lint-staged
```

---

## Dependabot

Abre PRs automáticos cuando hay actualizaciones disponibles en dependencias — sin Dependabot las vulnerabilidades de librerías antiguas se acumulan silenciosamente:

```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: "npm"
    directory: "/frontend"
    schedule: { interval: "weekly" }
    open-pull-requests-limit: 10
    groups:
      react-ecosystem:
        patterns: ["react*", "@types/react*"]

  - package-ecosystem: "nuget"
    directory: "/backend"
    schedule: { interval: "weekly" }
    open-pull-requests-limit: 10

  - package-ecosystem: "docker"
    directory: "/"
    schedule: { interval: "monthly" }

  - package-ecosystem: "github-actions"
    directory: "/"
    schedule: { interval: "monthly" }
```

---

## `.gitattributes`

Normaliza line endings — previene diffs de archivos completos por cambios de CRLF/LF:

```gitattributes
# Normalizar a LF en el repositorio
* text=auto eol=lf

# Archivos binarios — no tocar line endings
*.png binary
*.jpg binary
*.gif binary
*.ico binary
*.pdf binary
*.zip binary
*.dll binary
*.exe binary
*.nupkg binary
```

---

## Checklist de archivos en cada nuevo repositorio

```
✓ .gitignore        — sin bin/, obj/, node_modules/, .env
✓ .dockerignore     — contexto Docker mínimo
✓ .editorconfig     — consistencia entre IDEs del equipo
✓ .gitattributes    — LF consistente en todos los sistemas
✓ README.md         — cómo arrancar el proyecto localmente
✓ dependabot.yml    — dependencias actualizadas automáticamente
```

---

## Glosario

| Término | Definición |
|---------|-----------|
| .gitignore | Archivo que lista patrones de rutas que Git debe ignorar y no incluir en el repositorio |
| .dockerignore | Archivo que excluye rutas del contexto enviado al demonio Docker al construir una imagen |
| .editorconfig | Archivo de configuración que estandariza indentación, charset y saltos de línea entre editores del equipo |
| .gitattributes | Archivo que define atributos por ruta, como normalización de line endings o tratamiento de archivos binarios |
| Husky | Herramienta de Node.js que instala git hooks en el proyecto para ejecutar scripts automáticos antes de cada commit |
| lint-staged | Herramienta que ejecuta linters solo sobre los archivos modificados y en staging, evitando analizar todo el proyecto |
| Dependabot | Servicio de GitHub que abre PRs automáticos cuando detecta actualizaciones en dependencias del proyecto |
| CRLF / LF | Caracteres de fin de línea; CRLF es de Windows (`\r\n`), LF es de Unix (`\n`); .gitattributes normaliza entre ellos |
| Pre-commit hook | Script de Git que se ejecuta antes de crear el commit; permite bloquear commits que no pasen las validaciones |
| EditorConfig | Especificación y ecosistema de plugins para editores que leen el archivo `.editorconfig` de forma automática |

---

*Rogelio Arriaga Gonzalez*
