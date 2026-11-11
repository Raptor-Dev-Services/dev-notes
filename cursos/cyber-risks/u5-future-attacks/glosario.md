# Glosario - U5 - Prospectivas y ataques futuros

> Los términos de la unidad 5, en orden alfabético.

---

## A

### Aprendizaje automático
**Categoría:** Inteligencia artificial / Fundamentos
**Definición:** También llamado *machine learning*. Rama de la IA en la que un sistema mejora su desempeño a partir de datos, sin que alguien le programe cada regla. Puede ser supervisado (aprende de ejemplos con la respuesta correcta), no supervisado (busca patrones en datos sin etiqueta) o por refuerzo (prueba, recibe premio o castigo y ajusta).
**En el curso:** combinado con el fuzzing, es lo que según Fortinet (2018) volvería baratos los días cero. Ver [13 - Inteligencia artificial](../../../12-seguridad/02-riesgos/13-inteligencia-artificial.md).

---

### Aprendizaje profundo
**Categoría:** Inteligencia artificial / Fundamentos
**Definición:** Forma de aprendizaje automático que usa redes neuronales con muchas capas. Necesita grandes volúmenes de datos y es la base de las herramientas más avanzadas, desde los modelos de lenguaje hasta los generadores de video. Hereda los sesgos de los datos con que se entrena.
**En el curso:** el *deep* de deepfake viene de aquí: *deep learning*.

---

## C

### CGI
**Categoría:** Multimedia
**Definición:** Siglas de *computer-generated imagery*: imágenes en dos o tres dimensiones creadas por computadora para animación y efectos especiales. A diferencia de un deepfake, exige un estudio y especialistas; en el deepfake, la herramienta hace ese trabajo.

---

### Chatbot
**Categoría:** Inteligencia artificial / Aplicaciones
**Definición:** Programa que conversa con una persona y le responde de forma automática, por texto o por voz. Atiende clientes en línea, pero en manos de un atacante también sostiene en tiempo real la conversación de un phishing.

---

### Clonación de voz
**Categoría:** Deepfakes / Técnicas
**Definición:** Síntesis de una voz que imita el acento, la entonación y la manera de hablar de una persona real, a partir de grabaciones suyas. Bastan los videos que alguien publica en redes para obtener la materia prima.
**En el curso:** es la herramienta de los secuestros virtuales; se combate con una palabra clave acordada en familia y llamando al número conocido.

---

### Content Credentials
**Categoría:** Deepfakes / Defensa
**Definición:** Estándar de la Coalición para la Procedencia y Autenticidad del Contenido (C2PA) que adjunta a una imagen o un video un registro firmado de cómo se creó y qué ediciones tuvo. No dice si algo es verdadero: dice de dónde viene.

---

## D

### Deepfake
**Categoría:** Deepfakes / Fundamentos
**Definición:** Video, audio o imagen sintéticos, creados o alterados con redes neuronales profundas, que muestran a una persona real diciendo o haciendo algo que nunca ocurrió. El término nació a finales de 2017 en un foro de Reddit, por el usuario que firmaba "deepfakes".
**En el curso:** en el caso Arup (2024), todos los participantes de una videollamada, menos la víctima, eran deepfakes. Ver [15 - Deepfakes](../../../12-seguridad/02-riesgos/15-deepfakes.md).

---

### Día cero
**Categoría:** Vulnerabilidades
**Definición:** Falla de software que el fabricante todavía no conoce ni ha corregido. Un exploit de día cero la aprovecha, y como nadie la ha visto, las herramientas tradicionales no tienen firma para detenerlo.
**En el curso:** la predicción de la unidad es que la IA los convertiría de rarezas caras en mercancía vendida en la web oscura.

---

### Dividendo del mentiroso
**Categoría:** Deepfakes / Desinformación
**Definición:** Ventaja que obtiene quien niega una prueba auténtica alegando que podría ser falsa. Mientras más convincentes son los deepfakes, más fácil es desechar lo verdadero. El nombre lo propusieron Chesney y Citron (2019).

---

