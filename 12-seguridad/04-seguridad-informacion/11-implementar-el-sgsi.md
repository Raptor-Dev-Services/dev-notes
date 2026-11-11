# 11 - Desarrollo e implementación del sistema de gestión de seguridad de la información

> Unidad 4, tema 2 de la materia *Seguridad de la información*. La declaración de aplicabilidad, el plan de tratamiento, la documentación obligatoria, los controles del Anexo A y los roles de seguridad. Revisado a noviembre de 2026.

## Por qué importa

Con el proyecto aprobado y el riesgo evaluado (nota [10](10-activar-el-sgsi.md)), toca implantar. Aquí es donde un SGSI se gana o se pierde: los controles tienen que salir del análisis de riesgos y no de una lista copiada; cada decisión tiene que quedar documentada para que alguien la pueda auditar; y cada persona tiene que saber qué le toca. La implantación se hace **desde una perspectiva documentada**, y el documento que la ordena es la declaración de aplicabilidad.

## La declaración de aplicabilidad (SoA)

La **declaración de aplicabilidad** (*Statement of Applicability*, SoA) es un documento central del SGSI: enumera los objetivos de control y los controles de seguridad de la organización, dice cuáles se aplican y cuáles no, y justifica cada decisión. No se escribe al principio: se deriva de la evaluación de riesgos, una vez que:

- se eligieron los tratamientos de riesgo;
- se identificaron todos los requisitos legales y regulatorios aplicables;
- se entienden por completo las obligaciones contractuales;
- se revisaron las necesidades y los requisitos propios del negocio.

Un control entra a la SoA por cualquiera de esas cuatro razones: un riesgo que tratar, una ley, un contrato o una necesidad del negocio. Si un control del Anexo A se excluye, la exclusión también se justifica.

Una SoA útil tiene, por cada control, al menos estas columnas:

| Columna | Ejemplo |
|---|---|
| Control del Anexo A | 8.13 Respaldo de la información |
| ¿Aplica? | Sí |
| Razón de inclusión | Riesgo R-07 (pérdida de la base de clientes); obligación contractual con el cliente principal |
| Estado de implantación | Implantado; prueba de restauración trimestral |
| Evidencia o documento | Procedimiento PR-RES-01, bitácora de restauraciones |
| Justificación de exclusión | (solo si no aplica) |

Con la SoA en la mano, los auditores pueden armar su estrategia de revisión, y los integrantes del sistema saben hacia qué elementos dirigir los recursos de la mejora continua. Las hojas de trabajo públicas del ISO27k Toolkit, que combinan la SoA con un análisis de brechas por control, son un buen punto de partida.

## El plan de tratamiento de riesgos

La metodología de riesgos determina las características de los objetivos de control. Después se aplican controles a cada riesgo hasta que lo que quede sea un **riesgo residual** aceptable y congruente con la SoA. Implantar el plan de tratamiento implica:

- lograr los objetivos de control identificados, considerando el financiamiento y la asignación de roles y responsabilidades;
- implantar los controles que se eligieron al establecer el SGSI;
- definir **cómo se mide la eficacia** de cada control, para que los gerentes y el personal sepan si se cumplen los objetivos planeados;
- poner en marcha programas de **formación y sensibilización** en seguridad.

Tratar los riesgos con base en los objetivos de control de ISO/IEC 27001 es lo que hace que el sistema opere como fue diseñado. Hay que poder demostrar la cadena completa: de cada control al riesgo que trata, de ese riesgo a la evaluación, y de la evaluación a la política y a los objetivos del SGSI. Esa trazabilidad es lo primero que revisa un auditor.

## La documentación del SGSI

La documentación del sistema debe incluir:

1. las declaraciones documentadas de la política y de los objetivos del SGSI;
2. el alcance del SGSI;
3. los procedimientos y otros controles que lo apoyan;
4. una descripción de la metodología de evaluación de riesgos;
5. el informe de evaluación de riesgos y el plan de tratamiento de riesgos;
6. los procedimientos para planear, operar y controlar con eficacia los procesos de seguridad, y para describir y medir su eficacia;
7. los registros que la norma pide de forma específica;
8. la declaración de aplicabilidad.

