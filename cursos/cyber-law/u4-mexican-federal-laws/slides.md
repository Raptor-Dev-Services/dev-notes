<!-- _class: lead -->

# U4 - Leyes federales y ciberseguridad en México

### Legislación nacional e internacional en ciberseguridad

**Rogelio Arriaga - Raptor Dev Services**

<!--
Unidad de 2 horas. Hilo conductor: un incidente real de cualquier organización mexicana siempre termina en
tres preguntas jurídicas: ¿qué derechos están en juego (Constitución)?, ¿alguien cometió un delito (CPF)?,
¿qué debía hacer quien guardaba los datos (leyes de datos personales)? La unidad responde las tres.
Advertencia para el instructor: buena parte del material clásico sobre este tema quedó viejo en 2025. Todo
lo que mencione al INAI, al IFT, a la LFTR o a la LFPDPPP de 2010 se explica como historia.
-->

---

## El punto de partida

Usamos tecnología para todo, y eso tiene dos caras:

- La conectividad depende de una infraestructura previa: las telecomunicaciones.
- Cada uso genera datos que, sin conciencia digital, pueden caer en malas manos.
- Podemos ser víctimas de un delito cometido con tecnología... o cometer uno sin saberlo.

La ciberseguridad no es solo técnica: el derecho define qué se permite, qué se castiga y quién responde.

---

## Objetivos de la unidad

Al terminar, puedes:

1. Analizar el marco constitucional relacionado con la tecnología.
2. Identificar el marco penal federal de la ciberseguridad.
3. Reflexionar sobre la protección de datos personales en México, después de los cambios de 2024 y 2025.

---

## Agenda

| Bloque | Tema | Lectura |
|---|---|---|
| 1 | Marco constitucional y legal | Nota 10 |
| 2 | Ciberseguridad en el derecho penal | Nota 11 |
| 3 | Protección de datos personales | Nota 12 |
| 4 | Cierre, práctica y tarea | |

---

<!-- _class: lead -->

# Tema 1
## Marco constitucional y legal

---

## La Constitución: la norma de la que sale todo

- Reconoce derechos, organiza a los poderes y les pone límites.
- Texto vigente: **9 títulos y 136 artículos**, más transitorios.
- Ninguna ley, reglamento o norma técnica puede contradecirla.
- Se consulta siempre en su versión vigente (Cámara de Diputados).

<!--
Pregunta al grupo: ¿cuándo fue la última reforma? Que la busquen en vivo. Al 10 de noviembre de 2026 era la
del DOF 07-10-2026. El punto: la Constitución cambia seguido, y citar una versión vieja es un error común.
-->

---

## 1917: un texto hijo de su tiempo

- Congreso Constituyente en Querétaro, desde el 1 de diciembre de 1916.
- Publicada el 5 de febrero de 1917; formalmente reformó la de 1857.
- Artículos emblemáticos: 3 (educación), 27 (tierra), 123 (trabajo), 130 (Estado e iglesias).
- Debate excluyente: solo participó el grupo vencedor.

Una Constitución se reforma porque la realidad cambia: el ciberespacio no existía en 1917.

---

## Los artículos que tocan la tecnología

| Artículo | Qué aporta |
|---|---|
| 3, fr. V | Derecho a los beneficios de la ciencia y la tecnología |
| 6 | Acceso a TIC, banda ancha e internet; telecomunicaciones como servicio público |
| 7 | Libertad de difundir ideas por cualquier medio, incluidas las TIC |
| 16 | Datos personales (ARCO) e inviolabilidad de las comunicaciones privadas |
| 28 | Rectoría del Ejecutivo sobre telecomunicaciones |
| 73, fr. XXIX-O y XXIX-S | Quién legisla sobre datos personales |

---

## Artículo 3, fracción V

> Toda persona tiene derecho a gozar de los beneficios del desarrollo de la ciencia y la innovación tecnológica.

- Gozar de la tecnología implica usarla bien.
- Aquí empieza la **cadena de la ciberseguridad**: formación digital en casa y en la escuela.

---

## Artículo 6: acceso a las TIC

> El Estado garantizará el derecho de acceso a las tecnologías de la información y comunicación, así como a los servicios de radiodifusión y telecomunicaciones, incluido el de banda ancha e Internet.

