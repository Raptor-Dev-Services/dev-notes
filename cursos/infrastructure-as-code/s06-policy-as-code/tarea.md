# Tarea - S06 - Políticas como código

**Infraestructura como código**
**Tiempo estimado:** 5 a 6 horas (incluye el proyecto final)

---

## Parte 1 - Lecturas (1.5 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/09-cicd/07-policy-as-code.md` | Completa | 40 min |
| `dev-notes/09-cicd/01-checklists.md` | Shift-left y checklist de seguridad en el pipeline | 20 min |
| Documentación de OPA, "Policy Language" | `some ... in`, comprensiones, `not` e indefinido | 20 min |
| Documentación de Conftest | Writing rules, Exceptions y Testing policies | 15 min |

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Policy as Code* (Jimmy Ray, O'Reilly, 2024) | Cap. 1 - Policy as Code: A Gentle Introduction; Cap. 2 - Open Policy Agent; Cap. 11 - PaC and Infrastructure as Code; Cap. 12 - PaC and Terraform IaC |
| *Security as Code* (BK Sarthak Das y Virginia Chu, O'Reilly, 2023) | Cap. 3 - Securing Your Infrastructure; Cap. 5 - Controlling Access Through Automation |
| *Cloud Native Security Cookbook* (Josh Armitage, O'Reilly, 2022) | Cap. 6 - Infrastructure as Code; Cap. 7 - Compliance as Code |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: tres políticas de tu organización (2.5 a 3 horas)

Elige **tres reglas de seguridad o cumplimiento** de tu organización (tu equipo, tu proyecto o tu empresa) que hoy viven en un checklist o en la cabeza de alguien, y conviértelas en políticas de Conftest.

### Reglas del ejercicio

1. **Tres políticas distintas**, cada una en un archivo `.rego` con **su identificador** (`deny_<algo>`): una sobre **red o exposición**, una sobre **acceso o permisos** y una de **cumplimiento** (por ejemplo, etiquetas obligatorias o imagen con versión fija).
2. **Sobre un plan real cuando se pueda.** Al menos **una** debe correr sobre el plan JSON de recursos reales de OpenTofu (proveedores locales o Docker). Las demás pueden usar un plan reducido a mano, marcado **"Ejemplo ilustrativo"**.
3. **Tres pruebas por política:** un caso que debe rechazarse, uno que debe aceptarse y un caso borde (por ejemplo, un recurso que se está borrando, un valor `null` o una lista en lugar de texto).
4. **Mensaje de rechazo útil:** cada mensaje dice **qué recurso**, **qué regla** y **cómo se arregla**.
5. **Un "verlo fallar" por política:** muestra la política rechazando el plan inseguro (código de salida 1) y aceptándolo ya corregido (código 0).
6. **Un hueco:** busca **un caso** en el que una de tus políticas deje pasar algo inseguro (como pasó con `ip_protocol = "-1"`), documéntalo, arréglalo y agrega la prueba que lo cubre.
7. **Una excepción** documentada (archivo propio, por recurso concreto, con motivo, responsable y fecha de revisión en un comentario).
8. **Las políticas se prueban a sí mismas:** `conftest verify --policy policy` en verde. Si usas `opa`, `opa check --strict policy` limpio.
9. **Introduce al menos una regla como `warn`** y explica en qué fecha pasaría a `deny` y por qué.

### Qué entregar

```
politicas-organizacion/
├── policy/
│   ├── red.rego, red_test.rego
│   ├── acceso.rego, acceso_test.rego
│   ├── cumplimiento.rego, cumplimiento_test.rego
│   └── excepciones.rego
├── fixtures/              planes en JSON (reales o reducidos a mano, marcados)
├── .github/workflows/politicas.yml    validado con un parser
├── README.md              tabla "regla de la organización -> política -> prueba -> riesgo que cubre"
└── evidencia-s06/         salidas reales de cada política (falla y pasa) y del hueco
```

---

## Parte 3 - Proyecto final (2 a 3 horas de integración)

**Crear y endurecer un servidor desde cero con un solo comando.**

| Pieza | Qué se entrega | Cómo se ve fallar |
|---|---|---|
| OpenTofu | La infraestructura (servidor de laboratorio y lo que necesite) con estado remoto y bloqueo, módulo con versión fijada | Dos `apply` a la vez: el bloqueo lo impide |
| Ansible | El rol `endurecer` aplicado al servidor creado | La segunda corrida reporta `changed` (y luego `changed=0`) |
| CI | Un workflow con ansible-lint, Molecule y las políticas, en cada pull request | Un cambio malo por puerta deja el CI en rojo |
| Política | Al menos **tres** reglas sobre el plan, con pruebas | El plan con un recurso inseguro es rechazado |
| Punto de entrada | Un script o `make` que encadena crear, configurar y comprobar | Corre sin pasos manuales entre medias |

### Reglas del proyecto

- **Sin nubes reales ni costo:** proveedores locales, Docker o las VM del laboratorio. Lo que sea de nube real va marcado como **fragmento**.
- **Cada pieza, vista fallar**, con la salida real en `evidencia-final/`.
- **Sin secretos en el repositorio.** El estado y el `plan.json` **no** se suben.
- Un `README.md` con el comando único, los requisitos, las versiones fijadas (con "a noviembre de 2026") y lo que **no** cubre el proyecto.
- Una demostración de 10 minutos: se empieza de cero, se corre el comando único y se muestra cada pieza fallando.

---

## Criterios de evaluación

| Criterio | Peso | Qué se revisa |
|---|---|---|
| Políticas del ejercicio | 25 % | Tres políticas distintas, con identificador, mensajes útiles y pruebas de rechazo, aceptación y borde |
| Hueco y excepción | 10 % | El hueco es real, está arreglado y cubierto por prueba; la excepción está documentada y acotada |
| Proyecto: OpenTofu | 15 % | Estado remoto con bloqueo, módulo, versiones fijadas, `plan` revisable |
| Proyecto: Ansible | 15 % | Rol idempotente (`changed=0` en la segunda corrida), con `assert` de verificación |
| Proyecto: CI y política | 20 % | El workflow detiene un cambio malo por cada puerta y la política rechaza el plan inseguro |
| "Verlo fallar" | 10 % | Cada pieza con su fallo real, salida pegada y explicación |
| Documentación y veracidad | 5 % | README claro, versiones y fechas, lo ficticio marcado, nada de secretos |

---

## Para ir pensando

- ¿Qué parte de tu infraestructura **no** pasa por un plan (y por lo tanto, ninguna política la ve)?
- ¿Quién aprueba las excepciones en tu equipo y cada cuánto se revisan?
- Si mañana nace una regla nueva, ¿en cuántos pasos llega al pipeline? ¿Quién la prueba?
