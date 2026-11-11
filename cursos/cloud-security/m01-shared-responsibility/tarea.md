# Tarea - M01 - Qué es nuestro, qué es del proveedor

**Seguridad en la nube y Zero Trust**
**Tiempo estimado:** 2 horas

---

## Parte 1 - Lecturas (45 min)

| Recurso | Sección |
|---|---|
| `dev-notes/10-cloud/01-aws-basics.md` | El modelo de responsabilidad compartida y los servicios esenciales |
| `dev-notes/10-cloud/06-costos.md` | Etiquetado y recursos con costo oculto |
| AWS, "Shared Responsibility Model" | Controles heredados, compartidos y específicos |
| Microsoft Learn, "Shared responsibility in the cloud" | La matriz completa |

Libros recomendados: *Practical Cloud Security* (Dotson, 2023), caps. 1 a 3. *AWS Certified Security - Specialty (SCS-C02) Exam Guide* (Book y Scott, 2024), cap. 1.

---

## Parte 2 - Entregable (75 min)

Con el inventario de la práctica, entrega `responsabilidad.md` con:

1. **Tabla de responsabilidades** de las cuatro piezas del Taller Demo (`api-lab`, `db-lab`, `lab-datos`, la cuenta), con la columna "del proveedor" y "nuestra". Cada fila cita la fuente oficial (nube y página).
2. **Los dos pendientes:** propón y aplica (en el simulador) la etiqueta de clasificación de la instancia y una convención para etiquetar usuarios. Muestra el inventario antes y después.
3. **Tres frases mal pensadas** como las de la clase ("ya está cifrado porque está en la nube"), cada una reescrita para decir qué hace el proveedor y qué haces tú.
4. **Una regla** de una línea para el equipo: "todo recurso nuevo lleva `taller:owner` y `taller:clasificacion` o no se crea". Di cómo la verificarías sin acordarte (pista: módulo 6).

---

## Criterio de evaluación

- [ ] Cada afirmación de responsabilidad cita la documentación oficial de la nube correspondiente
- [ ] El inventario "después" tiene cero filas sin dueño en buckets e instancia
- [ ] Las tres frases corregidas separan lo del proveedor de lo propio
- [ ] No hay datos personales ni secretos en etiquetas
- [ ] Todo lo que se corrió, corrió en el simulador; nada contra una cuenta real
