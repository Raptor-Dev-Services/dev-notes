# 05 - La seguridad de la información y su complejidad en las organizaciones

> Unidad 2, tema 2 de la materia *Seguridad de la información*. Las cuatro dimensiones que hacen difícil proteger la información de una organización: legal, matemática, tecnológica y geopolítica. Revisado a noviembre de 2026.

## Por qué importa

En la era digital, proteger datos y sistemas es vital para cualquier organización, pero no basta con saber de redes. Un responsable de seguridad tiene que cumplir leyes y estándares de la industria, entender (al menos en lo esencial) las matemáticas que sostienen el cifrado, seguir el paso de amenazas que cambian cada mes y leer el tablero geopolítico donde se fabrican los equipos que usa. Las amenazas son cada vez más sofisticadas y los riesgos más diversos; la pregunta de fondo de este tema es **cómo organizar todos esos elementos y cumplir a la vez con requisitos propios, legales y normativos**. La respuesta (los marcos de referencia) llega en la nota [06](06-marcos-de-referencia-y-arquitecturas.md); aquí se dibuja el tamaño del problema.

## Dimensión legal y normativa

Una organización no decide sola cuánta seguridad necesita: la ley y sus contratos le fijan pisos mínimos.

### Protección de datos personales en México

La **Ley Federal de Protección de Datos Personales en Posesión de los Particulares** obliga a cualquier empresa o persona que trate datos personales a respetar principios como la licitud, el consentimiento, la información (avisar al titular qué se hace con sus datos), la calidad, la finalidad, la lealtad, la proporcionalidad y la responsabilidad. Impone además dos deberes que son, literalmente, seguridad de la información:

- **Deber de seguridad:** establecer y mantener medidas administrativas, técnicas y físicas que protejan los datos contra daño, pérdida, alteración, destrucción, uso, acceso o tratamiento no autorizado.
- **Deber de confidencialidad:** guardar secreto sobre los datos, incluso después de terminada la relación con el titular.

Los titulares tienen los derechos de acceso, rectificación, cancelación y oposición (derechos ARCO), y la organización debe tener procedimientos para atenderlos en plazo.

> Actualización al 2026: la ley de 2010 fue abrogada. El 20 de marzo de 2025 se publicó en el Diario Oficial de la Federación una nueva Ley Federal de Protección de Datos Personales en Posesión de los Particulares, vigente desde el día siguiente. El Instituto Nacional de Transparencia, Acceso a la Información y Protección de Datos Personales (INAI) desapareció y sus funciones en esta materia pasaron a la Secretaría Anticorrupción y Buen Gobierno. Las multas se expresan en Unidades de Medida y Actualización y van de 100 a 320,000 UMA, con agravantes cuando se trata de datos sensibles.

### El estándar de la industria de tarjetas de pago

Quien almacena, procesa o transmite datos de tarjetas de pago debe cumplir el **PCI DSS** (*Payment Card Industry Data Security Standard*), que no es una ley sino un requisito contractual que imponen las marcas de tarjetas a través de los bancos adquirentes. Sus doce requisitos se agrupan en seis objetivos:

| Objetivo | Requisitos |
|---|---|
| Redes y sistemas seguros | 1. Instalar y mantener controles de seguridad de red. 2. Aplicar configuraciones seguras a todos los componentes |
| Proteger los datos de cuenta | 3. Proteger los datos almacenados. 4. Cifrar con criptografía robusta los datos que viajan por redes públicas |
| Gestión de vulnerabilidades | 5. Proteger sistemas y redes contra software malicioso. 6. Desarrollar y mantener sistemas y software seguros |
| Control de acceso | 7. Restringir el acceso según la necesidad de conocer. 8. Identificar usuarios y autenticar el acceso. 9. Restringir el acceso físico a los datos |
| Monitoreo y pruebas | 10. Registrar y vigilar todo acceso a los componentes y a los datos. 11. Probar la seguridad de sistemas y redes con regularidad |
| Política de seguridad | 12. Sostener la seguridad con políticas y programas organizacionales |

> Actualización al 2026: la versión vigente es PCI DSS v4.0.1 (junio de 2024). La v3.2.1 se retiró el 31 de marzo de 2024 y los requisitos que la v4.0 marcó con fecha futura son obligatorios desde el 31 de marzo de 2025.

