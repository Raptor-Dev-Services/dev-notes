# 08 - Phishing: pescar datos con un anzuelo de confianza

> Materia 2, unidad 3 (Grooming, phishing y pharming), tema 2. Nota recreada a partir del material de la materia, con cifras, variantes y autoridades actualizadas al 2026.

## Por qué importa

El phishing lleva décadas en la lista de técnicas favoritas de los ciberdelincuentes, y no porque sea sofisticado: porque funciona. En lugar de romper un sistema, el atacante convence a una persona de que le abra la puerta. Basta con que la víctima crea que habla con su banco, su jefe o su paquetería para que entregue contraseñas, datos de tarjeta o acceso a la red de su empresa.

Por eso es también la puerta de entrada de muchos otros incidentes: robo de identidad, fraude bancario, ransomware y espionaje corporativo suelen empezar con un correo, un mensaje o una llamada. Saber reconocerlo es la habilidad defensiva más rentable que existe, tanto para un usuario como para una organización.

## Un ejemplo para empezar

Llega un correo que parece de tu banco: tu cuenta fue bloqueada y, para desbloquearla, tienes que entrar a un enlace. El enlace se ve como el portal del banco y la página pide el número de tu tarjeta, la fecha de vencimiento y otros datos de verificación. Los capturas, envías, y la página te redirige al sitio real del banco, lo que refuerza la sensación de que todo fue legítimo.

Lo que ocurrió en realidad es que una página falsa, hecha para parecer auténtica, capturó tus datos y se los entregó a un delincuente. La duda sobre si el mensaje era real, la urgencia y la apariencia convincente del enlace bastaron. Eso es el phishing: **explotar la confianza y la confusión** para obtener información valiosa.

## Qué es el phishing

Es una técnica de engaño basada en la **ingeniería social** aplicada al entorno digital. El atacante se hace pasar por una entidad legítima o de confianza (un banco, una tienda, un proveedor de servicios, una dependencia de gobierno, un compañero de trabajo) para que la víctima:

- abra un correo, un mensaje instantáneo, un SMS o, en general, un **enlace malicioso**;
- llegue a un sitio falso que imita al legítimo;
- y ahí entregue datos confidenciales, descargue malware o ejecute algo que cifra su equipo como parte de un ataque de ransomware.

El objetivo principal es robar datos personales valiosos: credenciales de acceso y números de tarjetas de crédito o débito. Sus blancos son muy variados: del sector financiero y el de salud a lo más personal, como cuentas de correo, perfiles de redes sociales e incluso el acceso a redes Wi-Fi. Las consecuencias pueden ser devastadoras: compras no autorizadas, robo de fondos, usurpación de identidad y exposición de información sensible, financiera o de salud.

### Los pretextos

Los mensajes de phishing suelen presentarse como avisos de bancos, proveedores, sistemas de pago o cualquier organización, e insisten en que hay que **capturar o actualizar los datos con urgencia**. Los argumentos más comunes son:

- Un problema técnico en el sistema o una pérdida de datos.
- Un fraude detectado recientemente y la necesidad de "subir el nivel de seguridad".
- Nuevas recomendaciones o cambios en la política de seguridad de la entidad.
- La promoción de un producto nuevo.
- Premios, regalos o ingresos inesperados.
- Accesos o usos extraños de tu cuenta.
- La inminente cancelación del servicio.
- Ofertas de empleo falsas.

Algunos estafadores llegan al cinismo de usar la propia seguridad como pretexto: "Para protegerse del phishing, haga clic en el enlace e ingrese su usuario y contraseña".

### De dónde viene el nombre

*Phishing* suena como *fishing*, pescar: el atacante lanza un anzuelo y espera a que alguien pique. La *ph* inicial es un guiño a los *phreakers*, quienes en los años setenta y ochenta manipulaban las redes telefónicas con técnicas parecidas de engaño. El término está ligado desde su origen a la suplantación de identidad.

## Por qué las empresas son el blanco principal

Las organizaciones concentran información valiosa: datos de clientes, secretos comerciales, acceso a sistemas financieros. Lo obtenido puede venderse a la competencia o usarse para explotar otras brechas. Además, el phishing se usa a menudo como **primer paso de un ataque más grande**, por ejemplo una amenaza persistente avanzada (APT): el atacante compromete el equipo de un empleado, deja una puerta trasera y la usa para moverse por la red interna y extraer datos durante meses sin ser detectado. Los empleados engañados sirven para saltar el perímetro de seguridad, distribuir malware dentro de un entorno cerrado o llegar a información sensible.

