# 01 · Checklists de verificación y DevSecOps

Los checklists mecanizan las decisiones repetibles en el ciclo de vida de un cambio: antes del commit, antes del merge, antes del deploy y durante un incidente. El concepto de **shift-left** integra la seguridad en cada etapa temprana del pipeline en lugar de revisarla al final.

> Fuentes: *Learning DevSecOps* (Mark Rendell): Ch.3 Integrating Security, Ch.5 Moving Toward Deployment; *Learning GitHub Actions* (Brent Laster): Ch.9 Actions and Security, Ch.12 Advanced Workflows

---

## Shift-left security — el principio

El término "shift-left" se refiere a mover las comprobaciones de seguridad y calidad hacia la izquierda del pipeline. Es decir, más temprano en el ciclo de desarrollo. Detectar una vulnerabilidad durante el commit del desarrollador cuesta minutos. Detectarla en producción puede costar días de remediación, reputación y datos.

```
Desarrollo → CI → Staging → Producción
    ↑
  aquí es donde shift-left ubica las comprobaciones
  (SAST, dependency scan, secret scan, lint)
```

Los tipos de análisis de seguridad en CI:

| Tipo | Qué analiza | Herramientas |
|------|-------------|-------------|
| SAST (Static Application Security Testing) | el código fuente en busca de vulnerabilidades antes de compilar | SonarCloud, CodeQL, Semgrep |
| DAST (Dynamic Application Security Testing) | la app en ejecución en busca de vulnerabilidades HTTP | OWASP ZAP, Burp Suite |
| Dependency Scanning | dependencias con CVEs conocidos | Dependabot, OWASP Dependency-Check, Trivy |
| Secret Scanning | credentials, tokens, API keys en el código | GitHub Secret Scanning, TruffleHog, gitleaks |
| Container Scanning | vulnerabilidades en la imagen Docker | Trivy, Grype, Snyk |
| SBOM (Software Bill of Materials) | inventario de dependencias para auditoría | Syft, CycloneDX |

---

## Checklist de seguridad en el pipeline CI

### Antes de que el PR pueda mergearse

- [ ] **Secret scanning:** ningún token, API key ni contraseña en el diff (GitHub Secret Scanning activo, o pre-commit con `gitleaks`)
- [ ] **Dependency scan:** `Dependabot` o `OWASP Dependency-Check` sin CVE crítico/alto sin justificar
- [ ] **SAST:** SonarCloud / CodeQL sin vulnerabilidades nuevas marcadas como bloqueantes
- [ ] **Tests:** cobertura no bajó del umbral del proyecto, todos los tests pasan
- [ ] **Lint / format:** `dotnet format --verify-no-changes` (o el equivalente del stack) sin errores
- [ ] **Container scan:** si hay cambios en Dockerfile o base image, `trivy image` sin CVE crítico

### Workflow de GitHub Actions para seguridad en PR

```yaml
# .github/workflows/security.yml
name: Security Gates

on:
  pull_request:
    branches: [main]

jobs:
  secret-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0   # historia completa para TruffleHog
      - name: TruffleHog — secret scan
        uses: trufflesecurity/trufflehog@main
        with:
          path: ./
          base: ${{ github.event.repository.default_branch }}
          head: HEAD

  dependency-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: OWASP Dependency-Check
        uses: dependency-check/Dependency-Check_Action@main
        with:
          project: 'mi-proyecto'
          path: '.'
          format: 'HTML'
          args: --failOnCVSS 7   # falla si hay CVE con score ≥ 7
      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: dependency-check-report
          path: reports/

  codeql-analysis:
    runs-on: ubuntu-latest
    permissions:
      security-events: write   # necesario para publicar resultados en GitHub Security
    steps:
      - uses: actions/checkout@v4
      - uses: github/codeql-action/init@v3
        with:
          languages: 'csharp'
      - uses: github/codeql-action/autobuild@v3
      - uses: github/codeql-action/analyze@v3
```

---

## Seguridad en workflows de GitHub Actions