### Dron
**Categoría:** Drones / Fundamentos
**Definición:** Aeronave sin tripulación que vuela sola o a control remoto; también se le dice VANT (vehículo aéreo no tripulado), y la regulación la llama RPAS (sistema de aeronave pilotada a distancia). Para la ciberseguridad es una computadora que vuela: depende de enlaces inalámbricos y se puede atacar o usar para atacar.
**En el curso:** su sola presencia cerró la pista de Gatwick unas 33 horas en diciembre de 2018. Ver [14 - Drones](../../../12-seguridad/02-riesgos/14-drones.md).

---

## F

### Face swapping
**Categoría:** Deepfakes / Técnicas
**Definición:** Intercambio del rostro de una persona por el de otra en una foto o un video. Es un derivado de la misma tecnología del deepfake, pero con resultados casi siempre evidentes: bordes visibles y luz que no coincide.
**En el curso:** los filtros de intercambio de caras de las apps de mensajería son face swapping, no deepfakes.

---

### Fuzzing
**Categoría:** Pruebas de software / Vulnerabilidades
**Definición:** Técnica, casi siempre automatizada, que manda a un programa entradas inválidas, aleatorias o malformadas, en grandes cantidades, para provocar fallas que revelen vulnerabilidades. Durante años fue cosa de laboratorio; con IA se vuelve rápida y accesible.

---

## I

### Infraestructura crítica
**Categoría:** Seguridad nacional
**Definición:** Instalaciones y servicios esenciales, como plantas de energía, aeropuertos, redes de agua o sistemas financieros, que tienen que seguir operando durante un desastre natural o un ataque. Su interrupción afecta la salud, la seguridad o la economía de muchas personas.
**En el curso:** el SPEI y Pemex, en la práctica; Gatwick, en el tema de drones.

---

### Inteligencia artificial (IA)
**Categoría:** Inteligencia artificial / Fundamentos
**Definición:** Simulación, por parte de sistemas informáticos, de tres procesos de la inteligencia humana: aprendizaje, razonamiento y autocorrección. Sus aplicaciones particulares clásicas son los sistemas expertos, el reconocimiento de voz y la visión artificial.

---

### Internet de las cosas (IoT)
**Categoría:** Redes
**Definición:** Conexión a internet de objetos de uso diario (cámaras, termostatos, autos, sensores) para que envíen datos y reciban órdenes. Cada objeto conectado amplía la superficie de ataque: si no se protege ni se actualiza, es una puerta de entrada más a la red donde vive.
**En el curso:** drones y vehículos autónomos son, en la práctica, cosas conectadas con motor.

---

### Inyección de instrucciones
**Categoría:** Inteligencia artificial / Ataques
**Definición:** En inglés, *prompt injection*. Ataque que esconde órdenes dentro del texto que procesa un modelo de IA (un correo, una página, un documento) para que haga algo distinto de lo que pidió su usuario. Encabeza la lista de riesgos de OWASP para aplicaciones con LLM.

---

## J

### Jamming y suplantación de GPS
**Categoría:** Drones / Ataques
**Definición:** Dos ataques contra el enlace de un dron. El *jamming* satura con ruido la frecuencia de control o de GPS para que el aparato pierda la señal. La suplantación (*spoofing*) le manda señales de posición falsas para desviarlo o hacerlo aterrizar donde quiere el atacante.
**En el curso:** como afectan también las comunicaciones de terceros, se reservan a las autoridades.

---

## M

### MITM
**Categoría:** Redes / Ataques
**Definición:** Siglas de *man in the middle*, ataque de intermediario: alguien se coloca entre dos partes que se comunican para leer o alterar lo que se dicen, sin que ninguna lo note.
**En el curso:** un dron con equipo de red puede hacerlo desde la ventana de un edificio.

---

## R

### Remote ID
**Categoría:** Drones / Regulación
**Definición:** Identificación remota: señal que el dron transmite durante el vuelo con su identidad y su posición, como una placa digital. La FAA de Estados Unidos la exige desde el 16 de septiembre de 2023; quien protege una instalación la usa para distinguir vuelos legítimos de intrusos.

---

### RGPD
**Categoría:** Regulación / Datos personales
**Definición:** Reglamento General de Protección de Datos de la Unión Europea (Reglamento (UE) 2016/679). Protege los datos personales y la privacidad, y limita su uso para entrenar y aplicar IA de consumo.
**En el curso:** desde 2024 lo acompaña el Reglamento (UE) 2024/1689, la primera ley integral de IA.
