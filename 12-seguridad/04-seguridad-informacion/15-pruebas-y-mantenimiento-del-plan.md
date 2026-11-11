# 15 - Pruebas y mantenimiento del plan de continuidad

> Unidad 5, tema 3 de la materia *Seguridad de la información*. Tipos de prueba, cómo se planea y se conduce un ejercicio, cómo se elige el escenario, el informe posterior y el mantenimiento del plan con control de cambios. Revisado a noviembre de 2026.

## Por qué importa

Un plan de continuidad que nunca se probó es una hipótesis. Las pruebas evalúan si lo documentado funciona en una situación parecida a la real, descubren oportunidades de mejora que generan valor y dan a la organización la confianza de que podrá sostener sus procesos críticos en un desastre. También son el primer paso del mantenimiento: de lo que salga de cada prueba dependen las mejoras y los cambios que mantienen el plan alineado con la operación y con la estrategia. La eficacia de un plan está en su ciclo de prueba y mejora continua.

## Elegir el tipo de prueba

Hay varios tipos de prueba, y cada uno evalúa características distintas del plan. Elegir mal tiene un costo: **una prueba mal planeada puede poner en riesgo la continuidad que se quiere proteger**. Las preguntas para elegir tienen que ver con dos cosas: qué tan maduro y confiable es el plan, y cuánto riesgo tolera la empresa durante una prueba.

De menor a mayor realismo (y riesgo):

| Tipo | Cómo se hace | Qué valida | Riesgo para la operación |
|---|---|---|---|
| Revisión documental | Cada responsable revisa su parte del plan contra una lista de verificación | Que el plan esté completo y actualizado | Ninguno |
| Recorrido estructurado | Los equipos leen el plan juntos, paso por paso | Que los pasos tengan sentido y que cada quien conozca su papel | Ninguno |
| Ejercicio de mesa | Un facilitador plantea un escenario y los participantes discuten qué harían, sin tocar sistemas | Decisiones, coordinación y comunicación | Ninguno |
| Simulación o prueba funcional | Se ejecutan procedimientos reales en un entorno controlado: restaurar respaldos, levantar un sistema en el sitio alterno | Que los procedimientos técnicos funcionen y en qué tiempo | Bajo |
| Prueba paralela | El sitio alterno procesa en paralelo con el principal, que sigue operando | Que el sitio alterno alcance los objetivos de recuperación | Bajo a medio |
| Ensayo general | Se reproduce un incidente completo con todos los equipos y recursos, sin detener los servicios reales | El funcionamiento del plan de principio a fin en condiciones casi reales | Medio |
| Simulacro con interrupción | Se interrumpen de verdad los servicios, como pasaría en un desastre, con escenarios definidos | La capacidad real de recuperación | Alto |

Un **ensayo general** da una perspectiva realista del plan sin interrumpir los servicios; un **simulacro** con interrupción muestra la realidad completa, pero solo conviene cuando el plan ya es maduro y la organización acepta el riesgo.

La guía del NIST (SP 800-34) agrupa estas actividades como **pruebas, capacitación y ejercicios**: las pruebas evalúan componentes del sistema (por ejemplo, restaurar desde un respaldo), la capacitación prepara al personal para su papel, y los ejercicios pueden ser de mesa (basados en discusión) o funcionales (basados en operaciones).

## Planear la prueba

Para sacarle el mayor valor a una prueba hay que escribir un plan detallado:

- **Objetivos claros y criterios de éxito específicos.** Por ejemplo: "restaurar la base de clientes en el sitio alterno en menos de 4 horas (RTO) con una pérdida de datos menor a 1 hora (RPO)".
- **Elementos seleccionados** del plan que se van a probar.
- **Calendario** con plazos y participantes de cada prueba.
- **Alcance, objetivos, escenario y logística**, por escrito.
- **Personal externo** cuando conviene: servicios de emergencia, seguridad, autoridades locales de protección civil, especialistas y proveedores.
- **Cuestionarios para los observadores**, que anotarán lo que vean durante la prueba.

