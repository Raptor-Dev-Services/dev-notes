<!-- _class: lead -->

# U2 - Seguridad de la información en las organizaciones

### Seguridad de la información

**Rogelio Arriaga - Raptor Dev Services**

<!--
Unidad de 5 horas. Hilo conductor: pasar de administrador de herramientas a consultor estratega. Tres
movimientos: justificar un proyecto con lenguaje de negocio, dimensionar la complejidad y elegir un
marco con criterio. Lecturas: notas 04, 05 y 06 de 12-seguridad/04.
-->

---

## El lema de la unidad

> Si un proyecto no contribuye a los objetivos más altos de la organización, no debería ejecutarse.

---

## Objetivos de la unidad

- Analizar con sentido crítico la necesidad de proyectos de seguridad que aporten valor, usando marcos de referencia.
- Identificar los elementos que entran en un plan de seguridad institucional con herramientas de análisis organizacional.
- Analizar distintos marcos de trabajo para armar uno propio, acorde con las necesidades de la organización.

---

## Por qué hay tantos marcos

- Las posturas de seguridad nacen de aspectos culturales, técnicos y comerciales.
- No hay acuerdo sobre qué debe incluir un marco.
- Se comparan marcos de alto nivel con otros pensados para vender productos.
- La diversidad es también una oportunidad: tomar lo mejor de cada uno.

---

## Agenda

| Bloque | Tema |
|---|---|
| 1 | ¿Gasto o inversión? |
| 2 | Evaluar proyectos |
| 3 | Las cuatro dimensiones de la complejidad |
| 4 | Lo crítico: procesos, cadena de valor, infraestructura |
| 5 | Orden Ejecutiva 13636 y el CSF del NIST |
| 6 | SABSA y Zachman |
| 7 | Cierre |

---

<!-- _class: lead -->

# Parte 1
## ¿Gasto o inversión?

---

## No todas las organizaciones son iguales

- Empresas privadas, organizaciones sin fines de lucro, entidades de servicio público.
- Un hospital privado y una clínica pública hacen lo mismo, pero no protegen igual.
- La diferencia: **cultura organizacional** (misión, visión, normatividad, valores).
- Esa cultura fija objetivos, alcance y prioridad de la seguridad.

---

## La cadena de preguntas

1. ¿Qué debo proteger?
2. ¿De qué o de quién?
3. ¿Cuánto debo gastar?
4. ¿Un firewall o una capacitación son inversión o gasto?
5. ¿Por qué invertir en seguridad?

La existencia de amenazas no es, sola, razón para invertir.

<!--
Ejercicio rápido: pide a dos personas que defiendan la compra de un firewall, una con estadísticas
de ataques y otra con un proceso de negocio que se protege. Que el grupo vote cuál convence.
-->

---

## Qué es un proyecto, según quién mire

| Perspectiva | Definición |
|---|---|
| Dirección de proyectos | Esfuerzo temporal para un resultado único |
| Economía | Flujo de fondos en el tiempo |
| Evaluación de proyectos | Recursos que se transforman para generar más valor del que cuestan |

---

## Evaluar antes de proponer

- Objetivos claros
- Restricciones y cómo manejarlas
- Menor costo y tiempo
- Mayor seguridad frente a varios riesgos
- Varias opciones de solución
- Riesgos del propio proyecto
- Relación costo-beneficio

---

## Inversión o proyecto social

| | Inversión | Social |
|---|---|---|
| Busca | Utilidades | Impacto en la sociedad |
| Se evalúa con | VPN, TIR, ROI, recuperación | Beneficios y costos sociales |
| Ejemplo | Un centro de operaciones que reduce fraudes | Una estrategia nacional de ciberseguridad |

---

## Retorno de la inversión en seguridad

- Pérdida anual esperada = costo de un incidente x veces al año.
- Filtración de 2 millones, una vez cada cuatro años: 500 mil al año.
- Un control de 150 mil al año la baja a 100 mil: ahorra 400 mil.
- Retorno: (400 - 150) / 150, cerca de 167 %.

---

## Tres casos para discutir

| Caso | Beneficio | Métrica |
|---|---|---|
| Concientización en ventanilla bancaria | Menos fraude por ingeniería social | Incidentes y montos |
| Parches en el servidor web | Menos intrusiones y caídas | Vulnerabilidades abiertas, disponibilidad |
| Estrategia nacional de ciberseguridad | Confianza y protección de infraestructura | Indicadores de impacto |

Actualización: la Estrategia Digital Nacional 2021-2024 concluyó; la política digital está hoy a cargo de la ATDT.

---

<!-- _class: lead -->

# Parte 2
## La complejidad

---

## Dimensión legal y normativa

- **Datos personales:** principios, deberes de seguridad y confidencialidad, derechos ARCO.
- **PCI DSS:** 12 requisitos para quien maneja tarjetas (vigente v4.0.1).
- **Regulación sectorial y contratos.**

