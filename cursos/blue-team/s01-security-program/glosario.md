# Glosario - S01 - Un programa de seguridad que se pueda sostener

> Definiciones de los términos técnicos de la sesión 1.
> Ordenado alfabéticamente.

---

## A

### Aceptación del riesgo
**Categoría:** Gestión de riesgos
**Definición:** Decisión documentada, con dueño y fecha, de no remediar un riesgo por ahora porque su costo o su impacto lo justifican. Sin documento, no es aceptación: es olvido.
**En el curso:** La excepción de la práctica (paso 8) y el anexo de la tarea.

---

### Activo
**Categoría:** Inventario
**Definición:** Todo lo que tiene valor para la organización y puede perderse, filtrarse o dejar de funcionar: equipos, aplicaciones, datos, cuentas, proveedores y personas con acceso.
**En el curso:** Los 12 activos de la tienda de ejemplo.

---

### Alcance
**Categoría:** Políticas
**Definición:** Apartado de una política que dice a qué activos, personas y situaciones aplica, y a cuáles no.
**En el curso:** Sección 2 de la política de parches.

---

## C

### CIS Controls
**Categoría:** Marcos
**Definición:** Lista priorizada de controles de seguridad del Center for Internet Security. La versión 8.1 (24 de junio de 2024) tiene 18 controles.
**En el curso:** Se citan el 1 (inventario de activos), el 2 (software), el 3 (datos) y el 7 (vulnerabilidades continuas).

---

### Clasificación de datos
**Categoría:** Inventario
**Definición:** Asignar a cada tipo de dato una clase (por ejemplo público, interno, confidencial, restringido) que determina cómo se protege. La escala la define cada organización.
**En el curso:** Columna `clase_datos` del inventario.

---

### Control compensatorio
**Categoría:** Gestión de riesgos
**Definición:** Medida que reduce el riesgo cuando el arreglo directo no se puede aplicar a tiempo, por ejemplo cerrar un puerto al exterior mientras llega el parche.
**En el curso:** Obligatorio en el formato de excepción.

---

### Criticidad
**Categoría:** Inventario
**Definición:** Qué tanto depende el negocio de que el activo funcione, en una escala escrita (1 a 3 en el curso).
**En el curso:** Primer factor del puntaje.

---

### CSF (Cybersecurity Framework)
**Categoría:** Marcos
**Definición:** Marco del NIST que organiza resultados de ciberseguridad en funciones y categorías. La versión 2.0 se publicó el 26 de febrero de 2024 y tiene seis funciones: Govern, Identify, Protect, Detect, Respond y Recover.
**En el curso:** Marco de referencia de todo el curso.

---

### CVE
**Categoría:** Vulnerabilidades
**Definición:** Identificador público de una vulnerabilidad conocida, con la forma `CVE-año-número`. Solo la nombra: no dice cuán grave es ni si alguien la explota.
**En el curso:** CVE-2024-6387 del caso real.

---

### CVSS
**Categoría:** Vulnerabilidades
**Definición:** Sistema de FIRST para calificar la severidad técnica de una vulnerabilidad, de 0.0 a 10.0. La versión 4.0 se publicó el 1 de noviembre de 2023 y tiene los grupos Base, Threat, Environmental y Supplemental.
**En el curso:** Primera de las tres señales de priorización.

---

## D

### Descubrimiento
**Categoría:** Inventario
**Definición:** Obtener de los sistemas mismos (puertos, paquetes, cuentas) lo que realmente hay, en lugar de lo que el equipo cree que hay.
**En el curso:** `ss -tulpn` y `dpkg-query` en `victima`.

---

### Dueño (de un activo)
**Categoría:** Inventario
**Definición:** Persona con nombre que responde por un activo: decide sobre él y contesta cuando falla. Un área no es un dueño.
**En el curso:** Columna `dueno`; la fila `sin dueno` es un hallazgo.

---

## E

### EPSS
**Categoría:** Vulnerabilidades
**Definición:** Exploit Prediction Scoring System, de FIRST: estimación, de 0 a 1 y actualizada a diario, de la probabilidad de que una CVE sea explotada en la naturaleza en los próximos 30 días. No mide el daño.
**En el curso:** Segunda señal de priorización.

---

### Estándar
**Categoría:** Políticas
**Definición:** Requisito concreto y obligatorio que concreta una política, por ejemplo un plazo máximo de parchado.
**En el curso:** Tabla de plazos de la clase.

---

