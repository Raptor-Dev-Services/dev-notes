# 12 - Cryptojacking: minar con la electricidad de otros

> Materia 2, unidad 4 (Carding, juice jacking y cryptojacking), tema 3. Nota recreada a partir del material de la materia, con la evolución de la amenaza actualizada al 2026.

## Por qué importa

La economía digital adoptó las criptomonedas para muchas transacciones, y su mercado volátil promete ganancias grandes. Donde hay ganancias fáciles hay delincuentes buscando atajos. El cryptojacking es uno de los más astutos: en lugar de robar dinero, roba **capacidad de cómputo y electricidad** para fabricar criptomonedas a nombre del atacante.

A primera vista parece un delito menor, porque la víctima no pierde archivos ni ve una pantalla de rescate. Pero su equipo se vuelve lento, se calienta, gasta más energía y se desgasta antes de tiempo; en una empresa, el costo se multiplica por cada servidor afectado y la factura de la nube puede dispararse. Y el malware que mina en silencio también puede abrir la puerta a otros ataques. Su característica más peligrosa es justamente esa: está diseñado para que nadie lo note.

## Qué es el cryptojacking

También llamado **minería maliciosa de criptomonedas**, es una amenaza que se esconde en una computadora, un teléfono o un servidor y usa sus recursos para "extraer" criptomonedas, sin el conocimiento ni el consentimiento del dueño. Puede apoderarse de navegadores web y comprometer todo tipo de dispositivos: equipos de escritorio y portátiles, teléfonos inteligentes e incluso servidores de red.

Como casi todos los ataques contra el público, su motivo es económico. A diferencia de la mayoría, está pensado para pasar completamente desapercibido. Para entender por qué existe, hay que entender cómo se crean las criptomonedas.

## Criptomonedas en cinco ideas

1. **Qué son.** Formas de dinero digital que existen únicamente en línea, sin forma física. Surgieron como alternativa al dinero tradicional y ganaron popularidad por su diseño, su potencial de crecimiento y la promesa de anonimato. El término une *criptografía* y *moneda*: dinero electrónico basado en principios de cifrado matemático.
2. **Bitcoin abrió el camino.** Fue lanzada en **2009** y su éxito inspiró miles de criptomonedas que funcionan de forma parecida. En menos de una década, personas de todo el mundo las usaban para comprar, vender e invertir.
3. **Sin banco central.** Las unidades de una criptomoneda son entradas en una base de datos, pero esa base no la controla nadie en particular: está replicada en miles de computadoras de la red. Ningún gobierno ni banco las respalda ni las regula de forma central. Funciona como un libro contable digital y público que usa criptografía para controlar la creación de monedas nuevas y verificar cada transferencia.
4. **Billeteras y llaves.** Los dueños guardan sus monedas en billeteras virtuales protegidas con llaves privadas. Transferir fondos de una billetera a otra exige registrar la operación en el libro público.
5. **La minería.** Como no hay una autoridad que decida cuánta moneda emitir, la mayoría entra en circulación por **minería**: en Bitcoin, aproximadamente cada diez minutos se agrupan las transacciones recientes en un bloque que solo se confirma cuando alguien resuelve un acertijo matemático muy costoso. Muchos mineros compiten a la vez; el primero que encuentra la solución gana una recompensa en monedas nuevas. Ese incentivo paga el tiempo y la potencia de cómputo que mantienen la red.

Un matiz de precisión: las criptomonedas como Bitcoin no son anónimas sino **seudónimas**. Las direcciones no llevan el nombre del dueño, pero todas las operaciones quedan públicas y pueden rastrearse.

### La carrera armamentista

Al principio cualquiera podía minar con su computadora. Conforme el acertijo se volvió más difícil, ni las PC más potentes alcanzaban a cubrir sus costos: los mineros agregaron tarjetas de video, después construyeron granjas enteras con hardware especializado que mina las 24 horas. Minar dejó de ser rentable para quien tiene recursos limitados, porque la electricidad y el equipo son las grandes inversiones de esta actividad.

Ahí nace el cryptojacking: en lugar de pagar por una computadora de minería, el delincuente **infecta computadoras normales y las suma** como si fueran una sola. Mientras más dispositivos trabajen para él, más rápido extrae monedas, y quien paga la luz y el desgaste es la víctima.

