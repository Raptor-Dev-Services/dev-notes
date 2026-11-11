# 14 - Drones: el juguete que también es plataforma de ataque

> Materia 2, unidad 5 (Prospectivas y ataques futuros), tema 2. Nota recreada a partir del material de la materia, con casos, regulación y usos militares actualizados al 2026.

## Por qué importa

Un dron se compra hoy en una tienda departamental, cabe en una mochila y graba en alta definición desde cientos de metros de altura. Lo que parece un aparato de entretenimiento para el cine, la fotografía o el fin de semana puede convertirse en un arma según las manos que lo controlen. Los drones vigilan, transportan carga, transmiten datos en tiempo real y llegan a lugares restringidos, y todo eso los hace atractivos para militares, espías, agencias de gobierno y delincuentes por igual.

Para la ciberseguridad el tema tiene dos caras. Un dron es una **computadora voladora** que depende de enlaces inalámbricos, así que se puede atacar: interferir, engañar o secuestrar. Y también es una **plataforma de ataque**: puede llevar equipo para interceptar redes, espiar o sabotear infraestructura crítica. Las dos requieren una ciberdefensa que hasta hace poco nadie consideraba.

## Qué es un dron

Un dron es un **vehículo aéreo no tripulado (VANT)** que vuela de forma autónoma o controlado a distancia. El término técnico actual es sistema de aeronave pilotada a distancia (RPAS). Para efectos de esta nota, son vehículos sin tripulación, con cámaras para grabar video y tomar fotografías, manejados por control remoto, y de tamaño muy variable: desde modelos que caben en el bolsillo hasta aparatos grandes con mejores especificaciones de localización, velocidad, alcance de mando y capacidad de carga. Llevan sensores, cámaras y sistemas de comunicación cada vez más avanzados.

### Para qué se usan

Ya no son solo una herramienta militar o de ocio. Hoy sirven para:

- Cine, fotografía y transmisión de eventos.
- Inspección de cultivos y conteo de ganado.
- Entrega de paquetes y de suministros médicos en zonas remotas.
- Inspección de inmuebles, torres, puentes y líneas eléctricas.
- Búsqueda de personas desaparecidas y apoyo en desastres.
- Vigilancia por parte del ejército y la policía, y combate al crimen.
- Conteo de especies en peligro de extinción en un territorio.

Su versatilidad sugiere que la lista seguirá creciendo.

## Por qué son una amenaza

Su capacidad de moverse fuera de la vista y sin tripulación los vuelve atractivos para usos legítimos e ilegítimos por igual. Al mismo tiempo, su dependencia de las conexiones inalámbricas y de las computadoras los hace **vulnerables a ciberataques**, ya sea desde otros drones o desde atacantes en tierra. Hay dinero en usarlos para atacar y también en derrotarlos: las empresas de tecnología militar compiten por diseñar y vender drones capaces de hackear y rastrear, y otras buscan dominar el negocio de neutralizar drones ajenos. Es un mercado todavía inmaduro, con mucha mercadotecnia y pocos productos terminados, pero con un apetito creciente.

| Riesgo | Qué implica |
|---|---|
| Privacidad | Cada vez más cámaras nos observan desde el cielo, y no necesariamente son de la policía: pueden ser de un vecino. Un dron graba y fotografía a distancia sin consentimiento y puede capturar rasgos biométricos del rostro. |
| Detección difícil | Son pequeños, vuelan bajo y se confunden con aves en un radar convencional; detectarlos y neutralizarlos a tiempo es complicado. |
| Crimen organizado | Grupos delictivos los usan para trasladar droga sin exponer a sus operadores. En México, algunos cárteles los adoptaron para mover cargamentos y, más recientemente, para lanzar explosivos. |
| Uso militar y terrorismo | Los drones de guerra electrónica están diseñados para borrar su memoria si caen, para no filtrar información, y pueden cargar explosivos y detonarlos a distancia. Algunos se diseñan para misiones suicidas: destruirse junto con el blanco, sin necesidad de recuperarlos y sin arriesgar al operador. |
| Infraestructura crítica | Basta la presencia de un dron no autorizado cerca de un aeropuerto, una planta eléctrica o una prisión para detener operaciones. |

## Ciberataques desde un dron y contra un dron

Como cualquiera puede comprar un dron y modificarlo, los ataques que antes exigían estar físicamente cerca de una red se vuelven más accesibles y variados.

