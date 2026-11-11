# Tarea - M02 - Auditar una identidad

**Seguridad en la nube y Zero Trust**
**Tiempo estimado:** 2 horas

---

## Parte 1 - Lecturas (45 min)

| Recurso | Sección |
|---|---|
| `dev-notes/10-cloud/15-aws-iam-seguridad.md` | Roles, políticas, evaluación, mínimo privilegio, OIDC |
| AWS IAM User Guide, "Security best practices in IAM" | Los 14 apartados |
| AWS IAM User Guide, "Policy evaluation logic" | Combinaciones de políticas |

Libros: *Practical Cloud Security* (Dotson, 2023), cap. 4. *Identity-Native Infrastructure Access Management* (Kontsevoy et al., 2023), caps. 2 a 5.

---

## Parte 2 - Entregable (75 min)

Entrega `identidades.md`:

1. **Una política del Taller Demo** (la que quieras: acceso de `api-lab` a la base, a un secreto o al bucket) escrita primero **amplia** y luego **mínima**.
2. **La prueba:** un script como `probar.py` que muestre qué se permite y qué se niega con cada versión, con su salida real en el simulador.
3. **El revisor:** el resultado de checkov sobre la versión amplia y la mínima (pegado). Di una cosa que checkov **no** puede saber de tu política.
4. **Una política de confianza para un CI** (fragmento, sin ejecutar): para un rol que asume un workflow de GitHub Actions, con `aud` y un `sub` acotado a una rama. Cita la versión vigente de la documentación de GitHub y di qué cambió para repositorios nuevos.
5. **Tu lectura del reporte de credenciales** de la práctica: acción recomendada por cada fila.

---

## Criterio de evaluación

- [ ] La versión mínima niega, con prueba, al menos una acción que la amplia permitía
- [ ] Ninguna llave real en el entregable
- [ ] Las afirmaciones sobre AWS o GitHub citan la página oficial
- [ ] El punto 3 nombra un límite real del revisor automático
- [ ] Todo se corrió en el simulador; nada contra una cuenta real