> Actualización al 2026: ISO/IEC 27001:2022 habla de **información documentada**. Como mínimo exige conservar: el alcance (cláusula 4.3), la política (5.2), los procesos de evaluación y de tratamiento de riesgos (6.1.2 y 6.1.3), la declaración de aplicabilidad (6.1.3), los objetivos de seguridad (6.2), la evidencia de competencia del personal (7.2), la información para planear y controlar la operación (8.1), los resultados de la evaluación y del tratamiento de riesgos (8.2 y 8.3), los resultados del seguimiento y la medición (9.1), el programa y los resultados de las auditorías internas (9.2), los resultados de la revisión por la dirección (9.3) y las no conformidades con sus acciones correctivas (10.2).

## Los controles del Anexo A

La edición 2013 de la norma organizaba los controles del Anexo A en **14 dominios**, numerados del A.5 al A.18. Cada dominio agrupa controles que deben integrarse al sistema:

| Dominio (ed. 2013) | Qué cubre |
|---|---|
| A.5 Políticas de seguridad | Dirección de la dirección en materia de seguridad |
| A.6 Organización de la seguridad | Roles, segregación de funciones, dispositivos móviles y teletrabajo |
| A.7 Seguridad de los recursos humanos | Antes, durante y al terminar el empleo |
| A.8 Gestión de activos | Inventario, clasificación y manejo de medios |
| A.9 Control de acceso | Requisitos de negocio, gestión de usuarios, acceso a sistemas y aplicaciones |
| A.10 Criptografía | Uso de controles criptográficos y gestión de llaves |
| A.11 Seguridad física y del entorno | Áreas seguras y equipo (ver la nota [08](08-seguridad-fisica.md)) |
| A.12 Seguridad de las operaciones | Procedimientos, malware, respaldos, bitácoras, vulnerabilidades técnicas |
| A.13 Seguridad de las comunicaciones | Redes y transferencia de información |
| A.14 Adquisición, desarrollo y mantenimiento de sistemas | Requisitos de seguridad y desarrollo seguro |
| A.15 Relaciones con proveedores | Seguridad en la cadena de suministro |
| A.16 Gestión de incidentes de seguridad | Reporte, respuesta y aprendizaje |
| A.17 Seguridad en la continuidad del negocio | Continuidad de la seguridad y redundancia (ver la nota [13](13-continuidad-y-resiliencia.md)) |
| A.18 Cumplimiento | Requisitos legales, contractuales y revisiones de seguridad |

> Actualización al 2026: el Anexo A de ISO/IEC 27001:2022 tiene **93 controles** en cuatro temas: organizacionales (5.1 a 5.37), de personas (6.1 a 6.8), físicos (7.1 a 7.14) y tecnológicos (8.1 a 8.34). Los catorce dominios desaparecieron, pero su contenido se conserva redistribuido; por ejemplo, la continuidad de A.17 quedó en los controles 5.29 y 5.30 (y su redundancia en el 8.14), y la criptografía de A.10 en el 8.24. Cada control trae ahora atributos (tipo de control, propiedades de seguridad, conceptos del marco del NIST, capacidades operativas y dominios) para filtrarlos por distintas vistas.

## Los roles de la seguridad de la información

Uno de los puntos clave al establecer un control es declarar quién hace qué, de forma consistente con la operación real. Hacerse las preguntas correctas (¿qué se protege?, ¿quién responde?, ¿quién opera?, ¿quién revisa?) aclara lo que se desea proteger. Un reparto típico de responsabilidades:

**Comité de seguridad de la información**
- Establece la estrategia de seguridad.
- Aprueba las políticas nuevas y sus cambios.
- Realiza otras actividades de alto nivel en la materia.
- Administra el programa de clasificación de activos, incluida la designación de propietarios.
- Mantiene la política y los estándares de seguridad.

**Área de seguridad de la información**
- Revisa periódicamente el estado general de la seguridad.
- Revisa y da seguimiento a los incidentes.
- Revisa y aprueba los proyectos de seguridad.
- Verifica que cada activo tenga un propietario asignado formalmente.
- Identifica los objetivos y estándares de seguridad, y define procesos y metodologías.
- Vigila que se cumplan la política y los procedimientos en los activos bajo custodia.
- Apoya la evaluación de seguridad de las iniciativas del área de servicios.