| Dirección | Técnica | Cómo funciona |
|---|---|---|
| Desde el dron | Intermediario (MITM) | El dron lleva equipo que se coloca entre los dispositivos de un edificio y su red, para leer o alterar el tráfico. |
| Desde el dron | Redes falsas | Crea un punto de acceso Wi-Fi con el nombre de una red conocida para que teléfonos y laptops se conecten solos. |
| Desde el dron | Sustracción de datos | Se acerca a ventanas o azoteas para captar señales inalámbricas, fotografiar pantallas o recoger datos de dispositivos expuestos. |
| Desde el dron | Reconocimiento | Mapea instalaciones, rondas de vigilancia y puntos débiles antes de un ataque físico o digital. |
| Contra el dron | Interferencia (*jamming*) | Satura la frecuencia de control o de GPS para que pierda el enlace. |
| Contra el dron | Suplantación de GPS (*spoofing*) | Envía señales de posicionamiento falsas para desviarlo o hacerlo aterrizar donde quiere el atacante. |
| Contra el dron | Secuestro del enlace | Explota la falta de cifrado o de autenticación entre el control y el dron para tomar su mando. |
| Contra el dron | Robo de datos | Extrae fotos, videos y registros de vuelo del dron o de la app que lo controla. |

Esa doble condición explica la preocupación creciente de gobiernos, empresas y particulares: el mismo aparato que protege una instalación puede atacarla.

## Casos de referencia

- **Aeropuerto de Gatwick, Reino Unido (diciembre de 2018).** Avistamientos repetidos de drones obligaron a cerrar la pista durante unas 33 horas en plena temporada navideña. Cerca de mil vuelos se cancelaron o desviaron y unos 140,000 pasajeros resultaron afectados. Un aeropuerto es infraestructura crítica que debe operar día y noche, y la mera presencia de un aparato barato bastó para paralizarlo.
- **Caracas, Venezuela (agosto de 2018).** Dos drones cargados con explosivos detonaron durante un acto público del presidente Nicolás Maduro. Fue uno de los primeros intentos de magnicidio con drones comerciales modificados documentados en video.
- **Frontera México y Estados Unidos.** Desde mediados de la década de 2010, las autoridades estadounidenses han interceptado drones que cruzan paquetes de metanfetamina y otras drogas por la frontera de San Diego.

> Actualización al 2026: la guerra en Ucrania, desde 2022, convirtió a los drones baratos y modificados en un arma central, con miles de unidades al mes y una competencia constante entre interferencia y contramedidas. En México, grupos del crimen organizado usan drones con explosivos contra fuerzas de seguridad y comunidades: el Ejército reportó 260 ataques con drones explosivos en 2023, la mayoría en Michoacán. Las dos tendencias confirman lo que el material anticipaba: un aparato comercial más software modificado basta para crear una amenaza de nivel militar.

## Regulación

> Actualización al 2026: el material original no abordaba la regulación.

- **México:** la Norma Oficial Mexicana NOM-107-SCT3-2019 (DOF, 14 de noviembre de 2019) establece los requerimientos para operar sistemas de aeronaves pilotadas a distancia. Clasifica los equipos por su peso y por su uso (recreativo, privado o comercial) y, conforme crece la categoría, exige registro, licencia del piloto y seguro, además de respetar las zonas restringidas cerca de aeropuertos e instalaciones estratégicas.
- **Estados Unidos:** la Administración Federal de Aviación (FAA) exige desde el 16 de septiembre de 2023 el *Remote ID*, una especie de placa digital que el dron transmite durante el vuelo y que permite identificarlo a distancia.
- **Unión Europea:** los Reglamentos de Ejecución (UE) 2019/947 y Delegado 2019/945 clasifican las operaciones por nivel de riesgo y obligan a registrar a los operadores.

## Defensa

Para quien protege instalaciones:

- **Detección por capas:** radar especializado, sensores de radiofrecuencia que detectan el enlace de control, cámaras ópticas y térmicas, y sensores acústicos.
- **Identificación:** leer el Remote ID o la telemetría para distinguir operaciones legítimas de intrusas.
- **Mitigación:** las medidas activas (interferir, suplantar el GPS, capturar con redes) suelen estar reservadas a autoridades, porque afectan comunicaciones de terceros; las empresas privadas deben coordinarse con ellas.
- **Geocercas:** los fabricantes incluyen zonas en las que el dron se niega a despegar o a entrar, aunque se pueden desactivar con firmware modificado.
- **Endurecer la red propia:** segmentar el Wi-Fi, no transmitir redes abiertas cerca de áreas sensibles y cifrar todo, porque un dron puede acercarse a cualquier ventana.