Antes de empezar, los participantes reciben toda la información necesaria y quedan bien enterados de la situación simulada.

## Elegir el escenario

El escenario debe ser **lo más realista posible**, para evaluar bien el plan y conseguir que los participantes se comprometan de verdad. Algunos criterios para elegirlo:

- que corresponda a una amenaza del catálogo con probabilidad o impacto altos (corte eléctrico prolongado, ransomware, pérdida del edificio, caída de un proveedor clave, ausencia de personal crítico);
- que obligue a usar las partes del plan que se quieren validar;
- que se desarrolle en etapas, con información nueva que el facilitador introduce conforme avanza el ejercicio;
- que cambie de una prueba a otra, para no entrenar solo un caso;
- que tenga un tiempo acotado y un final claro.

Elegir bien el escenario y la prueba mejora la continuidad y además fortalece la confianza en el plan.

## Conducir la prueba

- Los participantes, voluntarios, siguen los planes que les tocan, guiados por el **facilitador**.
- Los **observadores** documentan lo que ven y lo que perciben durante la prueba.
- Al terminar, facilitador y observadores se reúnen para documentar los resultados e identificar las lecciones aprendidas y las áreas de mejora.

### La reunión posterior

Tan pronto como sea posible después de la prueba se hace una reunión informativa (*debriefing*). Los participantes cuentan cómo se sintieron, qué salió mal y dónde podría mejorar su respuesta. A la reunión asisten todos: participantes, observadores y quienes mantienen el plan o lo activarían en el futuro. Al final se **asignan responsables** para cada mejora.

### El informe

El resultado final es un **informe de prueba** que describe el alcance, el enfoque, el método y los resultados, con recomendaciones y el responsable de cada acción. Su audiencia es el **comité de dirección de continuidad** (nota [14](14-plan-de-continuidad.md)). Con toda la información reunida durante la prueba se alimenta el plan de mantenimiento.

## Evaluar daños y declarar un desastre

Una prueba también ejercita la decisión más delicada del plan: cuándo se declara el desastre. La evaluación de daños sigue una secuencia:

1. Identificar las funciones de negocio afectadas.
2. Estimar cuánto tiempo tomará restaurar las funciones críticas.
3. Si ese tiempo supera el tiempo máximo tolerable de caída (MTD), declarar el desastre y activar el plan de continuidad.

Esperar a que la aseguradora determine el alcance total de los daños no forma parte de la evaluación: hacerlo retrasaría la activación del plan.

## Mantener el plan

El plan tiene que mantenerse para seguir siendo relevante y alineado con el negocio. Eso significa revisarlo con regularidad bajo un enfoque de **mejora continua** y **control de cambios**, para que responda a cualquier cambio en la empresa o en las amenazas.

### Cuándo revisarlo

Además de una revisión programada, al menos una vez al año, el plan se revisa cuando ocurre alguno de estos disparadores:

- cambios en la estructura, los procesos o las sedes de la organización;
- sistemas, aplicaciones o proveedores nuevos o retirados;
- rotación de personal clave o cambios en los datos de contacto;
- cambios en la regulación o en los contratos;
- un incidente real o una prueba que reveló fallas;
- un cambio relevante en el panorama de amenazas.

### Cómo mantenerlo

- Controlar las versiones del plan y registrar qué cambió, quién lo aprobó y cuándo.
- Distribuir la versión vigente a quienes la necesitan y retirar las anteriores.
- Mantener al día las listas de contacto, los inventarios de recursos y los acuerdos con proveedores y sitios alternos.
- Actualizar el BIA cuando cambien los procesos o sus tiempos tolerables.
- Volver a probar las partes que cambiaron.

Incluir la mejora continua y el control de cambios en la gestión del plan trae dos beneficios. Primero, la documentación se mantiene al día y refleja con precisión los procesos y procedimientos vigentes, lo que mejora la operación y la capacidad de responder a cambios internos o externos. Segundo, la documentación queda alineada con la estrategia general: los objetivos, metas y directrices estratégicas se reflejan en los planes y procedimientos, y el plan se adapta conforme evoluciona el negocio.

