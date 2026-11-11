# Glosario - U1 - Ciberseguridad y actualidad

> Los términos de la unidad 1, en orden alfabético.

---

## A

### Algoritmo
**Categoría:** Computación / Fundamentos
**Definición:** Serie finita de pasos precisos que resuelve un problema. Cuando ese procedimiento se escribe en un lenguaje de programación, se convierte en un programa.

---

### ARPANET
**Categoría:** Historia de internet
**Definición:** Red experimental financiada por la agencia de proyectos de investigación avanzada (ARPA, después DARPA) del Departamento de Defensa de Estados Unidos. Arrancó en 1969 con cuatro nodos universitarios y es el antecedente directo de internet.
**En el curso:** el primer nodo fue el laboratorio de Kleinrock en UCLA; el 1 de enero de 1983 la red cambió a TCP/IP. Ver [nota 01](../../../12-seguridad/02-riesgos/01-tic-internet-y-seguridad.md).

---

### Arquitectura abierta
**Categoría:** Historia de internet / Redes
**Definición:** Principio según el cual redes con diseños distintos pueden interconectarse sin modificar su funcionamiento interno; basta con que acuerden cómo intercambiar paquetes en la frontera.
**En el curso:** Bob Kahn la propuso en 1972 a partir del programa de radio por paquetes de DARPA, que necesitaba unir una red inalámbrica con ARPANET.

---

## C

### CERT
**Categoría:** Instituciones / Respuesta a incidentes
**Definición:** Equipo de respuesta a emergencias informáticas: recibe reportes, analiza incidentes, emite alertas y coordina la respuesta.
**En el curso:** en México, el CERT-MX pasó de la Policía Federal a la Dirección General Científica de la Guardia Nacional. Ver [nota 03](../../../12-seguridad/02-riesgos/03-ciberseguridad-en-mexico.md).

---

### Ciberentorno
**Categoría:** Conceptos generales
**Definición:** Espacio virtual que se forma cuando personas, sistemas y datos interactúan por medios digitales. Es el lugar donde están los activos que la ciberseguridad protege.

---

### Ciberseguridad
**Categoría:** Conceptos generales
**Definición:** Conjunto de herramientas, políticas, salvaguardas, métodos de gestión de riesgos, formación, buenas prácticas, seguros y tecnologías que se usan para proteger los activos de organizaciones y usuarios en el ciberentorno.
**En el curso:** es la definición de la Recomendación X.1205 de la UIT (2008), y sus objetivos se resumen en la tríada CID.

---

### Conmutación de paquetes
**Categoría:** Redes
**Definición:** Forma de transmitir datos en la que la información se divide en bloques pequeños (paquetes) que viajan por separado, pueden seguir rutas distintas y se reensamblan en el destino. Se opone a la conmutación de circuitos, que reserva una línea de punta a punta.
**En el curso:** Kleinrock publicó la teoría en 1961; el término "paquete" viene del NPL británico.

---

## D

### Dirección IP
**Categoría:** Redes
**Definición:** Número que identifica a un dispositivo dentro de una red para que los paquetes sepan a dónde ir. En IPv4 son cuatro números separados por puntos; IPv6 usa direcciones mucho más largas.

---

## E

### Estrategia Nacional de Ciberseguridad (ENCS)
**Categoría:** Política pública / México
**Definición:** Documento que fija la visión del Estado mexicano en ciberseguridad. Se presentó el 13 de noviembre de 2017, con apoyo de la OEA, y se organiza en tres principios rectores, cinco objetivos estratégicos y ocho ejes transversales, con mirada a 2030.
**En el curso:** no se volvió ley ni se actualizó entre 2018 y 2024; en diciembre de 2025 se publicó la Política General de Ciberseguridad para la Administración Pública Federal.

---

### Exploit
**Categoría:** Amenazas
**Definición:** Código, secuencia de comandos o técnica que aprovecha una vulnerabilidad para tomar el control de un sistema o hacer que se comporte como el atacante quiere. Los conocidos se neutralizan actualizando; los de día cero atacan huecos que el fabricante todavía no conoce y por eso no tienen parche.
**En el curso:** EternalBlue, filtrado en 2017, fue el exploit que usó WannaCry para propagarse.

---

## I

### Infraestructura crítica
**Categoría:** Seguridad nacional
**Definición:** Instalaciones, redes y servicios cuya falla afectaría gravemente la salud, la seguridad o la economía de un país: energía, agua, hospitales, telecomunicaciones, sistemas financieros.
**En el curso:** es uno de los ocho ejes transversales de la ENCS y un blanco cada vez más frecuente del ransomware.

---

### Internet de las cosas (IoT)
**Categoría:** Tecnologías
**Definición:** Objetos de uso diario (relojes, cámaras, electrodomésticos, sensores) conectados a internet que recogen datos y los intercambian. Cada uno es un punto de entrada más que hay que proteger.

---

## M

### Malware
**Categoría:** Amenazas
**Definición:** Software hecho con la intención de dañar, interrumpir o conseguir acceso no autorizado a un sistema. Es la categoría general que incluye virus, gusanos, troyanos y ransomware.

---

## P

### Perímetro
**Categoría:** Arquitectura de seguridad
**Definición:** Frontera tradicional entre la red interna de una organización e internet, donde se concentraban los controles de seguridad. Se desdibuja cuando los datos, las aplicaciones y las identidades se mudan a la nube y se multiplican los dispositivos IoT.

---

## R

### Ransomware
**Categoría:** Amenazas
**Definición:** Malware que cifra los archivos de la víctima o le bloquea el acceso y exige un pago, normalmente en criptomonedas, para devolverlos. Hoy suele combinarse con el robo previo de datos (doble extorsión).
**En el curso:** SamSam (2016) lo propagó por la red sin intervención del usuario; WannaCry (2017) lo unió con un gusano y se volvió un *cryptoworm*. Ver [nota 02](../../../12-seguridad/02-riesgos/02-ciberseguridad-panorama-general.md).

---

### Red
**Categoría:** Redes
**Definición:** Dos o más equipos conectados para compartir recursos e información. Internet es una red de redes unidas por los mismos protocolos.

---

### Responsabilidad compartida
**Categoría:** Nube
**Definición:** Modelo con el que los proveedores de nube reparten la seguridad: el proveedor protege la infraestructura física y la plataforma, y el cliente responde por sus datos, identidades, configuraciones y accesos.
**En el curso:** la mayoría de los incidentes en la nube vienen del lado del cliente, por credenciales robadas o configuraciones abiertas.

---

## T

### TCP/IP
**Categoría:** Redes / Protocolos
**Definición:** Familia de protocolos con la que funciona internet. IP se encarga de direccionar y enviar los paquetes; TCP, de que lleguen completos y en orden. Viene integrada en todos los sistemas operativos actuales.
**En el curso:** la diseñaron Kahn y Cerf entre 1973 y 1978, y ARPANET la adoptó el 1 de enero de 1983.

---

### Tríada CID
**Categoría:** Conceptos generales
**Definición:** Los tres objetivos de la seguridad de la información: confidencialidad (solo accede quien debe), integridad (nadie altera la información sin permiso) y disponibilidad (la información está ahí cuando se necesita).

---

## V

### Vulnerabilidad
**Categoría:** Amenazas
**Definición:** Debilidad en un sistema, una aplicación, una configuración o un proceso que un atacante puede aprovechar. Se corrige con un parche o, mientras no lo hay, se reduce con medidas de mitigación.
**En el curso:** es lo que aprovecha cualquier exploit; una base de datos en la nube sin contraseña, como la lista nominal expuesta en 2016, también lo es.
