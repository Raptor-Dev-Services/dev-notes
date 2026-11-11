<!-- _class: lead -->

# U4 - Gestión de la seguridad de la información

### Seguridad de la información

**Rogelio Arriaga - Raptor Dev Services**

<!--
Unidad de 5 horas. Hilo conductor: el ciclo de vida de un proyecto de SGSI, del patrocinio a la
recertificación. Se arma el expediente de arranque, se implanta con la declaración de aplicabilidad
y se prepara la auditoría. Lecturas: notas 10, 11 y 12 de 12-seguridad/04.
-->

---

## El lema de la unidad

> La certificación prueba que el sistema de gestión cumple la norma, no que la empresa sea segura.

---

## La seguridad es estrategia

- ISO, The Open Group, ISC2, CIS y otros publican marcos para homologar cómo se implanta y se evalúa la seguridad.
- La seguridad tiene que estar en la estrategia, no solo en la operación.
- Ejemplo: la regulación bancaria mexicana obliga a tener un CISO que reporte directo al director general (artículo 168 Bis 13 de las Disposiciones de la CNBV).

---

## Objetivos de la unidad

- Describir cómo se inicia un proyecto de SGSI a partir de las obligaciones de ISO/IEC 27001.
- Describir la documentación obligatoria y opcional del sistema.
- Analizar los tipos y mecanismos de auditoría interna y externa.

---

## Agenda

| Bloque | Tema |
|---|---|
| 1 | Activar el SGSI: dirección, caso de negocio, alcance, política |
| 2 | Inventario y metodología de riesgos |
| 3 | Declaración de aplicabilidad y tratamiento |
| 4 | Documentación, Anexo A y roles |
| 5 | Auditoría interna y certificación |
| 6 | Cierre |

---

<!-- _class: lead -->

# Parte 1
## Activar el SGSI

---

## La familia en el proyecto

| Norma | Papel |
|---|---|
| 27001 | Requisitos y Anexo A |
| 27002 | Cómo implantar cada control |
| 27003 | Guía de implantación |
| 27004 | Medición del desempeño |
| 27005 | Gestión de riesgos |

Vigentes: 27001 y 27002 de 2022.

---

## Sin dirección no hay proyecto

La dirección:

- respalda la seguridad desde el inicio, con políticas;
- asigna responsabilidades explícitas;
- aprueba la política, asigna recursos, nombra roles y revisa la implantación.

Sin recursos financieros, humanos y materiales, el proyecto no termina.

<!--
Pide a cada equipo una frase de 20 segundos para convencer a un director general. Si la frase usa
las palabras "firewall" o "hackers", vuelve a empezar.
-->

---

## El caso de negocio

- Problema u oportunidad
- Opciones
- Costos y beneficios
- Riesgos del proyecto
- Recomendación

Se presenta para que todos entiendan lo que el proyecto rinde.

---

## El argumento legal (montos de 2017)

| Norma | Sanción |
|---|---|
| Ley de datos personales | Hasta 10 años de prisión y 44.8 millones de pesos |
| Código Penal Federal | Hasta 8 años de prisión |
| Ley de la Propiedad Industrial | Hasta 10 años de prisión y 14 millones de pesos |
| Código de Comercio | Según el caso |

Actualización: nueva ley de datos personales (2025, multas en UMA); la LPI fue sustituida por la LFPPI (2020).

---

## Alcance

- Según negocio, organización, ubicación, activos y tecnología.
- Toda exclusión se justifica.
- Lo que queda fuera es menos confiable: la frontera necesita controles.
- Un alcance pequeño reduce el beneficio comercial.

---

## Política, inventario y metodología

- **Política:** el marco de todos los lineamientos.
- **Inventario:** tipo, ubicación, propietario, respaldo, licencia y valor de cada activo; se puede llevar en la CMDB de ITIL.
- **Metodología de riesgos:** homologa criterios; con línea base para detectar anomalías y guía para los auditores.

---

## Una buena evaluación de riesgos

- Identifica, cuantifica y prioriza contra los criterios.
- Estima (análisis) y compara (valoración).
- Guía las acciones y los controles.
- Se repite periódicamente y ante cambios.
- Tiene un alcance claro.

---

<!-- _class: lead -->

# Parte 2
## Implantar

---

## La declaración de aplicabilidad