Una misma empresa puede estar sujeta a la ley de datos personales, a PCI DSS, a regulación sectorial (por ejemplo, la de la CNBV para bancos, que se estudia en las notas [10](10-activar-el-sgsi.md) y [14](14-plan-de-continuidad.md)) y a las cláusulas de seguridad de sus contratos. Organizar todo eso sin duplicar esfuerzos ya es un proyecto en sí mismo.

## Dimensión matemática

La seguridad de la información descansa en conceptos matemáticos avanzados. No hace falta ser matemático para gestionarla, pero sí entender qué garantiza cada técnica y qué no.

### Criptografía, cifrado y "encriptamiento"

La **criptografía** es la disciplina que diseña técnicas para proteger la información frente a quien no debe leerla o modificarla. Es la herramienta principal para garantizar la **confidencialidad**, y con funciones de resumen (*hash*) y firmas digitales también apoya la integridad, la autenticidad y el no repudio.

**Cifrar** es transformar un texto legible en otro ilegible con un algoritmo y una llave; **descifrar** es el proceso inverso. En el ámbito técnico se oye mucho "encriptar" y "encriptamiento", calcos del inglés *encrypt*: se refieren al mismo proceso de cifrado.

| Tipo | Cómo funciona | Para qué se usa |
|---|---|---|
| Simétrico | La misma llave cifra y descifra | Cifrar grandes volúmenes: discos, bases de datos, tráfico de una sesión |
| Asimétrico | Un par de llaves: la pública cifra o verifica, la privada descifra o firma | Intercambiar llaves, firmas digitales, certificados |
| Funciones resumen | Producen una huella de tamaño fijo; no tienen llave ni se revierten | Verificar integridad, guardar contraseñas (con sal y algoritmos lentos) |

### Aritmética modular

Buena parte de la criptografía de llave pública se apoya en la **aritmética modular**: operar con los residuos de una división, como un reloj que vuelve a cero al llegar a 12. Calcular una potencia módulo un número es fácil para una computadora; deshacer la operación sin conocer cierta información secreta (factorizar un número enorme, o encontrar un logaritmo discreto) es, con la tecnología actual, impracticable. Esa asimetría entre lo fácil y lo difícil es la que aprovechan algoritmos como RSA y Diffie-Hellman.

### Curvas elípticas

Las **curvas elípticas** forman hoy parte de los estándares industriales de criptografía. Ofrecen la misma resistencia que RSA con llaves mucho más cortas (una llave de 256 bits de curva elíptica equivale aproximadamente a una RSA de 3072 bits), lo que ahorra cómputo, batería y ancho de banda. Por eso dominan en teléfonos, tarjetas inteligentes y en el intercambio de llaves de TLS.

> Actualización al 2026: la amenaza a largo plazo para RSA y las curvas elípticas es la computación cuántica. En agosto de 2024 el NIST publicó los primeros estándares de criptografía poscuántica: FIPS 203 (ML-KEM, para establecer llaves), FIPS 204 (ML-DSA) y FIPS 205 (SLH-DSA), ambos para firma digital. Las organizaciones ya planean la migración, empezando por los datos que deben seguir siendo confidenciales dentro de diez o más años.

## Dimensión tecnológica

La tecnología cambia más rápido que cualquier plan de seguridad. Tres ejemplos de la complejidad técnica que hay que gestionar:

- **Vulnerabilidades en dispositivos móviles de última generación.** El teléfono más nuevo trae sensores, aplicaciones de terceros, almacenamiento de credenciales y conexión permanente. Un fallo en su sistema operativo o en una aplicación popular afecta a millones de equipos en todos los países a la vez, y la organización depende del fabricante para corregirlo.
- **Bibliotecas de enlace dinámico (DLL).** Una DLL es un componente del sistema operativo Windows que contiene código y datos que varios programas cargan al ejecutarse, en lugar de llevar cada uno su propia copia. La comodidad tiene su precio: si un atacante logra que un programa cargue una DLL maliciosa en lugar de la legítima (técnicas conocidas como secuestro o carga lateral de DLL), su código corre con los permisos de ese programa.
- **Dispositivos de seguridad perimetral.** Los equipos que concentran varias defensas en el borde de la red (firewall, prevención de intrusos, antivirus de puerta de enlace, filtrado de contenidos, VPN) se conocen con muchos nombres: UTM (*unified threat management*), firewall de nueva generación, *appliance* de seguridad. La variedad de nombres complica comparar productos y entender qué protege realmente cada uno.