**Propietario del activo de información**
- Asigna el nivel de clasificación y lo revisa periódicamente contra los objetivos de los servicios.
- Asegura que los controles aplicados sean consistentes con la clasificación.
- Fija los criterios y niveles de acceso y los requisitos de respaldo de su información.
- Revisa periódicamente quién tiene acceso a sus sistemas.
- Toma acciones cuando hay violaciones de seguridad.
- Verifica la integridad y coherencia de la información a su cargo.
- Define las políticas y lineamientos de seguridad de sus activos y documenta sus procesos.
- Designa a quien custodia la información.

**Custodio o administrador de accesos (por ejemplo, de bases de datos)**
- Administra el acceso a los archivos físicos de información: medios magnéticos, ópticos e impresos.
- Implanta los controles definidos para los sistemas, incluidas las actualizaciones de seguridad.
- Implanta y gestiona la autenticación, la autorización y las bitácoras de auditoría de los activos que custodia.
- Apoya la identificación, valoración e investigación de brechas e incidentes.
- Administra los procedimientos de respaldo, recuperación y continuidad de esos activos.
- Administra los accesos a las aplicaciones.

**Usuario**
- Mantiene la confidencialidad de sus contraseñas.
- Reporta posibles violaciones de seguridad.
- Se ajusta a las políticas, lineamientos, procesos y manuales.
- Usa los activos solo para los fines del servicio y de la operación.
- Captura información correcta en los sistemas.
- Asiste a las campañas de capacitación en seguridad y en datos personales.

## Para cerrar el tema

- Aplicar controles a un riesgo siempre deja un remanente: el riesgo residual.
- La declaración de aplicabilidad es la guía práctica y obligatoria de los controles del sistema.
- Los controles se agrupan (antes en dominios, hoy en temas) para darles un seguimiento ordenado.
- El tratamiento de los riesgos se apoya en los controles del Anexo A de ISO/IEC 27001.

## Glosario

| Término | Definición |
|---|---|
| Declaración de aplicabilidad | Documento que enumera los controles aplicados y excluidos del Anexo A, con su justificación |
| Plan de tratamiento de riesgos | Plan que dice qué controles se implantan, quién lo hace, con qué recursos y en qué plazo |
| Riesgo residual | Riesgo que permanece después del tratamiento |
| Información documentada | Información que la norma exige controlar y conservar como evidencia |
| Registro | Información documentada que prueba que una actividad se realizó |
| Anexo A | Lista de controles de referencia de ISO/IEC 27001 |
| Dominio de control | En la edición 2013, cada uno de los 14 grupos de controles del Anexo A |
| Tema de control | En la edición 2022, cada uno de los cuatro grupos: organizacional, de personas, físico y tecnológico |
| Análisis de brechas | Comparación entre el estado actual de los controles y lo que exige la norma |
| Comité de seguridad | Órgano directivo que fija la estrategia y aprueba las políticas de seguridad |
| Propietario del activo | Responsable de clasificar el activo y decidir quién accede y cómo se protege |
| Custodio | Responsable de operar los controles técnicos sobre los activos que se le confían |

## Referencias

- INCIBE. (2010). *SGSI 09: Gestión y tratamiento de los riesgos* [Video]. YouTube. https://youtu.be/9T9X0q2y6vQ
- International Organization for Standardization e International Electrotechnical Commission. (2013). *ISO/IEC 27001:2013 Information technology. Security techniques. Information security management systems. Requirements*. ISO. https://www.iso.org/standard/54534.html
- International Organization for Standardization e International Electrotechnical Commission. (2022a). *ISO/IEC 27001:2022 Information security, cybersecurity and privacy protection. Information security management systems. Requirements*. ISO. https://www.iso.org/standard/27001
- International Organization for Standardization e International Electrotechnical Commission. (2022b). *ISO/IEC 27002:2022 Information security, cybersecurity and privacy protection. Information security controls*. ISO. https://www.iso.org/standard/75652.html
- ISO27k Forum. (s. f.). *ISO27k toolkit: ISMS status, Statement of Applicability and controls status workbook*. https://www.iso27001security.com/toolkit
- Mendoza, M. Á. (2015, 1 de abril). ¿Qué es la declaración de aplicabilidad (SoA)? *WeLiveSecurity*. https://www.welivesecurity.com/la-es/2015/04/01/que-es-declaracion-de-aplicabilidad-soa/
- Muñoz, M. (2015). *Guía de implantación de un SGSI basado en la norma UNE-ISO/IEC 27001*. Universitat Oberta de Catalunya. https://openaccess.uoc.edu/

---

*Rogelio Arriaga Gonzalez*
