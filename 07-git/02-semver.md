# 02 · Versionado Semántico (SemVer 2.0.0)

## Problema que resuelve

Sin una convención de versiones, los consumidores de una librería o API no saben si una actualización rompe su código, agrega funcionalidad o solo corrige errores. SemVer da un contrato explícito: el número de versión comunica la naturaleza del cambio.

## Formato

```
MAJOR.MINOR.PATCH
```

| Segmento | Incrementa cuando |
|----------|-------------------|
| MAJOR | cambio que rompe compatibilidad hacia atrás (breaking change) |
| MINOR | nueva funcionalidad compatible hacia atrás |
| PATCH | corrección de error compatible hacia atrás |

Regla: al incrementar MAJOR se resetean MINOR y PATCH a 0. Al incrementar MINOR se resetea PATCH a 0.

## Versión 0.x.y — API inestable

Durante el desarrollo inicial se usa `0.MINOR.PATCH`. La API pública se considera inestable y cualquier versión puede introducir breaking changes sin incrementar MAJOR.

```
0.1.0   primera entrega funcional
0.2.0   iteración con cambios que pueden romper
1.0.0   primera versión con API estable y pública
```

## Pre-release identifiers

Extensión con guión después de PATCH:

```
MAJOR.MINOR.PATCH-identificador
```

| Identificador | Significado |
|---------------|-------------|
| `alpha` | inestable, incompleto, solo para desarrollo interno |
| `beta` | funcionalidad completa, en pruebas externas |
| `rc.N` | release candidate, candidato a producción |

Ejemplos:

```
1.0.0-alpha
1.0.0-alpha.1
1.0.0-beta
1.0.0-beta.2
1.0.0-rc.1
1.0.0
```

Las pre-releases tienen precedencia menor que la versión de release:

```
1.0.0-alpha < 1.0.0-alpha.1 < 1.0.0-beta < 1.0.0-rc.1 < 1.0.0
```

## Build metadata

Extensión con `+` después de PATCH o del identificador pre-release:

```
1.0.0+20130313144700
1.0.0-beta+exp.sha.5114f85
```

El build metadata se ignora al determinar precedencia de versión.

## Precedencia de versiones

Comparación de izquierda a derecha, segmento a segmento:

```
1.0.0 < 2.0.0 < 2.1.0 < 2.1.1
1.0.0-alpha < 1.0.0
1.0.0-alpha < 1.0.0-alpha.1 < 1.0.0-alpha.beta < 1.0.0-beta
1.0.0-beta < 1.0.0-beta.2 < 1.0.0-beta.11 < 1.0.0-rc.1 < 1.0.0
```

Los identificadores numéricos se comparan numéricamente; los alfanuméricos lexicográficamente.

## Ejemplos prácticos

```
# corrección de bug en producción
2.3.1 → 2.3.2

# nueva funcionalidad sin romper nada
2.3.1 → 2.4.0

# cambio de API que rompe compatibilidad
2.3.1 → 3.0.0

# ciclo de release de feature grande
1.5.0-alpha → 1.5.0-beta → 1.5.0-rc.1 → 1.5.0
```

## Relación con back-template

En el back-template los proyectos NuGet (`GTM.Common`, `GTM.Domain`) siguen SemVer. El archivo `.csproj` define la versión:

```xml
<Version>1.2.0</Version>
<AssemblyVersion>1.2.0.0</AssemblyVersion>
<FileVersion>1.2.0.0</FileVersion>
```

Las APIs web usan SemVer en conjunto con API versioning (`v1`, `v2`). El versionado de ruta de la API no reemplaza a SemVer del paquete/servicio.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| librerías NuGet publicadas | scripts internos sin distribución |
| APIs con múltiples consumidores | prototipos desechables |
| paquetes npm del frontend | configuración de infraestructura |
| microservicios con contratos de API | |


> Fuente: *The Software Engineer's Guidebook* (Gergely Orosz): Ch.7 Software Development Best Practices

---

## Glosario

| Término | Definición |
|---------|-----------|
| SemVer | Semantic Versioning; convención de versiones con formato MAJOR.MINOR.PATCH que comunica el impacto de cada cambio |
| MAJOR | Segmento de versión que se incrementa cuando hay cambios incompatibles con la versión anterior (breaking change) |
| MINOR | Segmento de versión que se incrementa cuando se agrega funcionalidad nueva de forma compatible hacia atrás |
| PATCH | Segmento de versión que se incrementa cuando se corrigen errores de forma compatible hacia atrás |
| Breaking change | Cambio que rompe la compatibilidad con consumidores de la versión anterior de la API o librería |
| Pre-release | Identificador opcional después de PATCH (alpha, beta, rc.N) que indica versiones previas a la release estable |
| Release candidate | Versión candidata a producción (rc.N) considerada lista salvo que se encuentre algún defecto crítico |
| Build metadata | Información adicional después de `+` en la versión (ej. SHA del commit) que no afecta la precedencia |
| Precedencia | Orden de comparación entre versiones; pre-releases tienen menor precedencia que la release estable equivalente |
| API versioning | Estrategia para mantener múltiples versiones de una API web disponibles simultáneamente (v1, v2) |

---

*Rogelio Arriaga Gonzalez*