Para quien opera un dron:

- Actualizar el firmware del dron y de la app de control.
- Cambiar contraseñas de fábrica y preferir modelos con enlace cifrado.
- Revisar qué datos de vuelo y video se suben a la nube del fabricante.
- Respetar la privacidad de otras personas y las zonas restringidas.

## Para cerrar

Los drones se perciben como simples juguetes, pero pueden ser herramientas valiosas para el crimen organizado y el terrorismo, sobre todo cuando se combinan con capacidades digitales. Los riesgos van más allá del daño físico: invasión de la privacidad, manipulación de datos e interrupción de infraestructura crítica. Hacen falta medidas técnicas y legales que regulen su uso sin perder sus beneficios, y una comprensión de cómo funcionan y cómo se modifican, que es lo que permite anticipar sus usos maliciosos.

## Glosario

| Término | Definición |
|---------|-----------|
| Dron | Aeronave no tripulada, autónoma o controlada a distancia, de uso recreativo, comercial o militar. |
| VANT | Vehículo aéreo no tripulado. |
| RPAS | Sistema de aeronave pilotada a distancia, término técnico usado por la regulación. |
| Remote ID | Identificación remota: señal que el dron emite en vuelo con su identidad y posición. |
| Geocerca | Límite virtual que impide al dron despegar o volar en ciertas zonas. |
| Jamming | Interferencia deliberada de una señal de radio para cortar la comunicación. |
| Suplantación de GPS | Envío de señales de posicionamiento falsas para engañar a un receptor. |
| MITM | Ataque de intermediario que intercepta y puede alterar la comunicación entre dos partes. |
| Munición merodeadora | Dron diseñado para sobrevolar una zona y estrellarse contra un blanco con su carga explosiva. |
| Infraestructura crítica | Instalaciones esenciales, como aeropuertos o centrales eléctricas, que deben operar sin interrupciones. |
| Antidrón | Conjunto de tecnologías para detectar, identificar y neutralizar drones no autorizados. |

## Referencias

- ABC 10 News. (2017, 18 de agosto). *Drone caught flying packages of meth over San Diego border* [Video]. YouTube. https://youtu.be/l_rwBxaItDs
- BBC News. (2018, 20 de diciembre). *Gatwick Airport: Drone sightings cause delays* [Video]. YouTube. https://youtu.be/14NQbf9m2VU
- BBC News. (2018, 4 de agosto). *Venezuela President Maduro survives "drone assassination attempt"*. https://www.bbc.com/news/world-latin-america-45073385
- CBS News. (2023, 23 de agosto). *Drug cartels are sharply increasing use of bomb-dropping drones, Mexican army says*. https://www.cbsnews.com/news/drug-cartels-more-bomb-dropping-drones-mexico-army/
- Davis, G. (2018, 21 de diciembre). Rogue drones cause Gatwick Airport to close for over 30 hours: More on this threat. *McAfee Blogs*. https://web.archive.org/web/20210303081230/https://www.mcafee.com/blogs/consumer/consumer-threat-notices/rogue-drones-cause-gatwick-airport-to-close/
- Federal Aviation Administration. (s. f.). *Remote identification of drones*. https://www.faa.gov/uas/getting_started/remote_id
- Incredible Facts. (2019, 6 de enero). *10 best military drones in the world* [Video]. YouTube. https://youtu.be/muEJGEQA1Yk
- Make It Extreme. (2017, 18 de julio). *Homemade drone catcher* [Video]. YouTube. https://youtu.be/mi3Ed-paCE4
- Rowlatt, J. (2019, 14 de abril). *Gatwick drone attack possible inside job, say police*. BBC News. https://www.bbc.co.uk/news/uk-47919680
- Satti, B. (2016). *Are drone-led cyberattacks the wave of the future?* Security Intelligence. https://web.archive.org/web/20170128210827/https://securityintelligence.com/are-drone-led-cyberattacks-the-wave-of-the-future/
- Secretaría de Comunicaciones y Transportes. (2019). *NOM-107-SCT3-2019, que establece los requerimientos para operar un sistema de aeronave pilotada a distancia (RPAS) en el espacio aéreo mexicano*. Diario Oficial de la Federación. https://dof.gob.mx/nota_detalle.php?codigo=5578813&fecha=14/11/2019
- The New York Times. (2018, 12 de agosto). *How the drone attack on Maduro unfolded in Venezuela* [Video]. YouTube. https://youtu.be/EpFNCqCwVzo

---

*Rogelio Arriaga Gonzalez*
