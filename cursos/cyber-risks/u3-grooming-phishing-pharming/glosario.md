# Glosario - U3 - Grooming, phishing y pharming

> Los términos de la unidad 3, en orden alfabético.

---

## A

### Archivo hosts
**Categoría:** Redes / Navegación
**Definición:** Archivo de texto del sistema operativo que asocia nombres de dominio con direcciones IP. El equipo lo revisa antes de preguntarle al DNS, así que quien logre modificarlo decide a dónde va el navegador con esos nombres, sin tocar nada fuera de la máquina.
**En el curso:** es el blanco del pharming local; un antimalware actualizado debe impedir que se altere. Ver [09 - Pharming](../../../12-seguridad/02-riesgos/09-pharming.md).

---

## D

### DNS (Domain Name System)
**Categoría:** Redes / Navegación
**Definición:** Sistema de nombres de dominio: el directorio distribuido de internet que traduce nombres fáciles de recordar en las direcciones IP que las máquinas necesitan para conectarse. Cada consulta pasa por servidores (el del router, el del proveedor) en los que el usuario confía sin verlos.
**En el curso:** el pharming no ataca al banco ni a la víctima, ataca esta traducción.

---

### DNSSEC
**Categoría:** Defensa / Infraestructura
**Definición:** Extensiones de seguridad del DNS que firman las respuestas para poder comprobar que nadie las alteró en el camino. Frenan el envenenamiento de caché, pero no protegen a quien ya consulta un resolvedor del atacante y no valida las firmas por su cuenta.
**En el curso:** su adopción se aceleró después de la vulnerabilidad de Kaminsky, en 2008.

---

### Drive-by pharming
**Categoría:** Ataques / Pharming
**Definición:** Variante del pharming que cambia el servidor DNS configurado en un router o firewall doméstico, casi siempre entrando con la contraseña de administración que nadie cambió. A partir de ahí, todos los dispositivos de esa red reciben las respuestas que el atacante quiera.
**En el curso:** el primer caso documentado fuera del laboratorio ocurrió en México, en enero de 2008, con routers 2Wire y el sitio de un banco mexicano.

---

## E

### Envenenamiento de caché DNS
**Categoría:** Ataques / Pharming
**Definición:** Técnica para que un servidor DNS guarde en su memoria una respuesta falsa sobre un dominio. Mientras ese registro dure, todos los que consulten ese servidor irán al sitio del atacante, aunque sus equipos estén perfectamente limpios.
**En el curso:** la vulnerabilidad que Dan Kaminsky mostró en 2008 (CVE-2008-1447) permitía lograrlo en segundos contra casi cualquier servidor.

---

## G

### Grooming
**Categoría:** Riesgos / Menores
**Definición:** Tipo de acoso en el que un adulto, por medios digitales, se gana la confianza de una niña, niño o adolescente con el propósito de abusar de él o explotarlo sexualmente. Se distingue del cyberbullying porque quien agrede no es un par y la finalidad es sexual.
**En el curso:** sus cuatro etapas, en orden, son inicio del contacto, cercanía, fase sexual y contacto real. En México lo sanciona el art. 199 Septies del Código Penal Federal. Ver [07 - Grooming](../../../12-seguridad/02-riesgos/07-grooming.md).

---

## I

### Ingeniería social
**Categoría:** Factor humano
**Definición:** Técnicas de manipulación psicológica para que una persona entregue información, abra un archivo o haga algo que no haría si supiera quién se lo pide de verdad. Explota la confianza, el miedo, la prisa y la curiosidad, no las fallas del software.
**En el curso:** está detrás de los tres ataques de la unidad: el perfil falso del grooming, el pretexto del phishing y el adjunto que instala el malware del pharming local.

---

## N

### Navegador
**Categoría:** Redes / Software
**Definición:** Programa con el que se accede a los sitios web y se ven sus contenidos. Además de mostrar páginas, revisa certificados, filtra sitios de phishing conocidos y puede cifrar las consultas DNS; sus advertencias protegen solo si nadie las salta.
**En el curso:** una advertencia de certificado en el sitio del banco es motivo para detenerse, no para dar clic en "continuar".

---

## P