### Permisos mínimos para GITHUB_TOKEN

```yaml
# ✓ declarar permisos explícitos — nunca confiar en el default read-write
permissions:
  contents: read         # leer el código
  security-events: write # publicar resultados de CodeQL
  packages: write        # push a GitHub Container Registry
  id-token: write        # para OIDC (AWS, Azure)
```

```yaml
# ✗ default con permisos amplios — todo el repositorio queda expuesto si el workflow es vulnerable
# (no declarar permissions en repos con configuración legacy)
```

### Pinear versiones de actions con SHA

Un atacante que controla un repositorio de action puede cambiar la etiqueta `@v3` para apuntar a código malicioso. El SHA es inmutable.

```yaml
# ✗ etiqueta mutable
- uses: actions/checkout@v3

# ✓ SHA inmutable (auditado y congelado)
- uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683  # v4.2.2
```

### Secretos en workflows

```yaml
# ✓ referencia desde secrets
env:
  AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}

# ✗ nunca hardcodear en el YAML
env:
  API_KEY: "sk-live-1234567890abcdef"   # se indexa en el historial de git
```

---

## Workflows reutilizables (Reusable Workflows)

Un workflow reutilizable se activa con `workflow_call` en lugar de `push`/`pull_request`. Permite que múltiples repos usen la misma lógica de CI sin duplicar YAML.

```yaml
# .github/workflows/build-and-scan.yml  (en el repo common de la organización)
name: Build and Security Scan

on:
  workflow_call:
    inputs:
      image-name:
        required: true
        type: string
      dotnet-version:
        required: false
        type: string
        default: '10.0.x'
    secrets:
      registry-token:
        required: true

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ inputs.dotnet-version }}
      - run: dotnet build --configuration Release

  scan:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Scan imagen Docker
        run: trivy image ${{ inputs.image-name }}
        env:
          TRIVY_TOKEN: ${{ secrets.registry-token }}
```

```yaml
# Caller workflow en otro repo
name: CI

on: [push]

jobs:
  pipeline:
    uses: mi-org/common/.github/workflows/build-and-scan.yml@main
    with:
      image-name: 'mi-org/mi-app:latest'
    secrets:
      registry-token: ${{ secrets.REGISTRY_TOKEN }}
```

---

## Checklist 1 — Antes de hacer commit

- [ ] ¿Hay secretos, contraseñas, API keys reales en el diff?
- [ ] ¿Hay console.log, Console.WriteLine, debugger o código de prueba?
- [ ] ¿Pasa el lint y el format?
- [ ] ¿Compila localmente?
- [ ] ¿Pasan los tests que tocan el área modificada?
- [ ] ¿El mensaje de commit sigue Conventional Commits?
- [ ] ¿Las migraciones de BD tienen su script de rollback?

## Checklist 2 — Antes de hacer merge a main

- [ ] ¿El PR fue revisado por al menos una persona?
- [ ] ¿Pasaron todos los checks de CI (build, tests, SAST, dependency scan)?
- [ ] ¿Secret scanning no encontró credentials en el diff?
- [ ] ¿Hay tests para el código nuevo?
- [ ] ¿La documentación está actualizada?
- [ ] ¿Las variables de entorno nuevas están documentadas?
- [ ] ¿Las migraciones de BD son reversibles?
- [ ] ¿Hay cambios breaking? Si sí, ¿se documentaron y se incrementó MAJOR?

## Checklist 3 — Antes de desplegar a producción

- [ ] ¿Se probó en staging con datos reales (no producción) primero?
- [ ] ¿Las migraciones se probaron en staging?
- [ ] ¿Se agregaron las variables de entorno nuevas a producción?
- [ ] ¿Los secretos nuevos están en Key Vault / Secrets Manager?
- [ ] ¿Hay un plan de rollback documentado?
- [ ] ¿La ventana de deploy es la correcta (no viernes 5 PM)?
- [ ] ¿Está alguien on-call por si algo falla?
- [ ] ¿El SBOM fue generado y archivado para auditoría?

