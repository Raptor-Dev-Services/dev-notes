# 02 - Ciberseguridad, panorama general: ransomware, exploits, nube y elecciones

> Materia 2, unidad 1 (Ciberseguridad y actualidad), tema 2. Nota recreada a partir del material de la materia, que describe el panorama de 2017 y 2018. Las cifras se contrastaron con su fuente original y el panorama se actualiza al 2026.

## Por qué importa

Las noticias sobre ataques digitales y fugas de datos son cada vez más frecuentes, y muestran daños de todo tipo: equipos destruidos, información personal expuesta, servicios públicos detenidos. Los casos más graves afectan a grupos vulnerables, y en cuestión de segundos un ciberataque puede poner en crisis a un país entero.

Hablar de ciberseguridad hoy implica entender conductas nuevas que nacen de una evolución tecnológica muy rápida. Un antivirus ya no basta para proteger un equipo y su información: hace falta mirar más allá de lo convencional y conocer las técnicas que los ciberdelincuentes, protegidos detrás de un dispositivo y con alcance masivo, usan a diario. Esta nota recorre los episodios que definieron el panorama a finales de la década pasada y que siguen marcando las prácticas actuales.

## 2017: el año en que el ransomware se volvió masivo

Ese año se fijaron tendencias que siguen vigentes. Hubo ciberataques masivos, replicados en muchos países con el único fin de dañar sistemas, afectar usuarios y lucrar; desestabilizaron sectores enteros que no sabían cómo reaccionar, y tanto particulares como empresas sufrieron pérdidas que no pudieron recuperar.

### Qué es el ransomware

El **ransomware** es un tipo de malware que cifra los archivos del equipo de la víctima o bloquea su acceso, para que el delincuente pida un pago, normalmente en criptomonedas, a cambio de la llave que los libera. En los últimos años se volvió una de las mayores amenazas globales para usuarios y empresas, y muchos de sus casos llegaron a los medios de todo el mundo.

### De SamSam a WannaCry

| Momento | Qué pasó | Por qué importa |
|---|---|---|
| Marzo de 2016 | La campaña **SamSam** fue el primer ataque a gran escala que usó la red para propagar ransomware, sin que el usuario tuviera que ejecutar nada. | Demostró que el ransomware podía extenderse solo dentro de una organización. Los investigadores de Cisco anticiparon que pronto se automatizaría. |
| Abril de 2017 | El grupo Shadow Brokers filtró **EternalBlue**, un exploit para una vulnerabilidad del protocolo de archivos compartidos de Microsoft Windows. | Puso en manos de cualquiera un arma capaz de entrar a millones de equipos sin parche. |
| 12 de mayo de 2017 | Apareció **WannaCry**, un *cryptoworm*: ransomware combinado con las capacidades de un gusano, que usaba EternalBlue para propagarse por internet como reguero de pólvora. | En pocas horas afectó a más de 200,000 equipos de hospitales, fábricas y gobiernos en al menos 100 países (National Audit Office, 2017). |

La evolución fue directa: los atacantes juntaron la propagación automática de SamSam con las funciones de un gusano, y el ransomware pasó de infectar equipo por equipo a moverse solo por las redes aprovechando sus vulnerabilidades.

### Lo que ganó WannaCry (y lo que buscaba)

El informe anual de ciberseguridad de Cisco de 2018 hizo las cuentas: al momento en que se vaciaron sus billeteras, WannaCry había obtenido **poco más de 143,000 dólares** en bitcoins. Con base en el valor de los bitcoins en el momento de cada pago (unos 93,531 dólares en total), los investigadores estimaron alrededor de **312 pagos** de rescate. Como comparación, el kit de exploits Angler, cuando estaba activo, ganaba unos **100 millones de dólares al año**.

> Actualización al 2026: el material del curso decía que WannaCry había ganado "más de 143,000 millones de dólares". El informe de Cisco habla de 143,000 dólares, no de millones; la cifra se corrige aquí.

WannaCry no llevaba registro de qué archivos cifraba ni de quién pagaba, y nunca se supo cuántas víctimas recibieron una llave después de pagar. Siguió propagándose durante meses, y hubo quien siguió pagando en vano. Por ese rendimiento tan bajo, el gobierno de Estados Unidos y muchos investigadores concluyeron que el rescate era una cortina de humo y que su propósito real era **destruir datos**. En diciembre de 2017, Estados Unidos y el Reino Unido atribuyeron públicamente el ataque a Corea del Norte.

## Exploits: aprovechar las vulnerabilidades

Otro protagonista del periodo fue el uso de **exploits**. Un exploit es una herramienta o un fragmento de código que aprovecha una vulnerabilidad para tomar el control de un sistema y explotarlo. Se dividen en dos tipos:

| Tipo | Qué son | Cómo defenderse |
|---|---|---|
| Conocidos | Su existencia está documentada y ya hay medidas preventivas. Aparecen seguido en las noticias de seguridad y cada día surgen variantes nuevas. | Mantener todos los sistemas y aplicaciones actualizados; si no hay parche, aplicar técnicas de mitigación para reducir el riesgo. |
| Desconocidos o de día cero | Atacan vulnerabilidades que todavía no se han reportado al público ni al fabricante. | No hay firma que los bloquee, así que se vuelven casi indetectables; solo ayudan la defensa en profundidad y la detección por comportamiento. |

Los exploits de día cero son muy cotizados entre los delincuentes porque permiten robar información de empresas y gobiernos y, en los casos extremos, atacar infraestructura crítica. Uno de los métodos favoritos para usarlos es el correo: con una buena técnica de **ingeniería social**, un mensaje de phishing y el archivo infectado adecuado, se puede tomar el control del equipo de un usuario desprevenido que ejecuta algo que no conoce.

## La nube y el internet de las cosas

La nube es una de las herramientas más útiles para personas y empresas: guardar y procesar datos de forma remota, sin depender del almacenamiento físico propio. Pero cualquier servicio que usa mucha gente es atractivo para los ciberdelincuentes.

Conforme las aplicaciones, los datos y las identidades se mudan a la nube, los equipos de seguridad pierden el control del **perímetro** tradicional de la red. Los atacantes aprovechan que a los defensores les cuesta seguir el ritmo de la expansión de los entornos de nube y de internet de las cosas (IoT), y una de las razones es la falta de claridad sobre **quién es responsable** de proteger esos entornos.

La respuesta pasa por combinar buenas prácticas, tecnologías avanzadas como el aprendizaje automático y, a veces, métodos experimentales, según los servicios que use cada empresa y la evolución de las amenazas.

> Actualización al 2026: la confusión sobre la responsabilidad se resolvió con el **modelo de responsabilidad compartida**, que los grandes proveedores publican: el proveedor protege la infraestructura física y la plataforma; el cliente es responsable de sus datos, sus identidades, sus configuraciones y sus accesos. La mayoría de los incidentes en la nube siguen viniendo del lado del cliente: credenciales robadas y configuraciones abiertas por error.

## El sector financiero

La economía digital es uno de los temas más comentados. Muchos asocian de inmediato la palabra *bitcoin* con la moneda virtual de la que todo mundo habla, y es cierto que las criptomonedas y las mejoras a la economía digital crecieron mucho. Pero otro sector con un auge enorme de ciberataques es el **financiero**. En México basta recordar 2018: la ralentización del SPEI por los ataques a varios de sus participantes, cajeros que entregaban dinero de forma anómala y fallas repetidas en la banca en línea. Esos episodios muestran la magnitud del problema y lo rentable que es para el delincuente (el caso se estudia a detalle en la nota [10 - Carding](10-carding.md)).

## 2018: privacidad, dispositivos y elecciones

### Datos personales en redes sociales

2018 trajo avances, porque se corrigieron muchas vulnerabilidades en distintos sistemas, pero también incidentes muy conocidos de violación a la privacidad de usuarios de redes sociales. El más famoso fue el de **Cambridge Analytica**, que usó datos de decenas de millones de perfiles de Facebook obtenidos sin su consentimiento para hacer perfiles políticos.

> Actualización al 2026: el material relacionaba el escándalo con "la creación e implementación" del Reglamento General de Protección de Datos (RGPD) europeo. La cronología es otra: el RGPD se adoptó el 27 de abril de 2016 y empezó a aplicarse el 25 de mayo de 2018; el caso Cambridge Analytica se hizo público en marzo de 2018, dos meses antes. El escándalo no creó el reglamento, pero sí coincidió con su arranque y le dio enorme visibilidad.

### Wearables e IoT

Los dispositivos de IoT, como los relojes inteligentes y demás *wearables*, tienen ventajas evidentes, pero para un delincuente representan algo más de lo que el usuario imagina: un aparato así registra el estado de salud, las rutas y los lugares que frecuenta su dueño. Comprometerlo es entrar a la vida física de la víctima.

### Elecciones

En años recientes, varios gobiernos fueron acusados por la prensa internacional de involucrarse en actividades ilícitas durante procesos electorales de otros países. El caso más citado es la elección presidencial de 2016 en Estados Unidos, con acusaciones de intervención rusa.

> Actualización al 2026: el material afirmaba que se habían manipulado los sistemas de votación electrónica. Las conclusiones oficiales no van en ese sentido. La evaluación de la comunidad de inteligencia de Estados Unidos (enero de 2017) y la investigación posterior concluyeron que hubo una campaña de influencia en redes sociales y el robo y filtración de correos de un partido y de una campaña, además de sondeos a sistemas electorales estatales; no encontraron evidencia de que se alteraran votos. La lección de fondo sigue siendo la misma: la información se usó como arma.

### La información como arma

Estos casos dejan una realidad preocupante: el control sobre la infraestructura digital y los sistemas de información puede afectar el gobierno y la estabilidad de un país. Controlar las plataformas y las redes puede ser tan importante como el control físico de un territorio.