### Passkey
**Categoría:** Defensa / Autenticación
**Definición:** Credencial basada en criptografía de llave pública (estándar FIDO2) que reemplaza a la contraseña. Queda ligada al dominio legítimo, así que una página falsa no puede capturarla ni volver a usarla.
**En el curso:** es la defensa técnica más eficaz contra el robo de credenciales por phishing.

---

### Pharming
**Categoría:** Ataques / Redirección
**Definición:** Ataque que desvía el tráfico destinado a un sitio legítimo hacia uno falso sin que el usuario lo note, alterando el archivo hosts, el router, un servidor DNS o la red local. A diferencia del phishing, no necesita que la víctima se crea nada: le basta con que navegue como siempre.
**En el curso:** es la razón por la que "escribe tú la dirección" no es un consejo suficiente.

---

### Phishing
**Categoría:** Ataques / Ingeniería social
**Definición:** Engaño en el que el atacante se hace pasar por alguien de confianza (banco, tienda, autoridad, compañero de trabajo) para que la víctima abra un enlace, llegue a un sitio falso y entregue credenciales o datos de tarjeta, o instale malware.
**En el curso:** su blanco principal son las empresas, y en 2025 fue la categoría con más denuncias en el informe del IC3 del FBI (191,561). Ver [08 - Phishing](../../../12-seguridad/02-riesgos/08-phishing.md).

---

## R

### Redireccionamiento web
**Categoría:** Redes / Navegación
**Definición:** Mecanismo por el que un dominio o una dirección apunta hacia otra. Es legítimo y cotidiano (un sitio que se mudó, una versión para móvil), y justo por eso un atacante puede aprovecharlo para llevar a la víctima a donde le conviene.
**En el curso:** el pharming es, en el fondo, un redireccionamiento que nadie autorizó.

---

## S

### Sextorsión
**Categoría:** Riesgos / Menores
**Definición:** Chantaje con material íntimo de la víctima para conseguir más imágenes, dinero o un encuentro. En el grooming aparece cuando el menor quiere salir: el agresor amenaza con difundir lo que ya obtuvo.
**En el curso:** en los reportes que analizaron Quayle y Newman (2016), el agresor amenazó al menor, con difundir imágenes u otro daño, en el 24.1 % de los casos.

---

### Smishing
**Categoría:** Ataques / Phishing
**Definición:** Phishing por mensaje de texto y, cada vez más, por apps de mensajería: un mensaje breve a nombre de una persona o empresa conocida que pide abrir un enlace para robar datos o meter malware en el teléfono. El nombre viene de "SMS phishing".
**En el curso:** el aviso de paquete retenido del ejercicio de la práctica es un smishing.

---

### Spear phishing
**Categoría:** Ataques / Phishing
**Definición:** Phishing dirigido a una persona o entidad concreta y armado con información real sobre ella (cargo, contactos, horarios) para que el mensaje resulte creíble. Su tasa de éxito es mucho mayor que la del envío masivo.
**En el curso:** el whaling es su versión contra altos directivos.

---

## U

### Usurpación de identidad
**Categoría:** Riesgos / Consecuencias
**Definición:** Apropiarse de la identidad de otra persona y presentarse como ella, en público o en privado, para obtener créditos, recursos u otros beneficios a su nombre.
**En el curso:** es una de las consecuencias más frecuentes del phishing y del pharming, porque los dos empiezan por robar datos personales.

---

## V

### Vishing
**Categoría:** Ataques / Phishing
**Definición:** Phishing por llamada de voz, tradicional o por internet (la *v* viene de *voice*). El delincuente usa datos de redes sociales para sonar creíble y, con inteligencia artificial, hoy puede imitar la voz de un familiar o de un directivo.

---

### VoIP (Voice over Internet Protocol)
**Categoría:** Redes / Telecomunicaciones
**Definición:** Voz sobre protocolo de internet: tecnología que transmite llamadas como paquetes de datos por internet, en lugar de usar la red telefónica tradicional.
**En el curso:** es el canal al que se mudó el vishing, junto con la voz sintética.

---

## W

### Whaling
**Categoría:** Ataques / Phishing
**Definición:** Spear phishing dirigido a altos directivos, los "peces gordos" de una organización, porque tienen acceso a información sensible y autoridad para aprobar pagos.
**En el curso:** no confundirlo con el compromiso de correo empresarial (BEC): en el whaling el directivo es la víctima; en el BEC, el atacante suplanta al directivo o toma su cuenta para pedirle transferencias a otros.