Actualización: nueva Ley Federal de Protección de Datos Personales en Posesión de los Particulares (DOF 20 de marzo de 2025); el INAI desapareció y su función pasó a la Secretaría Anticorrupción y Buen Gobierno.

---

## Dimensión matemática

- **Criptografía:** la herramienta de la confidencialidad. "Encriptar" es el calco técnico de cifrar.
- **Aritmética modular:** fácil de calcular, difícil de revertir sin el secreto.
- **Curvas elípticas:** la misma fuerza con llaves mucho más cortas; ya son estándar.
- **Poscuántica:** FIPS 203, 204 y 205 (2024).

---

## Dimensiones tecnológica y geopolítica

- Vulnerabilidades en el teléfono más nuevo afectan a millones al mismo tiempo.
- Una DLL maliciosa corre con los permisos del programa que la carga.
- UTM, firewall de nueva generación, *appliance*: muchos nombres para lo mismo.
- Hardware, software y normas cruzan fronteras y reflejan la competencia entre países.

---

<!-- _class: lead -->

# Parte 3
## Lo crítico primero

---

## Infraestructura crítica

- Sistemas y activos tan vitales que su incapacidad debilitaría la seguridad, la economía o la salud pública.
- Producen los **servicios esenciales**.
- En una empresa: los servicios esenciales salen de los **procesos críticos**.

Un empleado suele describir sus tareas diarias, no los procesos críticos.

---

## La cadena de valor de Porter

| Primarias | De apoyo |
|---|---|
| Logística de entrada | Infraestructura de la empresa |
| Operaciones | Recursos humanos |
| Logística de salida | Desarrollo de tecnología |
| Mercadotecnia y ventas | Abastecimiento |
| Servicio | |

Un proceso crítico se parece a uno primario: si se detiene, el margen desaparece.

---

## A cualquier escala

| Organización | Infraestructura crítica |
|---|---|
| Pyme | Internet y el servidor con los datos de operación |
| Empresa mediana | ERP, red entre sucursales, centro de datos |
| Estado | SCADA de oleoductos, red financiera, telecomunicaciones |

---

<!-- _class: lead -->

# Parte 4
## La visión de Estados Unidos

---

## Orden Ejecutiva 13636 (2013)

- Intercambio de información sobre amenazas con el sector privado.
- Protección de la privacidad y las libertades civiles.
- Encargo al NIST de un marco voluntario de ciberseguridad.
- Identificación de la infraestructura en mayor riesgo.

Actualización: la PPD-21 se sustituyó por el NSM-22 (2024), con 16 sectores.

---

## El CSF del NIST

- **Núcleo:** funciones, categorías, subcategorías y referencias.
- **Niveles:** parcial, informado por el riesgo, repetible, adaptativo.
- **Perfiles:** actual y objetivo; la brecha es el plan.

Funciones (1.1): Identificar, Proteger, Detectar, Responder, Recuperar.

Actualización: CSF 2.0 (2024) suma **Gobernar** y se dirige a cualquier organización.

<!--
Mensaje para cerrar el bloque: cualquier iniciativa de seguridad necesita respaldo desde lo más alto
de la jerarquía. Lo dice el marco y lo confirma cada proyecto que fracasa sin patrocinio.
-->

---

<!-- _class: lead -->

# Parte 5
## Arquitecturas empresariales

---

## Zachman

- Matriz de seis preguntas (qué, cómo, dónde, quién, cuándo, por qué) por seis perspectivas.
- Es una **estructura** para describir la empresa, no una metodología.
- Junto con TOGAF y DoDAF, los marcos de arquitectura más citados.

---

## SABSA

| Capa | Vista |
|---|---|
| Contextual | Negocio |
| Conceptual | Arquitecto |
| Lógica | Diseñador |
| Física | Constructor |
| Componentes | Especialista |
| Operativa | Gestión del servicio |

Principios: **alineación** y **trazabilidad** en ambos sentidos.

---

## Las propuestas comerciales

- Cubren bien las capas física y de componentes.
- Casi nunca las capas contextual y conceptual.
- El estratega las ubica dentro de su plan, no al revés.

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Conclusiones

- TIR y VPN deciden si una inversión es viable; el ROI en seguridad se adapta.
- No todo proyecto es inversión: algunos se miden por impacto social.
- La criptografía sostiene la confidencialidad; las curvas elípticas ya son estándar.
- Una arquitectura integra componentes diversos con un objetivo común.
- Los objetivos de control salen del análisis de riesgos.
- La cadena de valor ayuda a identificar procesos críticos.

---

## Práctica y tarea

- **Práctica:** ¿qué marco de trabajo le conviene a mi organización? En equipo, cuatro marcos investigados y una elección justificada.
- **Tarea:** procesos críticos y catálogo de activos de su entorno laboral.

---

## Siguiente unidad

> Los controles lógicos más avanzados no sirven si alguien puede llegar al hardware.

Seguridad física y lógica.
