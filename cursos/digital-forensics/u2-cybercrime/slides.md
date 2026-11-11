<!-- _class: lead -->

# U2 - Cibercriminalidad

### Informática forense

**Rogelio Arriaga - Raptor Dev Services**

<!--
Unidad de 5 horas. Hilo conductor: antes de investigar un ciberdelito hay que reconocer la herramienta, saber
qué está pasando en el mundo y entender al usuario mexicano, que es a quien eligen los delincuentes.
Lecturas de la unidad: notas 05, 06 y 07 de 12-seguridad/03-forense.
-->

---

## La frase de la unidad

> "La tecnología es un siervo útil, pero un amo peligroso."
> Christian Lous Lange

La misma red que abarata la banca, el comercio y el trabajo en equipo entre continentes sirve para repartir código malicioso, exhibir datos y tumbar servidores.

---

## Objetivo de la unidad

Al terminar, cada quien podrá:

- Explicar por qué el ciberdelito es un fenómeno mundial.
- Usar con precisión el vocabulario del ciberdelito: técnicas, tipos de código malicioso, vectores.
- Reconocer qué facilita su expansión y qué la limita.
- Identificar las respuestas técnicas (CERT, contramedidas) y legales (convenios, códigos penales).

---

## Agenda

| Bloque | Tema | Lectura |
|---|---|---|
| 1 | Cibercriminalidad: fundamentos de investigación en México | Nota 05 |
| 2 | Cibercrimen en el mundo | Nota 06 |
| 3 | Cibercrimen en México | Nota 07 |
| 4 | Práctica: foro de discusión | |
| 5 | Cierre y tarea: mapa mental | |

---

## Dónde estamos

- En la unidad 1 vimos derecho, criminalística, método y TIC: las bases del investigador.
- Ahora miramos el fenómeno: quién ataca, con qué y a quién.
- Una computadora que falla sin explicación, datos sensibles expuestos o un equipo que participa sin saberlo en un ataque masivo contra un servidor: los tres son síntomas del mismo problema.

<!--
Pregunta de arranque: ¿quién ha recibido un SMS de "su paquete no pudo entregarse" o "su cuenta fue bloqueada"?
Casi todo el grupo levanta la mano. Ese es el punto de la unidad.
-->

---

<!-- _class: lead -->

# Parte 1
## Cibercriminalidad: fundamentos

---

## Un término que todavía se discute

- Ninguna ley mexicana define "cibercrimen" de forma cerrada.
- El 11 de septiembre de 2001 y el terrorismo digital empujaron a legislar en conjunto.
- Para trabajar: **conductas ya sancionadas por los códigos penales que usan las TIC como medio o como fin**.
- Modelo internacional: el Convenio de Budapest (2001), que se estudia en la unidad 3.

---

## Delitos viejos, herramientas nuevas

| Ya existía | Lo nuevo |
|---|---|
| Piratería de obras | Distribución masiva en línea |
| Pornografía infantil | Servidores y redes de intercambio |
| Amenazas, extorsión, fraude | Correo, SMS, redes sociales |
| Espionaje, secretos industriales | Intrusión remota |
| Falsificación de documentos | Suplantación de identidad digital |

Dos respuestas: técnica (los CERT) y legal (las leyes).

---

## Los CERT

- Nacen en noviembre de 1988, en Carnegie Mellon y con fondos de DARPA, tras el **gusano de Morris** (unas 6,000 computadoras infectadas).
- Hoy hay equipos de respuesta en los cinco continentes.
- En México: **CERT-MX**, hoy en la Guardia Nacional.

| Publican | Le sirve al investigador para |
|---|---|
| Vulnerabilidades de sistemas operativos | Saber por dónde entraron |
| Código malicioso y contramedidas | Reconocer la muestra |
| Buenas prácticas | Contrastar lo que hizo la víctima |
| Incidentes mundiales | Ubicar el caso en una campaña |

---

## La escala del problema

- El ejercicio del tema: si solo el 1 % de los usuarios tuviera intención delictiva, con 4 mil millones de usuarios serían más de 40 millones de atacantes posibles.
- Histórico (2015): 340 millones de nuevos códigos maliciosos, 529 millones de identidades expuestas, ransomware 35 % arriba.

**Actualización al 2026:** 6 mil millones de personas en línea (UIT, 2025); 1,008,597 denuncias y 20,877 millones de dólares en pérdidas ante el FBI en 2025; 4.99 millones de dólares por fuga de datos (IBM, 2026).

---

## La nube y el teléfono

**Nube (SaaS, PaaS, IaaS):** los servidores están en otro país, y la investigación depende de sus leyes. Un solo proveedor comprometido expone a muchos clientes. Causa principal de los incidentes: mala administración.

**Teléfono:**

- Vishing (llamada) y smishing (SMS) con remitentes falsificados.
- Troyanos bancarios que ocultan los SMS del banco y roban la clave de un solo uso.
- Android concentraba casi todo: 350 mil apps maliciosas en 2012.
- TOR para ocultar al operador.

