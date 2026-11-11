# Glosario - S02 - Bitácoras y detección

> Definiciones de los términos técnicos de la sesión 2.
> Ordenado alfabéticamente.

---

## A

### Agente
**Categoría:** SIEM
**Definición:** Programa que lee bitácoras (archivos, journal) en un equipo y las envía al SIEM. Wazuh y Elastic Agent son ejemplos.
**En el curso:** Wazuh agent en `victima`.

---

### Agrupación
**Categoría:** Reglas
**Definición:** Campo por el que una regla cuenta: por IP de origen, por usuario, por activo.
**En el curso:** Las reglas de la sesión cuentan por IP.

---

### Alerta accionable
**Categoría:** Alertas
**Definición:** Alerta que dice qué pasó, sobre qué activo, con qué evidencia y qué hacer primero. Si no, es ruido.
**En el curso:** Cuarta parte de la clase.

---

### auth.log
**Categoría:** Bitácoras
**Definición:** Archivo donde rsyslog escribe los mensajes de autenticación en Ubuntu (`sshd`, `sudo`, PAM, cron). El formato de fecha depende de la plantilla de rsyslog.
**En el curso:** Fuente principal de la regla de fuerza bruta.

---

## B

### Bitácora (log)
**Categoría:** Bitácoras
**Definición:** Registro ordenado de los eventos de un sistema: quién hizo qué, cuándo y desde dónde.
**En el curso:** Todo el tema de la sesión.

---

## C

### Centralizar
**Categoría:** SIEM
**Definición:** Enviar las bitácoras casi en tiempo real a otro sistema que las almacena, las indexa y evalúa reglas. Una copia ya enviada es evidencia; la local, no tanto.
**En el curso:** Parte 2.

---

### combined (formato)
**Categoría:** Nginx
**Definición:** Formato de registro de acceso predefinido en nginx: `$remote_addr - $remote_user [$time_local] "$request" $status $body_bytes_sent "$http_referer" "$http_user_agent"`.
**En el curso:** `access.log` de `victima`.

---

### Correlación (Sigma)
**Categoría:** Sigma
**Definición:** Regla que agrega sobre otras reglas: cuenta eventos (`event_count`), valores distintos (`value_count`) o secuencias (`temporal`) dentro de un `timespan`.
**En el curso:** Regla de fuerza bruta en formato Sigma.

---

## D

### Decodificador
**Categoría:** SIEM
**Definición:** Componente que extrae campos (IP, usuario, puerto) de una línea de bitácora para que las reglas puedan usarlos.
**En el curso:** `wazuh-logtest` muestra la fase de decodificación.

---

### DE.AE y DE.CM
**Categoría:** Marcos
**Definición:** Categorías de la función Detect de NIST CSF 2.0: análisis de eventos adversos y monitoreo continuo.
**En el curso:** Dónde encaja la sesión en el marco.

---

## E

### Enumeración de usuarios
**Categoría:** Ataques
**Definición:** Probar nombres de usuario para descubrir cuáles existen. `sshd` lo registra con una línea `Invalid user`.
**En el curso:** Señal gratis de la clase.

---

## F

### Falso negativo
**Categoría:** Detección
**Definición:** Ataque real que no genera alerta. Es el más caro: nadie lo sabe.
**En el curso:** Medido en la práctica con la ventana fija.

---

### Falso positivo
**Categoría:** Detección
**Definición:** Alerta sin ataque real. Su costo es la fatiga de alertas.
**En el curso:** El administrador que se equivoca dos veces.

---

### Fatiga de alertas
**Categoría:** Alertas
**Definición:** Pérdida de atención de quien recibe demasiadas alertas, la mayoría falsas.
**En el curso:** Motivo para medir los falsos positivos.

---

### Fuerza bruta
**Categoría:** Ataques
**Definición:** Probar muchas contraseñas contra una cuenta. Técnica T1110 de MITRE ATT&CK, subtécnica T1110.001 (Password Guessing).
**En el curso:** El ataque de Hydra del módulo 6.