- **Actualización 2024:** la competencia efectiva ya no la establece "el Estado" en abstracto, sino el Ejecutivo Federal a través de la dependencia del ramo.
- Apartado B: telecomunicaciones como servicio público de interés general; inclusión digital universal.

<!--
Punto de debate: si el Estado da wifi gratuito en plazas, ¿qué obligaciones de seguridad tiene? Las políticas
de uso de esas redes son lineamientos de ciberseguridad para proteger al usuario y a la infraestructura.
-->

---

## Artículos 7 y 16

**Artículo 7:** la libertad de difundir ideas no se restringe por vías indirectas, incluidas las TIC.
Una medida de "seguridad" que bloquea o filtra puede volverse censura.

**Artículo 16:**

> Toda persona tiene derecho a la protección de sus datos personales, al acceso, rectificación y cancelación de los mismos, así como a manifestar su oposición...

> Las comunicaciones privadas son inviolables.

Solo un juez federal autoriza intervenir comunicaciones.

---

## Artículos 28 y 73

- **28:** el Ejecutivo Federal tiene la rectoría de las telecomunicaciones y la radiodifusión.
- **Actualización 2024 y 2025:** el IFT, órgano autónomo, se extinguió. Lo sustituye la Comisión Reguladora de Telecomunicaciones, dentro de la Agencia de Transformación Digital y Telecomunicaciones.
- **73, XXIX-O:** el Congreso legisla sobre datos en posesión de particulares (ley federal).
- **73, XXIX-S:** leyes generales sobre datos en el sector público (ley general).

---

## Las leyes federales que completan el cuadro

| Ley | Qué aporta |
|---|---|
| Telecomunicaciones y Radiodifusión (2025) | Colaboración con la justicia, conservación de registros, líneas móviles con CURP |
| Seguridad Nacional | Amenazas, incluida la infraestructura estratégica |
| Firma Electrónica Avanzada | Firma y certificados digitales |
| Código de Comercio | Comercio electrónico y equivalencia funcional |
| Instituciones de Crédito y Ley Fintech | Medios electrónicos, secreto bancario, seguridad de la información |

México **no tiene** una ley general de ciberseguridad (al 2026).

---

## Telecomunicaciones y justicia

- Atender todo mandamiento escrito, fundado y motivado (art. 182 LMTR).
- Localización geográfica en tiempo real de los equipos.
- Registro de comunicaciones: 12 meses de consulta rápida más 12 de almacenamiento.
- Entrega en 24 horas (48 si ya está en almacenamiento); área disponible todo el año.
- Desde 2025: líneas móviles asociadas a una identificación con CURP (art. 103).

<!--
Dilema para discutir: estas bases ayudan a investigar extorsiones, pero son un blanco enorme. ¿Qué controles
exigirías tú para que existan? Engancha con el tema 3.
-->

---

## Caso: ¿los ciberataques son amenaza a la seguridad nacional?

Iniciativa publicada en la Gaceta Parlamentaria el 8 de enero de 2020:

- **Diagnóstico:** ataque al SPEI (2018), ransomware a Pemex (2019), México segundo en ransomware en la región.
- **Propuesta:** dos fracciones nuevas en el artículo 5 y una definición de ciberespacio en el 6.
- **Resultado:** no prosperó; el artículo 5 vigente sigue sin mencionar el ciberespacio.

Es el insumo de la práctica de esta unidad.

---

## Lo que avanzó por otra vía

- Iniciativas de ley de ciberseguridad: 2023 (no avanzó) y enero de 2026 (en comisiones).
- Diciembre de 2025: Plan Nacional de Ciberseguridad 2025-2030 de la Agencia de Transformación Digital y Telecomunicaciones.
- DOF 17-12-2025: Política General de Ciberseguridad para la Administración Pública Federal, obligatoria para las dependencias.

Política pública sí; ley, todavía no.

---

<!-- _class: lead -->

# Tema 2
## Ciberseguridad en el derecho penal

---

## Qué es un ciberdelito

La tecnología interviene de tres maneras:

| Papel | Ejemplo |
|---|---|
| Herramienta | Correos fraudulentos masivos |
| Objetivo | Cifrar o borrar los datos de un sistema |
| Medio | Extorsionar por una red social |

Casi ninguna conducta es nueva: internet las extiende y agrega algunas, como el acceso no autorizado.

---

## Lo que de verdad cambia: no es local