El campo de batalla ya no es solo físico. La capacidad de vigilar un Estado completo (sus sistemas, sus sitios, la información personal de cada ciudadano, sus preferencias y su comportamiento en internet) convirtió la información en un arma formidable. En las manos equivocadas, puede usarse para mucho más que vigilar, con efectos que también se sienten en el mundo físico.

## El panorama hoy

> Actualización al 2026: las tendencias que el material identificaba siguieron creciendo y cambiaron de forma.

- **Ransomware con doble extorsión.** Los grupos ya no solo cifran: primero roban los datos y amenazan con publicarlos. Funcionan como empresas que alquilan su malware a afiliados (ransomware como servicio).
- **Cadena de suministro.** En lugar de atacar a cada víctima, se compromete a un proveedor de software o servicios que usan miles de organizaciones a la vez.
- **Infraestructura crítica.** Oleoductos, hospitales y servicios de agua y energía se volvieron blancos frecuentes, y un incidente puede poner vidas en riesgo.
- **Marcos de gestión.** El Marco de Ciberseguridad del NIST se actualizó a su versión 2.0 en febrero de 2024 y agregó la función "Gobernar", que coloca la ciberseguridad como una responsabilidad de la alta dirección.

## Para cerrar

La ciberseguridad ya no puede tratarse de manera aislada. Requiere varias capas de protección, cada una contra un tipo distinto de amenaza, y un enfoque que involucre a todos los sectores de la sociedad: estrategias de prevención para la población general y una educación en ciberseguridad profunda y accesible. Entender el panorama es la condición para anticipar los riesgos que surgen todos los días, y la concientización y la prevención son eslabones tan importantes como la tecnología.

## Glosario

| Término | Definición |
|---------|-----------|
| Malware | Software diseñado para dañar, interrumpir u obtener acceso no autorizado a un sistema. |
| Ransomware | Malware que cifra o bloquea los datos de la víctima y exige un pago para liberarlos. |
| Gusano | Malware que se copia y se propaga solo de un equipo a otro aprovechando vulnerabilidades. |
| Cryptoworm | Ransomware con capacidad de gusano, como WannaCry. |
| Exploit | Código o técnica que aprovecha una vulnerabilidad para controlar un sistema. |
| Vulnerabilidad | Debilidad en un sistema o aplicación que un atacante puede aprovechar. |
| Día cero | Vulnerabilidad desconocida para el fabricante y sin parche disponible. |
| Ingeniería social | Manipulación de personas para que revelen información o ejecuten acciones que benefician al atacante. |
| Perímetro | Frontera tradicional entre la red interna de una organización e internet. |
| IoT | Internet de las cosas: objetos cotidianos conectados a internet que recopilan e intercambian datos. |
| Responsabilidad compartida | Reparto de tareas de seguridad entre el proveedor de nube y su cliente. |
| RGPD | Reglamento General de Protección de Datos de la Unión Europea. |
| Doble extorsión | Táctica de ransomware que combina el cifrado con la amenaza de publicar los datos robados. |

## Referencias

- Cadwalladr, C. y Graham-Harrison, E. (2018, 17 de marzo). Revealed: 50 million Facebook profiles harvested for Cambridge Analytica in major data breach. *The Guardian*. https://www.theguardian.com/news/2018/mar/17/cambridge-analytica-facebook-influence-us-election
- Cisco Talos. (2017, 12 de mayo). Player 3 has entered the game: Say hello to "WannaCry". *Cisco Talos Blog*. https://blog.talosintelligence.com/2017/05/wannacry.html
- Cisco. (2018). *Cisco 2018 annual cybersecurity report*. https://www.cisco.com/c/dam/m/digital/elq-cmcglobal/witb/acr2018/acr2018final.pdf
- National Audit Office. (2017, 27 de octubre). *Investigation: WannaCry cyber attack and the NHS*. https://www.nao.org.uk/reports/investigation-wannacry-cyber-attack-and-the-nhs/
- National Institute of Standards and Technology. (2024). *The NIST cybersecurity framework (CSF) 2.0* (NIST CSWP 29). https://doi.org/10.6028/NIST.CSWP.29
- Office of the Director of National Intelligence. (2017). *Assessing Russian activities and intentions in recent US elections* (ICA 2017-01D). https://www.dni.gov/files/documents/ICA_2017_01.pdf
- Parlamento Europeo y Consejo de la Unión Europea. (2016). *Reglamento (UE) 2016/679 relativo a la protección de las personas físicas en lo que respecta al tratamiento de datos personales y a la libre circulación de estos datos (Reglamento general de protección de datos)*. Diario Oficial de la Unión Europea, L 119, 1-88. https://eur-lex.europa.eu/legal-content/ES/TXT/?uri=CELEX:32016R0679
- The White House. (2017, 19 de diciembre). *Press briefing on the attribution of the WannaCry malware attack to North Korea*. https://trumpwhitehouse.archives.gov/briefings-statements/press-briefing-on-the-attribution-of-the-wannacry-malware-attack-to-north-korea-121917/

---

*Rogelio Arriaga Gonzalez*