## Auditar la gestión de la continuidad

El programa de continuidad también se audita. Algunos tipos de auditoría que se aplican:

| Tipo | Qué verifica |
|---|---|
| De políticas | Que la política de continuidad exista, esté aprobada y contenga los principios que guían el programa |
| De cumplimiento | Que haya compromisos claros respecto de las normas y regulaciones aplicables, y que se cumplan |
| De procesos | Que los procesos del programa (BIA, pruebas, mantenimiento) se ejecuten como están definidos |

Usar un estándar, como ISO 22301, para establecer la gestión de la continuidad da estructura y vuelve medible el programa, aunque no entrega una receta lista para aplicar.

> Actualización al 2026: ISO 22301:2019 pide un programa de ejercicios y pruebas (cláusula 8.5), que se ejecuten con regularidad y cuando haya cambios importantes, y que sus resultados alimenten la evaluación del desempeño y la mejora del sistema.

## Para cerrar el tema

- Todo plan de continuidad se pone a prueba para asegurar que funciona con las métricas y necesidades que acordaron las partes.
- Un ensayo general da una perspectiva real sin interrumpir los servicios.
- Un simulacro muestra la realidad interrumpiendo los servicios con escenarios planteados de antemano.
- La eficiencia de un plan está en su ciclo de prueba y mejora continua.
- La eficacia del plan depende de haber analizado bien los elementos que soportan las operaciones críticas.

## Glosario

| Término | Definición |
|---|---|
| Prueba | Actividad que pone a funcionar una parte del plan para comprobar que tiene la información, los recursos y los procedimientos adecuados |
| Ejercicio de mesa | Discusión guiada de un escenario sin tocar sistemas ni detener la operación |
| Ensayo general | Ejercicio completo que reproduce un incidente con todos los equipos, sin interrumpir los servicios reales |
| Simulacro | Activación programada del plan, con o sin interrupción, para comprobar su eficacia o detectar mejoras |
| Prueba paralela | Prueba en la que el sitio alterno procesa al mismo tiempo que el principal |
| Facilitador | Persona que conduce la prueba e introduce los eventos del escenario |
| Observador | Persona que registra lo que ocurre durante la prueba sin participar en la respuesta |
| Reunión posterior | Sesión inmediata a la prueba para recoger lo que salió bien, lo que salió mal y las mejoras |
| Informe de prueba | Documento con alcance, método, resultados, recomendaciones y responsables de cada acción |
| Evaluación de daños | Proceso que determina qué funciones se afectaron y si su restauración excede el MTD |
| Control de cambios | Proceso que registra, aprueba y comunica cada modificación del plan |
| Mantenimiento del plan | Revisión y actualización periódica del plan para que siga alineado con la organización |

## Referencias

- Databarracks. (2016). *How to run a tabletop DR test* [Video]. YouTube. https://youtu.be/ev6H8u-0Z6I
- Diligent. (2021, 3 de marzo). *Business continuity plan maintenance: How to review, test and update your BCP*. https://www.diligent.com/resources/blog/bcp-maintenance
- International Organization for Standardization. (2019). *ISO 22301:2019 Security and resilience. Business continuity management systems. Requirements*. ISO. https://www.iso.org/standard/75106.html
- SBS CyberSecurity. (2025). *Mastering business continuity plan testing: 4 essential steps*. https://sbscyber.com/blog/four-steps-to-better-business-continuity-plan-testing
- Swanson, M., Bowen, P., Phillips, A. W., Gallup, D., y Lynes, D. (2010). *Contingency planning guide for federal information systems* (NIST Special Publication 800-34 Rev. 1). National Institute of Standards and Technology. https://csrc.nist.gov/pubs/sp/800/34/r1/upd1/final

---

*Rogelio Arriaga Gonzalez*