Una organización que sufre un ataque de este tipo enfrenta pérdidas financieras graves, caída de su participación de mercado, daño a su reputación y pérdida de confianza de sus clientes. Según la magnitud, un incidente de phishing puede convertirse en una brecha de la que la empresa tarde años en recuperarse.

## La economía del phishing

| Aspecto | Cómo funciona |
|---|---|
| Vida corta de los sitios | Un sitio de phishing dura poco en línea, porque los filtros anti-phishing de navegadores y correos lo detectan rápido. El material del curso hablaba de unos cinco días; las mediciones publicadas son menores: el APWG midió en 2014 un promedio de 29 horas en línea (mediana de 10 horas), y un estudio de 2020 con datos de Google y PayPal encontró que una campaña típica dura unas 21 horas desde su arranque hasta su última víctima. Eso obliga a registrar sin parar sitios nuevos que imiten a los oficiales. |
| Venta de lo robado | Sacar dinero de una cuenta ajena sin dejar rastro es difícil. Por eso muchos phishers venden las credenciales y tarjetas a otros delincuentes que ya tienen esquemas probados para retirar el dinero. |
| Objetivos favoritos | Bancos, sistemas de pago electrónico y sitios de subastas o comercio en línea, porque dan acceso directo al dinero. |
| Credenciales de correo | Muy buscadas: se venden a quienes distribuyen malware o arman redes de bots, porque una cuenta legítima esquiva muchos filtros. |
| Calidad | Los mensajes y sitios falsos suelen estar muy bien hechos; algunos son copias exactas del original. |
| Direcciones engañosas | Las URL imitan las auténticas agregando palabras, cambiando una letra o usando caracteres que se ven iguales. |

El candado de HTTPS no prueba que un sitio sea legítimo: solo indica que la conexión está cifrada. Los atacantes obtienen certificados gratuitos con la misma facilidad que cualquier sitio, así que hoy es común encontrar páginas de phishing con candado.

## El atacante conoce a su víctima

En todos los tipos de phishing, el delincuente suele saber bastante de su objetivo: hábitos, preferencias, contactos cercanos, horarios, lugar de trabajo. Lo obtiene de redes sociales y de filtraciones previas, y eso le permite ser más preciso. Los envíos masivos se automatizan, y con la inteligencia artificial la tarea es aún más sencilla: un atacante puede programar campañas enormes, en horarios específicos, con textos personalizados y sin las faltas de ortografía que antes delataban el engaño.

## Variantes

| Variante | Canal | Cómo funciona |
|---|---|---|
| Phishing masivo | Correo | Envío a un grupo de personas con algo en común (una marca, una región, un perfil). Imita comunicaciones transaccionales: recibos, recordatorios de pago, tarjetas de regalo, ofertas por cobrar. |
| Spear phishing | Correo o mensaje | Dirigido a una persona o entidad concreta, con información detallada de la víctima para que el mensaje sea creíble. Tiene mucha mayor tasa de éxito. |
| Whaling | Correo | Spear phishing contra altos directivos ("peces gordos"), que tienen acceso y autoridad para aprobar pagos. |
| Smishing | SMS y, cada vez más, apps de mensajería | El atacante se hace pasar por una persona o empresa conocida y pide abrir un enlace incluido en el mensaje. |
| Vishing | Llamada de voz o VoIP | La *v* viene de *voice*. El delincuente usa datos de redes sociales para sonar creíble; hoy puede incluso clonar la voz de un familiar o directivo con IA. |
| Phishing en buscadores | Motores de búsqueda y anuncios | Una página falsa idéntica a la real se posiciona en los resultados o en anuncios pagados junto a la original. |
| Phishing por URL | Enlaces en sitios, anuncios, descargas, imágenes o videos | Enlaces falsos o acortados que redirigen a sitios fraudulentos o descargan malware. |
| Con inyección de malware | Archivo adjunto o instalador | El atacante toma control del dispositivo, modifica y extrae datos y vigila la actividad. |
| Quishing | Código QR | El enlace malicioso va dentro de un código QR impreso o en un correo, que esquiva los filtros que revisan texto. |
| Compromiso de correo empresarial (BEC) | Correo | El atacante suplanta o toma la cuenta de un directivo o proveedor y pide transferencias o cambios de cuenta bancaria. |

> Actualización al 2026: las tres últimas filas no estaban en el material original. El conjunto describe bien lo que el material anticipaba como tendencia: el smishing migró de los SMS a WhatsApp y Telegram, el vishing se mudó a la telefonía por internet y a la voz sintética, y la automatización con IA volvió los mensajes casi impecables.

## Cifras actuales

