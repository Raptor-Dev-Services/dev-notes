<!-- _class: lead -->

# U5 - Planes de contingencia

### Seguridad de la información

**Rogelio Arriaga - Raptor Dev Services**

<!--
Unidad de 5 horas. Hilo conductor: diseñar en frío para no improvisar en caliente. Se distingue cada
plan por su objetivo, se construye el BIA con sus métricas y se cierra con las pruebas. Lecturas:
notas 13, 14 y 15 de 12-seguridad/04.
-->

---

## El lema de la unidad

> Un plan de continuidad que nunca se probó es una hipótesis.

---

## ¿Está tu empresa preparada para un desastre?

- Crecer, proteger a la gente y servir a la sociedad exige proteger la empresa también en la peor crisis.
- Ser competitivo no basta: hay que ser **resiliente** ante incendios, sismos, accidentes o ataques.
- Continuidad del negocio y recuperación de desastres (BC/DR) ya se tratan como un solo programa.

---

## Objetivos de la unidad

- Comprender la continuidad y la resiliencia con las metodologías y marcos internacionales.
- Diseñar una estrategia para crear e implantar un plan de continuidad con los marcos de la Unión Europea y de Estados Unidos.
- Establecer una metodología para probar los planes y mejorarlos.

---

## Agenda

| Bloque | Tema |
|---|---|
| 1 | Continuidad, resiliencia y tipos de planes |
| 2 | NIST frente a ENISA |
| 3 | Organización y equipos del plan |
| 4 | BIA: MTD, RTO, RPO |
| 5 | Controles preventivos y estrategias |
| 6 | Pruebas y mantenimiento |
| 7 | Cierre |

---

<!-- _class: lead -->

# Parte 1
## Continuidad y resiliencia

---

## BCP o DRP

| | BCP | DRP |
|---|---|---|
| Reanuda | Procesos del negocio | Servicios informáticos críticos |
| Responsable | Unidades de negocio | Tecnologías de la información |

El DRP es una pieza del BCP.

---

## Para qué sirve el plan

- Proteger al personal y los activos.
- Reanudar procesos críticos en tiempos tolerables.
- Tomar las menores decisiones posibles en plena crisis.
- Mantener el servicio al cliente y responder a inversionistas.
- Cumplir ley, contratos y auditoría.
- No depender de una sola persona ni inventar procedimientos.

---

## Los tipos de planes (NIST SP 800-34)

| Plan | Alcance |
|---|---|
| Continuidad del negocio | Procesos de negocio |
| Continuidad de operaciones | Funciones esenciales en sitio alterno, hasta 30 días |
| Comunicación de crisis | Mensajes internos y externos |
| Protección de infraestructura crítica | Activos del sector |
| Respuesta a incidentes cibernéticos | Ataques |
| Recuperación de desastres | Reubicar sistemas |
| Contingencia del sistema | Un sistema |
| Emergencia para ocupantes | Personas en una instalación |

---

## Los siete pasos del NIST

1. Política
2. Análisis de impacto al negocio
3. Controles preventivos
4. Estrategias de contingencia
5. Plan de contingencia
6. Pruebas, capacitación y ejercicios
7. Mantenimiento

---

## NIST frente a ENISA

| NIST | ENISA |
|---|---|
| Orientado a controles | Orientado a riesgos |
| Política, BIA, controles, estrategias, plan, pruebas, mantenimiento | Iniciar programa, conocer la organización, responsabilidades, política, BIA, enfoque, implantar, probar, mantener |

El inicio cambia; el centro se parece. La continuidad es un resultado obligado de la gestión del riesgo.

<!--
Actualizaciones para mencionar: ISO 22301:2019 vigente, con su enmienda 1:2024 sobre cambio climático; en ISO/IEC 27001:2022 el antiguo A.17 vive
en los controles 5.29 y 5.30; NIS2 pide continuidad a las entidades esenciales en la UE.
-->

---

<!-- _class: lead -->

# Parte 2
## Diseñar el plan

---

## Antes de empezar

- ¿Qué departamentos, dependencias e instalaciones cubre?
- ¿Continuidad del negocio o de TI?
- Del catálogo de amenazas: las más probables y las de impacto muy alto.
- Con presupuesto y apetito de riesgo se fijan las premisas.

---

## Arranque del proyecto

- Iniciativa aprobada por la alta dirección, alineada al apetito de riesgo.
- Entender la organización: actividades críticas, estructura, partes interesadas.
- Un responsable con autoridad; personas que entreguen y mantengan el programa.

---

## Quién hace qué

| Figura | Papel |
|---|---|
| Gerente de continuidad | Entrega y mantiene el plan |
| Comité de dirección de continuidad | Gobierna: revisa, prueba y actualiza |
| Oro (alta dirección) | Estrategia, partes interesadas, medios |
| Plata (gestión de incidentes) | Mando y control |
| Bronce (respuesta) | Emergencias y evacuación en las primeras horas |