- Lista los controles aplicados y excluidos, con su justificación.
- Sale de la evaluación de riesgos, de la ley, de los contratos y del negocio.
- Ordena la estrategia de los auditores y los recursos de la mejora.

| Control | ¿Aplica? | Razón | Estado | Evidencia |
|---|---|---|---|---|

---

## El plan de tratamiento

- Financiamiento, roles y responsabilidades.
- Implantar los controles elegidos.
- Definir cómo se mide la eficacia de cada control.
- Formación y sensibilización.

El residuo es el **riesgo residual**, y lo aprueba la dirección.

---

## La documentación obligatoria (2022)

Alcance, política, procesos de evaluación y tratamiento, declaración de aplicabilidad, objetivos, evidencia de competencia, planeación operativa, resultados de riesgos, seguimiento y medición, programa y resultados de auditoría, revisión por la dirección, no conformidades y acciones correctivas.

---

## De 14 dominios a 4 temas

| 2013: dominios A.5 a A.18 | 2022: temas |
|---|---|
| Políticas, organización, recursos humanos, activos, acceso, criptografía, física, operaciones, comunicaciones, desarrollo, proveedores, incidentes, continuidad, cumplimiento | Organizacionales (37), de personas (8), físicos (14), tecnológicos (34) |

93 controles con atributos para filtrarlos.

---

## Roles

| Rol | Lo esencial |
|---|---|
| Comité de seguridad | Estrategia y aprobación de políticas |
| Área de seguridad | Vigila incidentes, aprueba proyectos, define métodos |
| Propietario del activo | Clasifica y decide accesos |
| Custodio | Opera controles, accesos, respaldos y bitácoras |
| Usuario | Cumple políticas y reporta |

---

<!-- _class: lead -->

# Parte 3
## Auditar y certificar

---

## Evaluar todo el tiempo

- Revisión por la dirección al menos una vez al año.
- Auditoría interna antes de la externa.
- Los registros son la evidencia.
- El sistema necesita operar un tiempo antes de certificarse.

---

## Qué es una auditoría

Proceso sistemático, independiente y documentado para obtener evidencias y evaluarlas con objetividad frente a los criterios.

- **Alcance:** ubicaciones, unidades, procesos y periodo.
- **Evidencia objetiva:** verificable y basada en la observación.
- **Hallazgo:** evidencia evaluada contra criterios.

---

## Las dos fases de certificación

| Fase | Qué revisa |
|---|---|
| 1 | Documentación: política, objetivos, alcance, riesgos, declaración de aplicabilidad |
| 2 | En sitio: que la organización cumple lo que dice y que el SGSI es eficaz |

---

## El ciclo del certificado

| Auditoría | Cuándo |
|---|---|
| Certificación | Al inicio |
| Seguimiento | Cada seis meses o cada año |
| Recertificación | A los tres años |

---

## Tipos de hallazgo

| Hallazgo | Qué exige |
|---|---|
| Observación | Analizarla |
| No conformidad menor | Plan de acción |
| No conformidad mayor | Corregir antes de certificar |

<!--
Remarca que la acción correctiva ataca la causa, no el síntoma. Y que el certificado lo emite un
organismo acreditado (en México, por la EMA), consultable en IAF CertSearch.
-->

---

## Lo que un certificado no prueba

- Que la empresa sea segura.
- Que los controles estén bien elegidos en sentido técnico.

Para eso: evaluaciones de seguridad, pruebas de penetración y métricas de eficacia de los controles.

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Conclusiones

- El caso de negocio comunica beneficios y requisitos a la dirección.
- El alcance dice a qué y a quién aplica el sistema; la política, sus reglas.
- El riesgo residual siempre existe.
- La declaración de aplicabilidad es la guía obligatoria de los controles.
- La auditoría interna es requisito y genera evidencia; la externa verifica conformidad.
- Con un SGSI hay que seguir evaluando la seguridad todo el tiempo.

---

## Práctica y tarea

- **Práctica:** ¿qué controles del Anexo A pide en la práctica el Reglamento General de Protección de Datos? ¿Hace falta un SGSI completo?
- **Tarea:** caso de negocio para implantar un SGSI.

---

## Siguiente unidad

> Un plan de continuidad que nunca se probó es una hipótesis.

Planes de contingencia.