- Autor, víctima, servidores y dinero pueden estar en países distintos.
- ¿Dónde se cometió el delito? Como en una llamada: en algún punto entre los dos extremos.
- El delincuente deja huellas, pero cruzan fronteras.
- Seguirlas exige tratados ratificados y cooperación rápida.

---

## La identidad virtual

- Somos números e identificadores en bases de datos de gobiernos y empresas.
- La mayoría de los ciberdelitos atacan esa información.
- Robar la identidad digital permite abrir créditos, vaciar cuentas o suplantar.

El delito cibernético muestra cuánto dependemos de las redes y qué tan frágil es la identidad.

---

## El Código Penal Federal: mapa rápido

| Conducta | Artículos |
|---|---|
| Pornografía de menores | 202 y 202 Bis |
| Contacto con menores con fines sexuales (grooming) | 199 Septies (desde 2018) |
| Violación a la intimidad sexual (Ley Olimpia) | 199 Octies a Decies (desde 2021) |
| Revelación de comunicaciones intervenidas | 211 Bis |
| Acceso ilícito a sistemas | 211 bis 1 a 211 bis 7 |
| Elusión de protecciones de software | 424 bis, fr. II |

---

## Acceso ilícito: la lógica de las penas

| Sistema | Modificar o destruir (sin autorización) | Conocer o copiar (sin autorización) |
|---|---|---|
| Particular protegido | 6 meses a 2 años | 3 meses a 1 año |
| Del Estado | 1 a 4 años | 6 meses a 2 años |
| Seguridad pública | 4 a 10 años | 4 a 10 años |
| Sistema financiero | 6 meses a 4 años | 3 meses a 2 años |

Pesa más quien abusa de un acceso autorizado; todo sube hasta una mitad si la información se usa para provecho.

---

## Tres lecciones para el profesional

1. **La autorización lo es todo:** una prueba de penetración sin carta firmada es la conducta del 211 bis 1.
2. **"Protegido por algún mecanismo de seguridad"** es requisito: sin control de acceso, hay laguna.
3. **El de adentro pesa más:** el abuso de un acceso legítimo se castiga con más dureza.

<!--
Dato curioso para comentar: los artículos 211 bis 2 y 3 siguen diciendo "salario mínimo general vigente en
el Distrito Federal". Desde 2016 ni existe el Distrito Federal ni el salario mínimo es unidad de cuenta: se
lee como UMA. Se cita tal cual porque así está el texto vigente.
-->

---

## Conductas frecuentes, encuadre corregido

| Conducta | Encuadre vigente |
|---|---|
| Ingeniería social | Fraude (386), revelación de secretos (210), acceso ilícito (211 bis) |
| Phishing | Fraude (386), falsificación (244 y 245), tratamiento engañoso de datos (art. 63 LFPDPPP) |
| Sexting difundido | Adultos: 199 Octies. Menores: 202 y 199 Septies |
| Sextorsión | Amenazas (282) y extorsión en la ley general de 2025 |

**Actualización al 2026:** el artículo 390 del CPF (extorsión) se derogó (DOF 28-11-2025); la extorsión vive ahora en una ley general, con agravantes por usar datos privados o medios electrónicos.

---

## Estados, Federación y el mundo

- Cada entidad tiene su código penal; el mapa es desigual.
- Hay que armonizar: la ley general de extorsión de 2025 es un ejemplo.
- **Convenio de Budapest (2001):** México sigue sin ser parte; está invitado a adherirse.
- **Convención de la ONU contra la Ciberdelincuencia (2024):** no está en vigor y México no aparece entre sus firmantes.

---

<!-- _class: lead -->

# Tema 3
## Ciberseguridad y protección de datos personales

---

## Por qué proteger datos

- Son de los activos más valiosos de una organización.
- Tres frentes: **disponibilidad, integridad y confidencialidad**.
- El fin último no son los datos: son los derechos de las personas.
- Los fragmentos que dejamos en línea se unen en perfiles: publicidad dirigida y, en malas manos, riesgo político.

Antes de tratar datos: ¿qué datos?, ¿para qué?, ¿con qué fundamento?, ¿con qué medidas?

---

## La autoridad cambió tres veces