### Excepción
**Categoría:** Políticas
**Definición:** Autorización documentada, con control compensatorio y fecha de vencimiento, para no cumplir una regla por un tiempo.
**En el curso:** Paso 8 de la práctica.

---

### Exposición
**Categoría:** Inventario
**Definición:** Qué tan alcanzable es un activo para quien ataca: internet, red interna o solo local.
**En el curso:** Segundo factor del puntaje.

---

## G

### Gobernanza (Govern)
**Categoría:** Marcos
**Definición:** Función de CSF 2.0 que cubre el contexto organizacional, la estrategia de riesgo, los roles, la política y la supervisión.
**En el curso:** Donde viven las políticas del curso.

---

### Guía
**Categoría:** Políticas
**Definición:** Recomendación para cuando hay más de una opción válida; no es obligatoria.
**En el curso:** Una de las cuatro capas de documentos.

---

## I

### Inventario de activos
**Categoría:** Inventario
**Definición:** Lista mantenida de los activos con su dueño, sus datos, su criticidad y su exposición. Se actualiza con cada alta y baja.
**En el curso:** `inventario.csv`.

---

## K

### KEV
**Categoría:** Vulnerabilidades
**Definición:** Known Exploited Vulnerabilities: catálogo de CISA de vulnerabilidades explotadas activamente. Cada entrada trae fecha de alta y fecha límite para las agencias federales de EE. UU. No es exhaustivo.
**En el curso:** Versión 2026.11.10 con 1779 entradas.

---

## M

### Mantenimiento mensual
**Categoría:** Parchado
**Definición:** Ventana periódica y acordada para instalar lo que no es urgente.
**En el curso:** Última fila de la tabla de plazos.

---

## N

### NVD
**Categoría:** Vulnerabilidades
**Definición:** National Vulnerability Database: base de datos de vulnerabilidades del NIST que publica cada CVE con su descripción, su puntaje y las versiones afectadas.
**En el curso:** Fuente del CVSS del caso real.

---

## P

### Parche
**Categoría:** Parchado
**Definición:** Actualización de software que corrige una falla. Un parche no está aplicado del todo si el proceso afectado sigue corriendo con el código viejo.
**En el curso:** Verificación de paquete y proceso.

---

### Política
**Categoría:** Políticas
**Definición:** Documento de alto nivel que fija qué se busca y por qué, y quién manda. Cambia poco.
**En el curso:** Capa más alta de la regla.

---

### Procedimiento
**Categoría:** Políticas
**Definición:** Pasos concretos para ejecutar una tarea. Cambia con las herramientas.
**En el curso:** `apt update`, `apt upgrade`, reiniciar, comprobar.

---

### Puntaje de prioridad
**Categoría:** Inventario
**Definición:** Número que resulta de multiplicar criticidad por exposición. Es un método cualitativo propio, no un estándar.
**En el curso:** Salida de `priorizar.sh`.

---

## R

### Remediación
**Categoría:** Vulnerabilidades
**Definición:** Acción que elimina o reduce una vulnerabilidad: parchar, cambiar la configuración, quitar la exposición o aceptar el riesgo por escrito.
**En el curso:** Tercer paso del ciclo.

---

## S

### Superficie de ataque
**Categoría:** Inventario
**Definición:** Conjunto de puntos por los que un atacante puede intentar entrar: puertos abiertos, cuentas, aplicaciones y proveedores.
**En el curso:** `ss -tulpn`.

---

## T

### Tiempo medio de remediación
**Categoría:** Métricas
**Definición:** Promedio de días entre que una vulnerabilidad se conoce en la organización y se cierra, por severidad.
**En el curso:** Métrica de la política.

---

## U

### unattended-upgrades
**Categoría:** Parchado
**Definición:** Paquete de Ubuntu que instala solas las actualizaciones de los orígenes que su configuración permita. Se configura en `/etc/apt/apt.conf.d/20auto-upgrades` y `50unattended-upgrades`.
**En el curso:** Paso 7 de la práctica.

---

## V

### Verificación
**Categoría:** Vulnerabilidades
**Definición:** Comprobar con una medición que un arreglo funcionó: por ejemplo, la versión del paquete y la fecha de arranque del proceso.
**En el curso:** Cuarto paso del ciclo.

---

### Vulnerabilidad
**Categoría:** Vulnerabilidades
**Definición:** Debilidad que puede aprovecharse. Gestionarla es un ciclo continuo: descubrir, priorizar, remediar, verificar y reportar.
**En el curso:** Parte 4 de la clase.

---