> Actualización al 2026: el material original no traía cifras. Las fuentes de referencia son el Anti-Phishing Working Group (APWG), que cuenta los sitios de phishing detectados, y el Internet Crime Complaint Center (IC3) del FBI, que recibe denuncias en Estados Unidos.

- El APWG contó unos **3.8 millones** de ataques de phishing durante 2025, y **1,069,681** solo en el segundo trimestre de 2026, un 10 % más que en el primero.
- El informe 2025 del IC3 (publicado en abril de 2026) recibió **1,008,597** denuncias con pérdidas por **20,877 millones de dólares**, un 26 % más que en 2024. El phishing y la suplantación (*phishing/spoofing*) volvió a ser la categoría con más denuncias: 191,561. Sus pérdidas directas parecen pequeñas porque el daño grande se registra en los delitos que el phishing abre, como el fraude de inversión o el compromiso de correo empresarial.

## Recomendaciones

### De la guía de la antigua Policía Federal

La Policía Federal publicó una guía sobre phishing que explica cómo los delincuentes suplantan la imagen de una empresa o dependencia para que la víctima crea que los datos los pide una fuente oficial. Señala los medios por los que llega (mensaje de texto, llamada, sitio web que simula una entidad, ventana emergente y correo) y recomienda:

- No responder **nunca** solicitudes de información personal por correo, llamada o mensaje.
- Escribir la dirección del sitio directamente en el navegador, en lugar de llegar por enlaces de otros sitios.
- Revisar periódicamente las cuentas para detectar movimientos irregulares.
- Recordar que los bancos no piden información confidencial por canales inseguros como el correo.
- Usar un filtro anti-spam.

> Actualización al 2026: la Ley de la Guardia Nacional de 2019 ordenó la extinción de la Policía Federal, y en 2020 sus divisiones, incluida la científica que atendía los delitos cibernéticos, pasaron a la Guardia Nacional (que desde la reforma constitucional de septiembre de 2024 depende de la Secretaría de la Defensa Nacional, como reitera su ley de 2025). La guía sigue siendo válida en su contenido; para denunciar hoy se acude a la policía cibernética de la Guardia Nacional o de la entidad, y a la fiscalía.

### Del UNAM-CERT

- Verificar el remitente: desconfiar de correos que no muestran su procedencia, cuyo dominio es distinto del habitual o tiene errores de escritura.
- No responder un correo sospechoso: hacerlo confirma al atacante que la cuenta existe.
- Sospechar de cualquier mensaje con tono de urgencia o curiosidad que pida una acción inmediata: abrir un enlace, descargar un archivo o mandar datos.
- Fijarse en la dirección del sitio al que remite el enlace: puede tener variaciones o llevar a otro sitio.
- No abrir adjuntos de remitentes desconocidos.
- Configurar el cliente de correo para ver la dirección real del remitente, no solo su nombre, y para no cargar contenido remoto automáticamente.
- Activar filtros de correo no deseado y fraudulento.

### Lo que agrega la práctica actual

- **Autenticación resistente al phishing.** Las llaves de seguridad y las *passkeys* (estándar FIDO2) no se pueden capturar en una página falsa, porque están ligadas al dominio legítimo. Son la defensa técnica más eficaz contra el robo de credenciales.
- **Para organizaciones:** publicar SPF, DKIM y DMARC en sus dominios para que nadie pueda enviar correo a su nombre, capacitar con simulaciones periódicas y tener un botón visible para reportar correos sospechosos.
- **Verificar por otro canal.** Ante una petición de pago, cambio de cuenta o datos, llamar al número oficial que uno ya tiene, nunca al que viene en el mensaje.

## Si fuiste víctima

1. Si diste datos financieros, llama de inmediato a tu banco para bloquear cuentas y tarjetas y cambiar lo necesario.
2. Cambia la contraseña comprometida y la de cualquier otra cuenta donde la hayas reutilizado; activa el segundo factor.
3. Si expusiste información de tu trabajo, avisa a tu jefe y al área de sistemas o seguridad.
4. Denuncia ante las autoridades: la Condusef atiende los asuntos con instituciones financieras, y la fiscalía o la policía cibernética los delitos.

> Actualización al 2026: el material original remitía también al INAI. La reforma constitucional de simplificación orgánica (DOF, 20 de diciembre de 2024) lo eliminó, y la nueva Ley Federal de Protección de Datos Personales en Posesión de los Particulares (DOF, 20 de marzo de 2025) asignó esa materia a la Secretaría Anticorrupción y Buen Gobierno, que es hoy la autoridad ante la que se presentan las quejas por mal uso de datos personales en manos de empresas.

## Para cerrar