| Periodo | Autoridad |
|---|---|
| 2010 a 2015 | IFAI |
| 2015 a marzo de 2025 | INAI, órgano autónomo |
| Desde el 21 de marzo de 2025 | Secretaría Anticorrupción y Buen Gobierno |

**Actualización al 2026:** nuevas leyes de datos (DOF 20-03-2025) para particulares y para el sector público. Las de 2010 y 2017 quedaron abrogadas.

<!--
Subraya la pérdida de independencia: el material clásico decía que una buena ley necesita un regulador
independiente con mandato y recursos. Que el grupo opine si el nuevo esquema lo cumple, con el Protocolo
Adicional al Convenio 108 en la mano.
-->

---

## La ley de particulares (2025), en lo que importa a seguridad

- **Principios (art. 5):** licitud, finalidad, lealtad, consentimiento, calidad, proporcionalidad, información y responsabilidad.
- **Aviso de privacidad (art. 15).**
- **Medidas de seguridad administrativas, técnicas y físicas (art. 18)**, proporcionales al riesgo y a la sensibilidad.
- **Aviso inmediato de vulneraciones (art. 19)** que afecten de forma significativa a los titulares.
- **Confidencialidad (art. 20)**, aun después de terminar la relación.

---

## Derechos ARCO

| Derecho | Qué permite |
|---|---|
| Acceso | Saber qué datos se tienen y cómo se tratan |
| Rectificación | Corregir datos inexactos o desactualizados |
| Cancelación | Pedir que se eliminen, con excepciones |
| Oposición | Negarse a un tratamiento |

La portabilidad existe solo en la ley del sector público.

---

## Sanciones y delitos

- Multas de hasta 320,000 UMA, más multa adicional por reincidencia y hasta el doble con datos sensibles (art. 59).
- **Delitos (antes arts. 67 a 69, hoy 62 a 64):**
  - Vulneración de seguridad con ánimo de lucro por quien está autorizado: 3 meses a 3 años.
  - Tratamiento engañoso para lucrar: 6 meses a 5 años.
  - Datos sensibles: penas al doble.

---

## Sector público y Convenio 108

- Ley general de 2025: aplica a autoridades de los tres órdenes; autoridades garantes de control interno.
- Seguridad y justicia: medidas de **nivel alto** (art. 76).
- **Convenio 108** y su Protocolo Adicional: vigentes para México desde el 1 de octubre de 2018.

---

## Consejos que no dependen de ninguna ley

- Antimalware actualizado en todos los dispositivos.
- Contraseñas largas y únicas, en un gestor.
- Nada de datos ni sesiones desde redes inseguras.
- En formularios, solo lo indispensable.
- Si alguien pide datos, preguntar para qué; si no convence, no entregarlos.

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Conclusiones de la unidad

- La Constitución es la norma suprema; la tecnología aparece desde sus primeros artículos y es el punto de partida para leer la jerarquía de las leyes.
- Hace falta un marco penal que identifique y castigue lo nocivo en lo digital, y que se interprete al ritmo de la tecnología. Federación y estados deben armonizarse.
- Nadie está por encima de la ley y desconocerla no exime: el profesional necesita saber dónde termina su autorización.
- Un control legal sólido respalda a los controles técnicos. Cada usuario debe conocer y saber ejercer sus derechos ARCO.

---

## Práctica y tarea

**Práctica:** análisis de la iniciativa de 2020 que proponía tratar los ataques cibernéticos como amenazas a la seguridad nacional. Se complementa a tres compañeros con información, no con opinión.

**Tarea:** ensayo de una a tres cuartillas, pensando como legislador: por qué hace falta un control legal de la ciberseguridad y qué incorporarías a las leyes.

Instrucciones completas en los documentos de la unidad.

---

## Lecturas

- Nota 10: marco constitucional y legal.
- Nota 11: ciberseguridad en el derecho penal mexicano.
- Nota 12: ciberseguridad y protección de datos personales.
- Textos vigentes en la página de la Cámara de Diputados: Constitución, Código Penal Federal y las dos leyes de datos de 2025.

---

## Siguiente unidad: regulación internacional

- La Unión Europea: Reglamento de Ciberseguridad, protección de datos y certificación.
- Estados Unidos: un mosaico de leyes sectoriales y estrategias.
- Latinoamérica: Argentina, Colombia y Chile.

Pregunta para llevar: ¿qué tendría que hacer una empresa mexicana que trata datos de clientes europeos?
