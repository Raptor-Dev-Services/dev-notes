# Tarea - M05 - Detección y respuesta en la nube

**Seguridad en la nube y Zero Trust**
**Tiempo estimado:** 2 horas

---

## Parte 1 - Lecturas (45 min)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/15-aws-iam-seguridad.md` | Auditoría con CloudTrail e IAM Access Analyzer | 15 min |
| `dev-notes/10-cloud/16-aws-cloudwatch.md` | CloudWatch Logs, filtros y alarmas | 30 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *Practical Cloud Security* (Chris Dotson, 2023) | Cap. 7 - detectar, responder y recuperarse de incidentes |
| *AWS Certified Security - Specialty (SCS-C02) Exam Guide* (Adam Book y Stuart Scott, 2024) | Caps. 4 a 9 - respuesta a incidentes, Config, Security Hub y GuardDuty, bitácoras |
| *Incident Response in the Age of Cloud* (Erdal Ozkaya, 2021) | Cap. 11 - respuesta a incidentes en la nube |
| *Cloud Native Security Cookbook* (Josh Armitage, 2022) | Cap. 3 - visibilidad de seguridad a escala (recetas 3.5, 3.6, 3.8, 3.9) |

---

## Parte 2 - Ejercicio: tus tres reglas con su prueba (75 min)

Parte del registro `eventos-lab.json` de la práctica. Agrega al menos **seis eventos nuevos** (inventados por ti, con IP de documentación y la cuenta ficticia `111122223333`) para tres conductas que no cubre `buscar.py`. Algunas ideas, elige tres:

- Un cambio de IAM (`eventSource` `iam.amazonaws.com`, `readOnly` falso)
- Un inicio de sesión de consola sin MFA de un usuario que no es root
- Un `DeleteBucketPolicy` o un cambio en el bloqueo de acceso público
- Un `errorCode` distinto de `AccessDenied`
- Una identidad que aparece por primera vez desde una IP que no figura en el resto del archivo

Para **cada** regla entrega:

1. **El código** (una función o un bloque de `buscar.py`)
2. **Su prueba positiva:** el evento que debe dispararla y la salida donde aparece
3. **Su prueba negativa:** un evento parecido que **no** debe dispararla y la salida donde no aparece
4. **Una línea** de qué decisión toma quien recibe la alerta
5. **Cómo se verifica que sigue funcionando** dentro de seis meses

Después, escribe `analisis.md` (máximo una página) con:

- Qué ruido esperas de cada regla en una cuenta real y cómo lo reducirías, sin excluir por nombre parcial
- Qué conducta importante **no** cubren tus reglas ni GuardDuty ni Config, y quién debería cubrirla

---

## Criterio de aceptación

- [ ] Las tres reglas corren con `python3 buscar.py <archivo>` y la salida está pegada en la entrega
- [ ] Cada regla tiene prueba positiva **y** negativa, con la salida real
- [ ] Los eventos inventados están rotulados como ejemplo ilustrativo y usan solo IP de documentación
- [ ] `analisis.md` nombra una limitación real de tus reglas, no una frase general
- [ ] No usaste una cuenta de nube real

## Evaluación

| Criterio | Peso |
|---|---|
| Reglas correctas y probadas en positivo y negativo | 50 % |
| Claridad de la decisión que habilita cada alerta | 20 % |
| Análisis de ruido y de lo no cubierto | 20 % |
| Se entiende sin explicarla de viva voz | 10 % |