> Actualización al 2026: dos cambios afectan este panorama. En septiembre de 2022, Ethereum abandonó la minería por prueba de trabajo (el evento conocido como *The Merge*), así que ya no se mina con tarjetas de video. Y los criptojackers prefieren desde hace años **Monero**, una moneda diseñada para minarse bien con procesadores comunes y con mayor privacidad en sus transacciones, lo que la vuelve ideal para repartir el trabajo entre miles de equipos ajenos.

## Cómo llega a un dispositivo

| Vector | Cómo funciona |
|---|---|
| Correo de phishing | La víctima recibe un correo aparentemente inofensivo con un enlace o un adjunto. Al abrirlo, un código descarga el programa de minería, que queda corriendo en segundo plano. |
| Minería en el navegador | El atacante inyecta un script de minería en JavaScript en un sitio web o en un anuncio que se muestra en muchos sitios. Cuando la víctima visita la página, el script corre solo; no se instala nada en la computadora y se detiene al cerrar la pestaña. |
| Servidores y nube | El atacante aprovecha vulnerabilidades sin parchar, contraseñas débiles o credenciales de nube filtradas para levantar mineros en servidores, contenedores o máquinas virtuales de la víctima. |
| Aplicaciones y extensiones | Programas pirata, extensiones de navegador o apps que traen un minero escondido. |
| Dispositivos IoT | Cámaras, enrutadores y otros equipos conectados con poca protección, que se suman a una red de minería. |

En todos los casos, el código resuelve los cálculos y envía los resultados al servidor del atacante mientras la víctima no se da cuenta. Como JavaScript corre en casi todos los sitios web, la variante del navegador no necesita instalar nada.

## El caso Coinhive

El cryptojacking basado en la web apareció por primera vez a gran escala en **septiembre de 2017**, con el lanzamiento de **Coinhive**: un minero en JavaScript presentado como una forma para que los dueños de sitios monetizaran su contenido sin anuncios molestos. Funcionaba en todos los navegadores principales y era fácil de integrar. Sus creadores se quedaban con el **30 %** de lo minado y, mediante llaves criptográficas, asignaban el **70 %** restante a la cuenta del sitio.

La idea se presentó como interesante, pero pronto fue criticada: los delincuentes empezaron a inyectar el minero en sitios hackeados sin permiso de sus dueños. Existía una versión con consentimiento, llamada AuthedMine, que solo minaba después de que el visitante lo aceptaba, pero fue la versión sin consentimiento la que se esparció. Durante la primera mitad de 2018, Coinhive se convirtió en la amenaza que más detectaban los antivirus.

> Actualización al 2026: Coinhive cerró el 8 de marzo de 2019, tras la caída del valor de Monero y un cambio en su algoritmo. La minería en navegadores bajó mucho desde entonces, pero el cryptojacking no desapareció: se mudó a los servidores y a la nube, donde un solo acceso robado da mucho más poder de cómputo.

### Las cifras

- Symantec reportó que las detecciones de mineros de criptomonedas en equipos crecieron **8,500 %** durante 2017, el año del auge de Coinhive.
- Después del cierre de Coinhive, la amenaza volvió a crecer: SonicWall contabilizó **1,060 millones** de intentos de cryptojacking en 2023, un aumento de **659 %** respecto de 2022.

Los informes de la industria alternan: en algunos años el cryptojacking cede terreno ante el ransomware y los troyanos, y en otros regresa con fuerza, siguiendo de cerca el precio de las criptomonedas.

## Cómo darse cuenta

La mayoría del software de cryptojacking está hecho para esconderse, pero eso no significa que no deje huella. El robo de recursos:

- **Ralentiza** los demás procesos: las aplicaciones tardan más y el equipo responde mal.
- **Calienta** el equipo y hace que el ventilador trabaje más de lo normal; en teléfonos, la batería se agota rápido.
- **Sube la factura de electricidad** y, en la nube, la de cómputo.
- **Acorta la vida útil** del dispositivo por el desgaste.

En una empresa los efectos se suman: baja la eficiencia operativa, sube el gasto de energía, pueden interrumpirse servicios y quedan expuestos datos críticos, porque el malware que mina puede servir de entrada a otros ataques. Las instituciones con muchos servidores interconectados, como universidades, son especialmente vulnerables.

## Cómo protegerse