---

## Trae tu propio dispositivo (BYOD)

- 62 % de las organizaciones solo pedía contraseña en los móviles (SANS, 2012).
- Más de la mitad de los adultos mexicanos presta su dispositivo a sus hijos.
- 170 mil apps móviles de alto riesgo al mes en 2014; 98 % de los ataques a Android (RSA, 2015).

<!--
Pregunta al grupo: ¿su teléfono de trabajo es el mismo que el personal? ¿Quién lo administra?
-->

---

## Troyanos financieros y mercados negros

| Familia | Rasgo |
|---|---|
| Bugat v2 | 14 % de los troyanos financieros investigados por RSA en 2012 |
| Gozi Prinimalka | Clonaba el equipo de la víctima en una máquina virtual para usar su IP |
| Carberp | Se vendía en unos 40 mil dólares |
| Man in the browser | Lee y altera la operación bancaria dentro del navegador |

Los delincuentes venden ahora en círculos cerrados: más difícil infiltrarlos.

---

## Hacktivismo

- Grupos con fines políticos, religiosos o sociales, amparados en el anonimato.
- **DDoS:** miles de equipos saturan un servidor a la misma hora.
- **Doxing:** exhibir datos de una persona pública para presionarla.
- **Hackeo y exposición:** robar y publicar datos de una organización.

En el Código Penal Federal: sabotaje (art. 140, con fin específico) y alteración de información en equipos protegidos (art. 211 bis 1). Detalle en la nota 09.

---

## Catálogo de código malicioso (1 de 2)

| Nombre | En una línea |
|---|---|
| BEC | Correo falso del director general pidiendo una transferencia |
| Bomba lógica | Espera una fecha o condición para destruir |
| Gusano | Se copia solo por la red |
| Hoax | Alerta falsa que satura buzones |
| Rootkit | Esconde al intruso dentro del sistema |
| Pharming | Desvía el dominio legítimo a un sitio falso |
| Botnet | Red de equipos zombis en renta |

---

## Catálogo de código malicioso (2 de 2)

| Nombre | En una línea |
|---|---|
| Polimórfico | Cambia su firma en cada copia |
| Stealth | Oculta tamaño, fechas y memoria |
| Retrovirus | Ataca al antivirus |
| Macrovirus | Vive en documentos de Office |
| Spyware y keylogger | Registra y envía lo que haces |
| Troyano y backdoor | Funciones ocultas y control remoto |
| Phishing, spoofing, spam, ingeniería social | Engaño al usuario o a la red |

---

<!-- _class: lead -->

# Parte 2
## Cibercrimen en el mundo

---

## El correo, puerta de entrada (2018)

- Spam: 55 % de los correos.
- Phishing a la baja: 1 de cada 3,207 correos.
- Menos enlaces y más adjuntos: los correos maliciosos con URL bajaron de 12.3 % a 7.8 %.
- Los más expuestos: empleados de organizaciones pequeñas y usuarios de Office.

Fuente: Symantec, *Internet Security Threat Report*, vol. 24 (2019).

---

## México en las tablas del correo

| Indicador (2018) | México |
|---|---|
| Correos maliciosos con URL | **29.7 %, segundo lugar mundial** (detrás de Brasil, 35.7 %) |
| Proporción de correo malicioso | 1 de cada 850 |
| Infecciones de ransomware | 3.5 %, **sexto lugar** mundial |

En México el engaño por enlace era casi cuatro veces más común que el promedio.

---

## Lo que dominó en 2018

- **Emotet:** de 4 % a 16 % de los troyanos financieros.
- **PowerShell malicioso:** 1,000 % más; la macro de Office lo invoca y él descarga la carga.
- **Detección heurística:** más de la mitad de lo bloqueado se reconoció por comportamiento.
- **Mac:** sigue siendo minoría (2.8 %), pero sus variantes casi se duplicaron entre 2016 y 2017.
- **Cryptojacking:** casi 69 millones de eventos, cuatro veces más que en 2017.
- **Ransomware:** bajó 20 % en total, pero subió 12 % en empresas.

---

## Móviles y web

- Ransomware móvil: 30 % más; 1 de cada 36 dispositivos corporativos de alto riesgo.
- Apps maliciosas, sobre todo de herramientas (39.1 %).
- 1 de cada 10 URL maliciosa; 953,800 ataques web bloqueados al día.
- **Formjacking:** 4,800 sitios comprometidos al mes robando tarjetas en la página de pago.
- Sitios más comprometidos: DNS dinámico, apuestas y hospedaje.

<!--
Pedir que identifiquen cuál de estas tendencias ya vieron en su trabajo o en su familia.
-->

---

<!-- _class: lead -->

# Parte 3
## Cibercrimen en México

---

## El internauta mexicano

- De 20.2 millones (2006) a 82.7 millones (2018): casi cuatro veces más.
- **Actualización al 2026:** 104.9 millones, 86.1 % de la población de seis años o más (INEGI, ENDUTIH 2025).
- Edad: el 44 % tenía entre 6 y 24 años, unos 35 millones de personas.
- Promedio de 5.1 horas diarias en línea.