## Dimensión geopolítica

Las tecnologías y sus vulnerabilidades cruzan fronteras. El hardware se diseña en un país, se fabrica en otro y se usa en todo el mundo; el software se actualiza desde servidores que obedecen a la ley de su jurisdicción. Las decisiones de seguridad reflejan así dinámicas geopolíticas globales: países que compiten por el liderazgo tecnológico y por la estabilidad, restricciones a proveedores por motivos de seguridad nacional, normas técnicas que nacen en los países que crearon internet y que el resto adopta. Revisar los documentos de seguridad nacional de esos países permite anticipar el rumbo de sus programas tecnológicos, lo que para otros países puede representar un riesgo (ver la nota [06](06-marcos-de-referencia-y-arquitecturas.md)).

## Un mapa del territorio

Un manual de referencia como el *Computer and Information Security Handbook* (Vacca) da una idea del tamaño del campo con solo leer su índice: fundamentos de seguridad de sistemas y redes, gestión de la seguridad, criptografía, privacidad y gestión de accesos, seguridad del almacenamiento, recuperación ante desastres, análisis forense y tecnologías emergentes, cada una con su teoría, su tecnología y su práctica. Nadie domina todo; el trabajo del estratega es saber qué piezas necesita su organización y cómo encajarlas.

## Glosario

| Término | Definición |
|---|---|
| Datos personales | Cualquier información que identifica o hace identificable a una persona física |
| Derechos ARCO | Derechos del titular a acceder, rectificar, cancelar y oponerse al tratamiento de sus datos |
| Deber de seguridad | Obligación de proteger los datos personales con medidas administrativas, técnicas y físicas |
| PCI DSS | Estándar de seguridad que deben cumplir quienes manejan datos de tarjetas de pago |
| Criptografía | Disciplina que diseña técnicas para proteger la información frente a lectura o modificación no autorizada |
| Cifrado | Transformación de información legible en ilegible mediante un algoritmo y una llave; "encriptamiento" en el habla técnica |
| Cifrado asimétrico | Esquema con un par de llaves, una pública y una privada |
| Aritmética modular | Operaciones con los residuos de una división; base de RSA y Diffie-Hellman |
| Curva elíptica | Estructura matemática que permite criptografía de llave pública con llaves cortas |
| Criptografía poscuántica | Algoritmos diseñados para resistir ataques con computadoras cuánticas |
| DLL | Biblioteca de enlace dinámico: componente que varios programas cargan al ejecutarse |
| UTM | Dispositivo que concentra varias defensas perimetrales; también llamado firewall de nueva generación |

## Referencias

- Barker, E. (2020). *Recommendation for key management: Part 1. General* (NIST Special Publication 800-57 Part 1 Rev. 5). National Institute of Standards and Technology. https://csrc.nist.gov/pubs/sp/800/57/pt1/r5/final
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley Federal de Protección de Datos Personales en Posesión de los Particulares* (DOF 20-03-2025, última reforma DOF 14-11-2025). https://www.diputados.gob.mx/LeyesBiblio/pdf/LFPDPPP.pdf
- National Institute of Standards and Technology. (2024). *Module-lattice-based key-encapsulation mechanism standard* (FIPS 203). https://csrc.nist.gov/pubs/fips/203/final
- PCI Security Standards Council. (2024). *Payment Card Industry Data Security Standard: Requirements and testing procedures* (v4.0.1). https://www.pcisecuritystandards.org/document_library/
- Secretaría Anticorrupción y Buen Gobierno. (2026). *Secretaría Anticorrupción y Buen Gobierno*. Gobierno de México. https://www.gob.mx/buengobierno
- Vacca, J. R. (Ed.). (2013). *Computer and information security handbook* (2.ª ed.). Morgan Kaufmann. https://www.sciencedirect.com/book/9780123943972/computer-and-information-security-handbook

---

*Rogelio Arriaga Gonzalez*