## Checklist 4 — Diagnóstico rápido cuando algo falla en producción

- [ ] Health checks: `/health` responde 200 en cada servicio?
- [ ] Logs: en Seq / CloudWatch / Loki buscar errores en los últimos 15 minutos
- [ ] Métricas: latencia p95, tasa de error, uso de CPU/memoria
- [ ] Conexiones BD: ¿hay pool agotado? ¿queries lentas?
- [ ] Servicios externos: ¿Stripe está caído? ¿el SMTP responde?
- [ ] Última versión desplegada: ¿este bug existía antes? ¿coincide con un deploy reciente?
- [ ] ¿Se activó el rollback? ¿cuándo lo haremos si el fix demora más de 30 min?

---

## SBOM — Software Bill of Materials

Un SBOM es un inventario de todos los componentes de software (dependencias, versiones, licencias) de una aplicación. Es requerido por algunos marcos de cumplimiento (NIST, Executive Order 14028 de EE.UU.) y permite responder rápidamente a vulnerabilidades nuevas.

```bash
# Generar SBOM con Syft (CycloneDX format)
syft packages dir:. -o cyclonedx-json=sbom.cdx.json

# Escanear el SBOM con Grype para CVEs
grype sbom:./sbom.cdx.json --fail-on critical
```

```yaml
# En el pipeline CI — generar y archivar SBOM
- name: Generar SBOM
  uses: anchore/sbom-action@v0
  with:
    path: .
    format: cyclonedx-json
    output-file: sbom.cdx.json

- uses: actions/upload-artifact@v4
  with:
    name: sbom
    path: sbom.cdx.json
    retention-days: 365
```

---

## Relación con back-template

El pipeline del back-template (ver `09-cicd/02-github-actions.md`) incluye los jobs de SAST (CodeQL) y dependency scan (Dependabot alerts activados en el repo). Los permisos de GITHUB_TOKEN están declarados explícitamente en cada workflow. Los secretos van en GitHub Secrets y se pasan a los jobs vía `${{ secrets.NOMBRE }}`.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| SAST en PRs como gate bloqueante | DAST en el pipeline de PR (muy lento — usar en nightly) |
| Pinear actions con SHA en repos públicos | SHA pinning en repos privados de empresa (mayor overhead sin el mismo riesgo) |
| Workflows reutilizables para lógica CI compartida entre repos | Reusable workflows para pasos de 2 líneas — YAML inline es más simple |
| SBOM en cada release para auditoría | Generar SBOM en cada commit — costoso y sin valor adicional |

---

## Glosario

| Término | Definición |
|---------|-----------|
| SAST | Static Application Security Testing — análisis del código fuente en busca de vulnerabilidades |
| DAST | Dynamic Application Security Testing — análisis de la app en ejecución vía HTTP |
| Dependency scanning | revisión de librerías de terceros contra bases de datos de CVEs conocidos |
| CVE | Common Vulnerabilities and Exposures — identificador estandarizado de vulnerabilidades |
| CVSS | Common Vulnerability Scoring System — puntuación numérica de severidad (0-10) |
| Secret scanning | detección de credentials y tokens en el código fuente o historial git |
| Shift-left | práctica de mover las comprobaciones de calidad y seguridad hacia las etapas tempranas del SDLC |
| SBOM | Software Bill of Materials — inventario de todos los componentes de software de una aplicación |
| Reusable workflow | workflow de GitHub Actions con `workflow_call` que puede ser invocado por otros workflows |
| GITHUB_TOKEN | token temporal de acceso al repositorio generado automáticamente para cada ejecución de workflow |
| CodeQL | motor de análisis estático de GitHub para detectar vulnerabilidades en código |
| TruffleHog | herramienta de secret scanning que analiza el historial git en busca de credentials |
| Trivy | scanner open-source de vulnerabilidades para contenedores, filesystems y repositorios |

---

*Rogelio Arriaga Gonzalez*