| Riesgo por edad | Interés del delincuente |
|---|---|
| 6 a 24 años | Extorsión, reclutamiento, trata |
| 25 años o más | Fraude, robo de datos, suplantación |

---

## Hábitos que abren la puerta

- 90 % se conecta al mediodía; 67 % siente que está conectado todo el día.
- 82 % usa redes sociales y 79 % da clic en vínculos desconocidos.
- 92 % se conecta con el teléfono; wifi contratado (47 %) y plan de datos (39 %).
- Banca en línea: 58 %; compras en línea: 46 %; 64 % paga con tarjeta de crédito.
- Comercio electrónico 2017: 396 mil millones de pesos (+20.1 %).

---

## El costo medido: Norton 2013

| Indicador | México | Mundo |
|---|---|---|
| Costo total | 39 mil millones de pesos | |
| Costo por víctima | 4,381 pesos (+123 %) | |
| Víctimas por pérdida o robo del teléfono | 54 % | 27 % |
| Víctimas en computadora o en línea | 61 % | 50 % |

Y los hábitos: 70 % juega y trabaja con el mismo móvil; 37 % mezcla datos personales y de trabajo en la misma nube compartida.

---

## Caso: los ataques al SPEI (2018)

- Abril a mayo: cinco participantes atacados; octubre: un sexto.
- Blanco: los aplicativos con que cada participante se conecta, **no** el SPEI central.
- Patrón: transferencias a cuentas reales desde cuentas inexistentes y retiro en efectivo.
- Respuesta del Banco de México: plataforma alterna, alertas, oficial de seguridad, protocolo de cuatro niveles de alerta.

<!--
Subrayar para la unidad 4: la investigación de este caso dependió de bitácoras que los atacantes intentaron borrar.
El monto exacto no aparece en el informe oficial; las cifras de prensa varían.
-->

---

## Cajeros y tarjetas

1. Antes: lector falso con computadora en el hueco del cajero y un "error del sistema".
2. Hoy: lector sobrepuesto y cámara oculta sobre el teclado; la víctima no nota nada.
3. Registradores de teclas y vaciado por software (*jackpotting*).

Para usar una tarjeta ajena basta: banda + NIP, banda + firma, o número + nombre + vencimiento + código del reverso.

---

## Actualización al 2026

- **Ciberacoso:** 19.4 millones de personas, 20.4 % de los usuarios de 12 años o más (INEGI, MOCIBA 2025).
- **Instituciones:** la PGR es FGR desde 2018; la Guardia Nacional absorbió a la Policía Federal y vigila sitios de internet para prevenir delitos (Ley de la Guardia Nacional, 2025, art. 9, fr. XXXVII).
- **Ley Olimpia:** difundir contenido íntimo sin consentimiento es delito federal (CPF, art. 199 Octies, desde 2021).

---

## Práctica: foro de discusión

**¿Las tendencias mundiales del cibercrimen afectan a México, y cómo?**

1. Primera aportación: su postura con tres datos con fuente (uno mundial, uno de México, uno reciente).
2. Segunda aportación: comentar a tres compañeros, de acuerdo o no, y por qué.

Instrucciones completas en la práctica de la unidad.

---

## Tarea: mapa mental

**Los delitos informáticos que identifica la lectura de la nota 05.**

- Agrupados por categoría (fraude, sabotaje, espionaje, contenido ilícito, código malicioso).
- Cada rama con su técnica y un ejemplo del material.
- Una tabla que compare tres de ellos.

Instrucciones y rúbrica en la tarea de la unidad.

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Conclusiones

- **Tema 1.** El ciberdelito usa las TIC como medio o como fin: código malicioso, phishing, ransomware. Los CERT publican tendencias y contramedidas.
- **Tema 2.** La escala es mundial: miles de millones de usuarios, ataques crecientes al internet de las cosas y el teléfono como blanco principal; los países desarrollados son origen y destino.
- **Tema 3.** México tiene más de 100 millones de usuarios muy expuestos; la clonación en cajeros lleva décadas. Hace falta prevención y respuesta coordinada.

---

## Al terminar puedes

- Nombrar y distinguir las principales técnicas y tipos de código malicioso.
- Explicar qué publica un CERT y cómo se usa en una investigación.
- Leer un informe de amenazas y ubicar a México en él.
- Relacionar los hábitos del usuario mexicano con los delitos que más lo afectan.
- Distinguir los datos históricos de los vigentes y saber dónde actualizarlos.

---

## Lecturas y lo que sigue

- `12-seguridad/03-forense/05-cibercriminalidad-fundamentos.md`
- `12-seguridad/03-forense/06-cibercrimen-en-el-mundo.md`
- `12-seguridad/03-forense/07-cibercrimen-en-mexico.md`

**Siguiente unidad: marco normativo.** El Convenio de Budapest, el Código Penal Federal y las leyes complementarias: con qué herramientas legales se persigue lo que vimos hoy.