1. **Mantener actualizados** el sistema operativo y todas las aplicaciones, para que las vulnerabilidades conocidas estén corregidas.
2. **Usar software de seguridad confiable** que detecte malware y scripts maliciosos.
3. **Instalar bloqueadores** de anuncios y de scripts en el navegador; las extensiones que bloquean mineros y la precaución con los scripts que se ejecutan solos reducen mucho el riesgo.
4. **Vigilar el uso del procesador** y otras métricas del sistema; un uso alto sin explicación es la señal principal.
5. **Capacitar** a empleados y usuarios para reconocer el phishing y no abrir enlaces ni adjuntos de correos no solicitados.
6. **Aplicar políticas de seguridad y gestión de parches** para reducir las vulnerabilidades que se pueden explotar.
7. **En la nube:** proteger las credenciales con segundo factor, no dejarlas en el código, fijar límites de recursos y alertas de facturación, y revisar periódicamente qué instancias y contenedores están corriendo.
8. **En la red:** bloquear las conexiones a grupos de minería conocidos desde los equipos corporativos.

## El marco legal

Usar recursos de cómputo ajenos sin autorización suele considerarse ilegal, porque implica acceder sin permiso a sistemas informáticos. En México encaja en el acceso ilícito a sistemas y equipos de informática del Código Penal Federal, y en otros países en sus equivalentes. Para una empresa, además, un minero en sus servidores es un incidente de seguridad que obliga a investigar qué más hizo el atacante con ese acceso.

## Para cerrar

El cryptojacking surgió con la revolución de las criptomonedas y se alimenta de su volatilidad. Su rasgo distintivo es el sigilo: opera sin alertar al usuario. Por eso la defensa depende de revisar con regularidad el rendimiento de los equipos, bloquear scripts y autoejecutables, mantener todo actualizado y vigilar el comportamiento de la infraestructura. Las estrategias proactivas son las que lo atrapan antes de que el daño crezca.

## Glosario

| Término | Definición |
|---------|-----------|
| Cryptojacking | Uso no autorizado de los recursos de un dispositivo para minar criptomonedas en beneficio de un tercero. |
| Criptomoneda | Dinero digital descentralizado basado en criptografía y registrado en un libro contable público. |
| BTC | Abreviatura de bitcoin. |
| Minería | Proceso de resolver acertijos criptográficos para validar bloques de transacciones y obtener monedas nuevas como recompensa. |
| Minero | Persona o equipo que realiza la minería. |
| Blockchain | Cadena de bloques: libro contable distribuido en el que cada bloque enlaza con el anterior y no se puede alterar sin que se note. |
| Billetera | Aplicación o dispositivo que guarda las llaves privadas que permiten disponer de criptomonedas. |
| Prueba de trabajo | Mecanismo de consenso en el que los participantes compiten gastando cómputo para validar bloques. |
| Monero | Criptomoneda orientada a la privacidad que se mina bien con procesadores comunes; la favorita del cryptojacking. |
| Coinhive | Servicio de minería en JavaScript (2017-2019) cuyo abuso popularizó el cryptojacking en navegadores. |
| Script | Pequeño programa que se ejecuta dentro de otro entorno, como el navegador. |
| Grupo de minería | Servicio que reúne el poder de cómputo de muchos mineros y reparte las recompensas. |

## Referencias

- Ethereum.org. (s. f.). *The Merge*. https://ethereum.org/roadmap/merge/
- Intego Mac Security. (2018, 21 de marzo). *How to avoid cryptojacking on a Mac (unwanted cryptocurrency mining)* [Video]. YouTube. https://youtu.be/3DW_MaoCmGU
- Krebs, B. (2018, 26 de marzo). *Who and what is Coinhive?* Krebs on Security. https://krebsonsecurity.com/2018/03/who-and-what-is-coinhive/
- Nakamoto, S. (2008). *Bitcoin: A peer-to-peer electronic cash system*. https://bitcoin.org/bitcoin.pdf
- Palabra de hacker. (2019, 1 de julio). *Cryptojacking: Detección, prevención y casos reales* [Video]. YouTube. https://youtu.be/qk2omuPa_ho
- RSA Conference. (2019, 19 de febrero). *Quick look: Cryptojacking meets IoT* [Video]. YouTube. https://youtu.be/lPIHbFerK0s
- SonicWall. (2024). *2024 SonicWall cyber threat report*. https://www.sonicwall.com/resources/white-papers/2024-sonicwall-cyber-threat-report
- Symantec. (2018). *Internet security threat report* (vol. 23). https://docs.broadcom.com/doc/istr-23-2018-en
- Symantec. (2019, 28 de enero). *What is cryptojacking?* [Video]. YouTube. https://youtu.be/OW4SOfuZ5PE

---

*Rogelio Arriaga Gonzalez*
