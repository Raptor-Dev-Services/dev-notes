# Glosario - U4 - Carding, juice jacking y cryptojacking

> Los términos de la unidad 4, en orden alfabético.

---

## B

### BIN o IIN
**Categoría:** Tarjetas / Identificación
**Definición:** *Bank Identification Number*, también llamado *Issuer Identification Number*. Son los primeros dígitos del número de una tarjeta y dicen a qué red pertenece y qué banco la emitió. No son los 16 dígitos completos: tradicionalmente eran seis y la revisión de 2017 de la norma ISO/IEC 7812-1 los llevó a ocho, que las redes de pago adoptaron hacia 2022.
**En el curso:** a partir del BIN se pueden fabricar números con formato válido; la nota [10-carding.md](../../../12-seguridad/02-riesgos/10-carding.md) explica por qué el carder todavía tiene que probarlos.

---

### Binero
**Categoría:** Carding / Personas
**Definición:** Quien fabrica números de tarjeta partiendo del BIN de un banco. Los números tienen el formato correcto, pero nadie sabe si corresponden a una cuenta activa hasta que se prueban contra un sistema de cobro.

---

### Bloqueador de datos USB
**Categoría:** Juice jacking / Defensa
**Definición:** Adaptador pequeño que se pone en la punta del cable y corta electrónicamente las líneas de datos, de modo que solo pasa la energía. Se le dice coloquialmente "condón USB" y convierte cualquier cable en uno de solo carga.
**En el curso:** es la salida recomendada cuando no queda más remedio que cargar en un puerto público.

---

### BTC
**Categoría:** Criptomonedas
**Definición:** Abreviatura con la que se identifica a bitcoin, la primera criptomoneda, lanzada en 2009.

---

## C

### Cable de solo carga
**Categoría:** Juice jacking / Defensa
**Definición:** Cable USB que lleva únicamente los conductores de energía y tierra, sin los dos de datos que tiene un cable normal en el USB clásico. Sin esos hilos no hay por dónde pasar información. Por fuera se ve igual que uno de datos, así que conviene marcarlo.
**En el curso:** no siempre carga más rápido; algunos protocolos de carga rápida necesitan las líneas que este cable no tiene.

---

### Carder
**Categoría:** Carding / Personas
**Definición:** Delincuente que se dedica al carding: consigue datos de tarjetas ajenas, los prueba y los usa o los revende.

---

### Carding
**Categoría:** Fraude financiero
**Definición:** Uso no autorizado de tarjetas de crédito o débito de otra persona para comprar o pagar servicios. También nombra la técnica automatizada de probar listas enormes de tarjetas, robadas o generadas, contra el cobro de una tienda en línea para saber cuáles funcionan.
**En el curso:** es un fraude silencioso: la víctima suele enterarse por su estado de cuenta, a veces semanas después.

---

### CC
**Categoría:** Carding / Jerga
**Definición:** Siglas de *credit card*, tarjeta de crédito: un instrumento con el que el banco presta al titular para que pague ahora y devuelva después. En la jerga del carding, una "CC" es una tarjeta con todos sus datos (número, vencimiento, código y titular), lista para usarse o venderse.

---

### ChoiceJacking
**Categoría:** Juice jacking / Ataques
**Definición:** Familia de ataques presentada en 2025 por investigadores de la Universidad Tecnológica de Graz en el simposio USENIX Security. Un cargador malicioso consigue aprobar por su cuenta el aviso con el que el teléfono pide permiso para intercambiar datos.
**En el curso:** Google lo corrigió en el boletín de Android de noviembre de 2024 y Apple en iOS 18.4, de marzo de 2025; por eso actualizar el sistema es parte de la defensa.

---

### Coinhive
**Categoría:** Cryptojacking / Casos
**Definición:** Servicio de minería en JavaScript lanzado en septiembre de 2017, que se ofrecía para que un sitio ganara dinero sin anuncios molestos: 30 % de lo minado para el servicio y 70 % para el dueño del sitio. Los delincuentes lo inyectaron en sitios ajenos sin permiso y en la primera mitad de 2018 fue la amenaza que más detectaban los antivirus. Cerró el 8 de marzo de 2019.

---

### Criptomoneda
**Categoría:** Criptomonedas
**Definición:** Dinero digital que existe solo en línea, sin forma física, y que se apoya en criptografía para crear monedas nuevas y validar cada transferencia. No la respalda ningún gobierno ni banco: sus operaciones quedan en un libro contable público copiado en miles de equipos. Viene de *cripto*, que en griego significa oculto, y de *moneda*.
**En el curso:** las criptomonedas como bitcoin no son anónimas sino seudónimas; las direcciones no llevan nombre, pero cada operación se puede rastrear.

---

### Cryptojacking
**Categoría:** Malware / Abuso de recursos
**Definición:** Minería maliciosa de criptomonedas: un programa oculto en una computadora, un teléfono, un servidor o un navegador usa su procesador y su electricidad para minar a favor de otro, sin que el dueño lo sepa ni lo autorice.
**En el curso:** sus señales son la lentitud, el calor, el consumo de luz y el desgaste del equipo.

---

### CVV o CVV2
**Categoría:** Tarjetas / Identificación
**Definición:** *Card Verification Value*, el código de verificación impreso en la tarjeta, casi siempre de tres dígitos. Sirve para comprobar que quien paga a distancia tiene la tarjeta a la vista, y un comercio nunca debe guardarlo.

---

## J

### Juice jacking
**Categoría:** Ataques a dispositivos móviles
**Definición:** Ataque que aprovecha un puerto o un cable USB de carga público, alterado para copiar datos del dispositivo conectado, instalarle malware o ambas cosas. Funciona porque los teléfonos usan el mismo conector para la energía y para los datos.
**En el curso:** el término se difundió en 2011 a raíz de un quiosco de demostración en DEF CON 19.

---

## M

### Minería
**Categoría:** Criptomonedas
**Definición:** Trabajo de resolver un acertijo matemático muy costoso para confirmar un bloque de transacciones y recibir monedas nuevas como premio. En Bitcoin se cierra un bloque aproximadamente cada diez minutos. La electricidad y el equipo son sus mayores costos, y eso es lo que el cryptojacking traslada a la víctima.

---

### Minero
**Categoría:** Criptomonedas / Personas
**Definición:** Persona o equipo que hace minería. En el cryptojacking, "minero" también nombra al programa escondido que calcula en la máquina de la víctima y manda los resultados al atacante.

---

### Monero
**Categoría:** Criptomonedas
**Definición:** Criptomoneda orientada a la privacidad que se mina bien con procesadores comunes. Por eso es la preferida del cryptojacking: permite repartir el trabajo entre miles de equipos ajenos.

---

## N

### NIP
**Categoría:** Tarjetas / Identificación
**Definición:** Número de identificación personal: la clave numérica con la que el titular autoriza operaciones en cajeros y terminales.
**En el curso:** los skimmers suelen acompañarse de una cámara escondida para capturarlo.

---

## S

### Skimmer
**Categoría:** Carding / Dispositivos
**Definición:** Lector oculto que se instala en un cajero o una terminal de pago para copiar los datos de la banda magnética de las tarjetas que pasan por él.

---

### SOCKS
**Categoría:** Redes / Protocolos
**Definición:** Protocolo que manda el tráfico de un equipo a través de un servidor intermediario, de modo que el destino ve la dirección IP del intermediario y no la del origen.
**En el curso:** los sistemas antifraude revisan la reputación de la IP y si coincide con el domicilio del titular, justo para detectar este tipo de intermediarios.
