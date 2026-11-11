# Tarea - M04 - La política Zero Trust de una aplicación

**Seguridad en la nube y Zero Trust**
**Tiempo estimado:** 2 horas

---

## Parte 1 - Lecturas (45 min)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/08-zero-trust.md` | Modelo perimetral vs Zero Trust, mTLS, Least Privilege, Segmentación | 30 min |
| NIST SP 800-207, "Zero Trust Architecture" (agosto de 2020) | Sección 2.1 (principios) y sección 3 (componentes lógicos) | 15 min |

Mientras lees la nota de Dev Notes, anota qué afirmaciones son de **tu nota** y cuáles de **NIST**: la nota habla de cinco "pilares" que son su síntesis, distintos de los siete principios de NIST y de los cinco pilares de CISA.

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *Zero Trust Networks* (Razi Rais et al., 2024) | Caps. 1 a 9 (fundamentos, confianza, decisiones de autorización, dispositivos, identidades, aplicaciones, tráfico, realización) |
| *Identity-Native Infrastructure Access Management* (Ev Kontsevoy et al., 2023) | Caps. 2 a 6 |
| *Practical Cloud Security* (Chris Dotson, 2023) | Cap. 1 |

---

## Parte 2 - Ejercicio (1 h 15 min)

Elige **una aplicación tuya** (si no tienes, usa `api-lab/facturas`) y entrega `docs/politica-zero-trust.md` con:

### 1. La política como dato

Un archivo (JSON o YAML) que declare, por recurso: quién accede (roles), qué atributos exige (MFA, estado del equipo, horario u otros), duración de la sesión y qué se registra. **Todo lo no declarado se deniega.**

### 2. Diez escenarios con su resultado esperado

Una tabla con el escenario, la decisión esperada, el componente que decide (PE, PA o PEP) y la razón. Al menos **tres** deben ser denegaciones por razones distintas y uno debe ser una **revocación con la sesión vigente**.

### 3. La prueba de que las pruebas sirven

Para dos controles distintos, muestra qué escenario **cae** cuando lo quitas, con la salida real. Si no cae ninguno, agrega el escenario que falta.

### 4. Plan de migración desde VPN

Los seis pasos de la clase aplicados a tu caso: inventario, política, modo informe, corte de ruta, limpieza y registro. Marca qué paso **no puedes** ejecutar todavía y por qué.

### 5. Equivalente en nube

Escribe cómo se expresaría tu política en AWS (IAM, Verified Access o Session Manager) o en Azure (Conditional Access). Lo que no ejecutes va marcado como **fragmento** y con su fuente (documentación oficial, con fecha de consulta).

---

## Criterio de aceptación

- [ ] La política es un archivo, no código disperso, y **deniega por omisión**
- [ ] Hay una revocación que corta una sesión vigente
- [ ] Hay una mutación (control quitado) con la salida de la prueba que cae
- [ ] El plan de migración incluye modo informe **antes** de cortar
- [ ] Ninguna afirmación sobre AWS, Azure o NIST va sin fuente y fecha

## Criterios de evaluación

| Criterio | Peso |
|---|---|
| Política declarativa, completa y con denegación por omisión | 30 % |
| Escenarios con componente y razón; revocación incluida | 25 % |
| Prueba de mutación con evidencia | 25 % |
| Plan de migración y honestidad sobre lo no ejecutado | 20 % |
