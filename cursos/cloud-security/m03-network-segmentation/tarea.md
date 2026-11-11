# Tarea - M03 - Inventario de reglas de red

**Seguridad en la nube y Zero Trust**
**Tiempo estimado:** 2 horas

---

## Parte 1 - Lecturas (45 min)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/11-aws-vpc-networking.md` | Subredes públicas y privadas, Security Groups, NACLs, VPC Endpoints | 30 min |
| Documentación de AWS: "Control traffic to your AWS resources using security groups" | Buenas prácticas | 15 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *Practical Cloud Security* (Chris Dotson, 2023) | Cap. 6: seguridad de red, segmentación y filtrado de salida |
| *Cloud Native Security Cookbook* (Josh Armitage, 2022) | Cap. 5: redes seguras (recetas 5.2 y 5.3, bases de red en AWS y Azure) |
| *Defensive Security Handbook* (Amanda Berlin et al., 2024) | Caps. 16 y 17 |

---

## Parte 2 - Ejercicio (1 h 15 min)

Con el resultado de la práctica (o con la red de un laboratorio tuyo, **no la de un tercero**), entrega `docs/inventario-red.md` con:

### 1. Una tabla de reglas de entrada y salida

| Recurso | Dirección | Origen o destino | Puerto | Dueño | Para qué existe | ¿Se queda? |
|---|---|---|---|---|---|---|
| `sg-db` | Entrada | `sg-app` | 5432 | Equipo de datos | La API consulta la base | Sí |

Toda regla que **no** tenga dueño y razón se marca "se cierra" y se explica cómo se cierra.

### 2. Los tres hallazgos más graves

Para cada uno: qué es, por qué ocurre, cómo lo **detectaste**, cómo lo **corriges** y cómo lo **verificas** (el comando y su salida).

### 3. Una regla propia

Una política de checkov (YAML) o un chequeo de tu auditor que cubra algo que las reglas listas no cubren. Incluye el caso que **falla** y el que **pasa**.

### 4. El mapa de Azure

Elige tres filas de tu tabla y escribe su equivalente en Azure (NSG, ASG, Private Endpoint). Marca cuáles **no pudiste ejecutar** y por qué.

---

## Criterio de aceptación

- [ ] La tabla incluye reglas de **entrada y de salida**
- [ ] Cada corrección trae su verificación con salida real
- [ ] La regla propia tiene un caso que falla y uno que pasa
- [ ] Nada se probó contra recursos ajenos ni cuentas reales
- [ ] Lo que se basó en la documentación lleva su fuente y la fecha de consulta

## Criterios de evaluación

| Criterio | Peso |
|---|---|
| Inventario completo, con dueño y razón | 30 % |
| Detección, corrección y verificación de cada hallazgo | 35 % |
| Regla propia con caso que falla y caso que pasa | 20 % |
| Honestidad sobre lo no ejecutado | 15 % |