El phishing seguirá evolucionando mientras crezca el número de personas conectadas y persista la brecha de conocimiento sobre prevención. Las soluciones técnicas y las campañas de bancos y empresas ayudan, pero no bastan si no hay educación desde la escuela. La mejor estrategia sigue siendo la misma: conocer cómo trabaja el engaño, dudar de la urgencia y verificar por un canal independiente.

## Glosario

| Término | Definición |
|---------|-----------|
| Phishing | Engaño en el que el atacante suplanta a una entidad de confianza para obtener datos o instalar malware. |
| Ingeniería social | Manipulación psicológica para lograr que una persona revele información o haga algo contra sus intereses. |
| Spear phishing | Phishing dirigido a una persona o entidad concreta, con información previa sobre ella. |
| Whaling | Spear phishing dirigido a altos directivos. |
| Smishing | Phishing por SMS o apps de mensajería. |
| Vishing | Phishing por llamada de voz o VoIP. |
| Quishing | Phishing que esconde el enlace malicioso en un código QR. |
| BEC | Compromiso de correo empresarial: fraude que suplanta a un directivo o proveedor para desviar pagos. |
| APT | Amenaza persistente avanzada: ataque dirigido y prolongado que busca permanecer oculto en una red. |
| Usurpación de identidad | Apropiarse de la identidad de alguien para obtener recursos, créditos u otros beneficios en su nombre. |
| Passkey | Credencial basada en criptografía de llave pública, ligada al dominio, que reemplaza a la contraseña. |
| DMARC | Política publicada en el DNS de un dominio que indica qué hacer con correos que dicen venir de él y no pasan las verificaciones. |

## Referencias

- Aaron, G. y Rasmussen, R. (2015). *Global phishing survey 2H2014: Trends and domain name use*. Anti-Phishing Working Group. https://docs.apwg.org/reports/APWG_Global_Phishing_Report_2H_2014.pdf
- Anti-Phishing Working Group. (2026). *Phishing activity trends report, 2nd quarter 2026*. https://docs.apwg.org/reports/apwg_trends_report_q2_2026.pdf
- Black Hat. (2017). *Ichthyology: Phishing as a science* [Video]. YouTube. https://www.youtube.com/@BlackHatOfficialYT
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley Federal de Protección de Datos Personales en Posesión de los Particulares* (DOF 20-03-2025, última reforma DOF 14-11-2025). https://www.diputados.gob.mx/LeyesBiblio/pdf/LFPDPPP.pdf
- Comisión Nacional para la Protección y Defensa de los Usuarios de Servicios Financieros. (2019, 26 de julio). ¡Cuidado! fraudes al por mayor. *Proteja su Dinero*. https://revista.condusef.gob.mx/usuario-inteligente/que-hay-de-nuevo/2019/07/cuidado-fraudes-al-por-mayor/
- Cybersecurity and Infrastructure Security Agency. (s. f.). *Phishing guidance: Stopping the attack cycle at phase one*. https://www.cisa.gov/resources-tools/resources/phishing-guidance-stopping-attack-cycle-phase-one
- Federal Bureau of Investigation. (2026). *2025 IC3 annual report*. Internet Crime Complaint Center. https://www.ic3.gov/AnnualReport/Reports/2025_IC3Report.pdf
- FIDO Alliance. (s. f.). *Passkeys*. https://fidoalliance.org/passkeys/
- Google for Education. (2017, 25 de junio). *Stay safe from phishing and scams* [Video]. YouTube. https://youtu.be/R12_y2BhKbE
- Kjchints. (2018, 6 de octubre). *La ciencia del phishing* [Video]. YouTube. https://youtu.be/t3WrAb3xI88
- Legaltech Summit. (2022, 22 de marzo). *Delitos informáticos, nuevas tecnologías y derecho penal* [Video]. YouTube. https://youtu.be/6x5ltZy1qAw
- Oest, A., Zhang, P., Wardman, B., Nunes, E., Burgis, J., Zand, A., Thomas, K., Doupé, A. y Ahn, G.-J. (2020). Sunrise to sunset: Analyzing the end-to-end life cycle and effectiveness of phishing attacks at scale. En *29th USENIX Security Symposium* (pp. 361-377). USENIX Association. https://www.usenix.org/conference/usenixsecurity20/presentation/oest-sunrise
- Policía Federal. (2018). *¿Conoces qué es el phishing?* Gobierno de México. https://web.archive.org/web/20180712075926/https://www.gob.mx/policiafederal/articulos/conoces-que-es-el-phishing?idiom=es
- UNAM-CERT. (s. f.). *¿Qué es el phishing?* Coordinación de Seguridad de la Información, UNAM. https://www.seguridad.unam.mx/que-es-el-phishing-2

---

*Rogelio Arriaga Gonzalez*