---

## J

### journal / journalctl
**Categoría:** Bitácoras
**Definición:** Registro binario de systemd y su herramienta de consulta. Con `-o short-iso` las fechas salen en ISO 8601.
**En el curso:** Alternativa a `auth.log`.

---

## L

### local_rules.xml
**Categoría:** Wazuh
**Definición:** Archivo del manager de Wazuh para las reglas propias, en `/var/ossec/etc/rules/`. Los identificadores propios van de 100000 a 120000.
**En el curso:** Regla 100100.

---

## M

### MaxAuthTries
**Categoría:** sshd
**Definición:** Opción de `sshd_config` (por omisión 6): intentos de autenticación por conexión. A la mitad, los fallos adicionales se registran.
**En el curso:** Parte 1.

---

### Mutación (de una prueba)
**Categoría:** Pruebas
**Definición:** Estropear a propósito una regla para comprobar que su prueba cae. Si no cae, la prueba no mide nada.
**En el curso:** Paso 6 de la práctica.

---

## P

### Password spraying
**Categoría:** Ataques
**Definición:** Probar una contraseña contra muchas cuentas. Subtécnica T1110.003. Se distingue de la fuerza bruta por contar usuarios distintos.
**En el curso:** Regla de usuarios distintos por IP.

---

## R

### Regla de detección
**Categoría:** Reglas
**Definición:** Condición sobre eventos que, al cumplirse, genera una alerta. Tiene fuente, condición, agrupación, umbral, ventana, severidad y acción.
**En el curso:** Parte 3.

---

### Runbook
**Categoría:** Alertas
**Definición:** Guía corta con los primeros pasos para atender un tipo de alerta.
**En el curso:** Runbook de la fuerza bruta.

---

## S

### Sigma
**Categoría:** Reglas
**Definición:** Formato abierto en YAML para describir detecciones independientes del SIEM. Obligatorios: `title`, `logsource` y `detection`.
**En el curso:** Fragmento de la parte 3.

---

### SIEM
**Categoría:** SIEM
**Definición:** Sistema que reúne bitácoras de muchas fuentes, las almacena, correlaciona y genera alertas.
**En el curso:** Wazuh en el laboratorio.

---

### sshd-session
**Categoría:** sshd
**Definición:** Binario que atiende cada sesión desde OpenSSH 9.8 (1 de julio de 2024); el `sshd` solo escucha. Cambia el nombre del proceso en las líneas de bitácora.
**En el curso:** Verlo fallar: la regla que busca solo `sshd[`.

---

## T

### Threshold (regla)
**Categoría:** Elastic
**Definición:** Tipo de regla de Kibana que dispara cuando se acumulan N coincidencias, agrupadas por un campo.
**En el curso:** Equivalente en Elastic de la regla de la clase.

---

## U

### Umbral
**Categoría:** Reglas
**Definición:** Número de coincidencias necesarias para disparar una regla. No hay uno universal: se elige midiendo.
**En el curso:** Parte 3.

---

## V

### Ventana deslizante
**Categoría:** Reglas
**Definición:** Ventana que se mueve con cada evento (por ejemplo, los últimos 60 segundos), sin cortes fijos. No pierde ataques que cruzan un borde de minuto.
**En el curso:** `detecta-ssh-ventana.py`.

---

### Ventana fija
**Categoría:** Reglas
**Definición:** Ventana que corta el tiempo en cubetas (por minuto, por hora). Un ataque que cruza el borde queda repartido y puede quedar bajo el umbral.
**En el curso:** `detecta-ssh.sh`.

---

## W

### wazuh-logtest
**Categoría:** Wazuh
**Definición:** Herramienta del manager (`/var/ossec/bin/wazuh-logtest`) que recibe una línea de registro y muestra sus fases de pre-decodificación, decodificación y reglas.
**En el curso:** Paso 10 de la práctica.

---