---

## La política de contingencia

Roles, alcance, recursos, capacitación, calendario de pruebas, calendario de mantenimiento y frecuencia mínima de respaldos.

---

## El análisis de impacto al negocio

Relaciona cada componente de TI con el proceso crítico que sostiene.

1. Procesos y su criticidad de recuperación
2. Recursos necesarios
3. Prioridades de recuperación

Es el punto de partida del plan.

---

## Las métricas

| Métrica | Pregunta |
|---|---|
| MTD | ¿Cuánto aguanta el negocio detenido? |
| RTO | ¿En cuánto tiempo vuelve el servicio mínimo? |
| RPO | ¿Cuántos datos estamos dispuestos a perder? |
| ROL | ¿Con qué nivel de servicio se da por recuperado? |

Regla: RTO menor que MTD. El RPO manda en la política de respaldos.

<!--
Ejercicio: facturación con MTD de 48 horas. Si el RTO es de 72, ¿qué pasa? El plan no sirve. Si el
RPO es de 4 horas, ¿alcanza un respaldo nocturno? No.
-->

---

## Controles preventivos

- UPS y planta generadora
- Aire acondicionado redundante
- Detección y supresión de incendios
- Sensores de agua
- Contenedores resistentes para respaldos
- Respaldos frecuentes y fuera del sitio

---

## Sitios alternos

| Sitio | Listo en | Costo |
|---|---|---|
| Frío | Semanas | Bajo |
| Templado | Días | Medio |
| Caliente | Horas | Alto |
| Espejo | Minutos | Muy alto |

También: acuerdos recíprocos, proveedores comerciales y recuperación en la nube.

---

## Planes complementarios (INCIBE)

| Fase | Contenido |
|---|---|
| 0 | Alcance |
| 1 | Análisis de la organización: BIA y riesgos |
| 2 | Estrategia |
| 3 | Crisis, recuperación de entornos, procedimientos técnicos |
| 4 | Pruebas y mantenimiento |
| 5 | Concienciación |

---

## Manejo de incidentes

- NIST SP 800-61 Rev. 2 (2012): preparación; detección y análisis; contención, erradicación y recuperación; actividades posteriores.
- Rev. 3 (2025): integrada a las seis funciones del CSF 2.0.

---

## Lo que pide la regulación bancaria

- El director general elabora el plan de continuidad según el Anexo 67 de las Disposiciones de la CNBV; lo aprueba el consejo (artículo 164 Bis).
- Capacitación, política de comunicación de contingencias.
- Aviso a la CNBV dentro de 60 minutos de contingencias de 60 minutos o más; análisis de causas en 15 días hábiles.

---

<!-- _class: lead -->

# Parte 3
## Probar y mantener

---

## Tipos de prueba

| Tipo | Riesgo para la operación |
|---|---|
| Revisión documental | Ninguno |
| Recorrido estructurado | Ninguno |
| Ejercicio de mesa | Ninguno |
| Simulación o prueba funcional | Bajo |
| Prueba paralela | Bajo a medio |
| Ensayo general | Medio |
| Simulacro con interrupción | Alto |

---

## Planear y conducir

- Objetivos y criterios de éxito medibles.
- Escenario realista, en etapas.
- Facilitador, participantes y observadores con cuestionario.
- Reunión posterior inmediata; responsables de cada mejora.
- Informe al comité de continuidad.

---

## Declarar un desastre

1. Identificar funciones afectadas.
2. Estimar el tiempo de restauración.
3. Si supera el MTD: declarar y activar el plan.

---

## Mantener el plan

- Revisión anual y ante disparadores: cambios de estructura, sistemas, proveedores, personal, regulación, incidentes o pruebas.
- Control de versiones y de cambios.
- Contactos, inventarios y acuerdos al día.
- Volver a probar lo que cambió.

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Conclusiones

- El plan de continuidad organiza la recuperación de las funciones críticas.
- La gestión de la continuidad es certificable y de mejora continua.
- El BIA es parte de la gestión de riesgos y el inicio del plan.
- El RPO dice cuánto se puede perder; el RTO, cuánto tarda en volver.
- El ensayo general no interrumpe; el simulacro sí.
- La eficiencia del plan está en probarlo y mejorarlo.

---

## Práctica y tarea

- **Práctica:** qué exige la regulación bancaria a los planes de continuidad y cómo se evalúan.
- **Tarea:** análisis de impacto al negocio de una institución de crédito.

---

## Fin de la materia

Con esta unidad se completa el expediente: conceptos, organización, controles, gestión y continuidad.

> Antes de proteger hay que saber qué vale, contra qué y cuánto cuesta no hacerlo.
