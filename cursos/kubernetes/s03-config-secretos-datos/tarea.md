# Tarea - S03 - Configuración, secretos y datos con estado

**Kubernetes: del Compose al clúster**
**Tiempo estimado:** 4 horas

---

## Parte 1 - Lecturas (2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/05-secretos-produccion.md` | Todo el documento; subrayen las opciones que existen en la nube de su proyecto | 50 min |
| Documentación de Kubernetes, "Secrets" (kubernetes.io) | Recuadro de advertencia y "Good practices" | 20 min |
| Documentación de Kubernetes, "Persistent Volumes" | Ciclo de vida de un volumen y una PVC; modos de acceso; política de reclamación | 30 min |
| Documentación de Kubernetes, "StatefulSets" | Identidad de los pods, `volumeClaimTemplates` y limitaciones | 20 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *Kubernetes: Up and Running* (Burns et al., 2022) | Cap. 13 - ConfigMaps and Secrets; cap. 16 - Integrating Storage Solutions and Kubernetes |
| *Kubernetes Recipes* (Stencel) | Cap. 3 - Configuring Stateful Applications; cap. 7 - Storage in Kubernetes |
| *CKA Study Companion* (Sachdeva, 2025) | Cap. 15 - Storage in Kubernetes |

---

## Parte 2 - Ejercicio (2 horas)

### Dónde vivirían los secretos de su proyecto en producción, y por qué

Escriban `docs/secretos-en-produccion.md` en su repositorio. Es un documento de decisión, como el ADR de la sesión 1.

#### 1. Inventario

Una tabla con **todos** los secretos que su aplicación usa hoy: nombre, para qué sirve, quién lo necesita (qué componente) y dónde está hoy (archivo `.env`, variable del CI, YAML...). Si una fila dice "en el repositorio", es un hallazgo: márquenlo.

#### 2. Opciones

Al menos **dos opciones reales** para producción, de entre:

- Secret de Kubernetes con cifrado en reposo y RBAC de mínimo privilegio.
- Un gestor de secretos de su proveedor de nube (según `10-cloud/05-secretos-produccion.md`).
- Un almacén externo conectado al clúster.

Para cada opción: qué protege, qué **no** protege, quién puede leer el secreto, cómo se rota y cuánto cuesta operarla. Si citan una cifra o un comportamiento, con su fuente (la documentación o el libro con capítulo).

#### 3. Decisión

Cuál eligen, y las consecuencias. Respondan específicamente:

- ¿Cómo evitan que el valor quede en el repositorio y en la anotación `last-applied-configuration`?
- ¿Quién puede leer el Secret hoy y quién debería poder leerlo?
- ¿Qué pasa si el secreto se filtra: cómo se rota y cuánto tarda?

#### 4. Datos

Una sección corta con la decisión de **dónde vive su base de datos**: dentro del clúster o fuera, quién hace el respaldo, quién lo restaura y **cuándo se probó** una restauración. Si todavía no se probó, dígalo: es el hallazgo más importante.

---

## Entregable

- `docs/secretos-en-produccion.md`, con el inventario, las dos opciones, la decisión y la sección de datos.
- La salida de los comandos de la práctica que prueban sus dos afirmaciones: el secreto decodificado con `base64 -d` y la fila que sobrevive al borrado del pod.
- Una línea, honesta: "lo que **no** sé todavía sobre secretos en mi proyecto es ...".

---

## Criterio de evaluación

| Aspecto | Peso | Qué se busca |
|---------|------|--------------|
| Inventario completo | 20 % | No faltan secretos; los que están en el repositorio quedan marcados |
| Dos opciones reales | 25 % | Se explican con lo que protegen y lo que no protegen; sin exagerar ni minimizar |
| Decisión y consecuencias | 25 % | Responde a rotación, lectura y filtración; no promete lo que la opción no da |
| Datos | 20 % | Dice quién respalda y quién restaura, y si se probó |
| Fuentes | 10 % | Cada afirmación comprobable cita su fuente o la salida de un comando |

Una decisión de "no correr la base en el clúster", con buenas razones, es tan válida como la contraria.

---

## Para la próxima sesión

Lleven su clúster con el Secret ya creado. En la sesión 4 se agregan RBAC, políticas de seguridad y una NetworkPolicy que aísla la base.
