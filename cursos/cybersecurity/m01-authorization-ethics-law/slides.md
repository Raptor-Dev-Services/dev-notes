<!-- _class: lead -->

# Módulo 1 - Autorización, Ética y Marco Legal

### Ciberseguridad

**Rogelio Arriaga - Raptor Dev Services**

---

## Por qué este módulo va primero

Todo lo que viene después en este curso (Nmap, Sherlock, Hydra) son herramientas de doble uso: la misma técnica defiende o ataca según quién la use y con qué permiso.

**Regla del curso:** ninguna técnica ofensiva se practica fuera de un laboratorio propio o con autorización escrita.

---

<!-- _class: lead -->

# Parte 1
## Autorización por escrito

---

## Qué es un pentest autorizado

Una prueba de penetración autorizada es un ataque simulado, acordado por escrito con el dueño del sistema, con alcance, fechas y reglas de enganche definidas antes de empezar.

Sin ese documento, exactamente la misma técnica deja de ser una prueba de seguridad y se convierte en un delito.

---

## Qué debe tener una autorización mínima

- Sistemas y rangos de IP exactos incluidos (y excluidos).
- Ventana de tiempo en la que se permite probar.
- Técnicas permitidas y prohibidas (¿se permite fuerza bruta? ¿denegación de servicio?).
- A quién contactar si algo sale mal.
- Firma de quien tiene autoridad real sobre esos sistemas.

---

## En este curso: laboratorio propio

Todas las prácticas de los módulos 3 a 6 se ejecutan dentro de tu propio laboratorio (máquinas virtuales que tú controlas), nunca contra un sistema de un tercero, con o sin su "permiso verbal".

> Un permiso verbal no es una autorización. Si no está firmado, no existe para efectos legales.

---

<!-- _class: lead -->

# Parte 2
## Marco legal mexicano (resumen didáctico)

---

## El Código Penal Federal no tiene excepción para "solo estaba probando"

El capítulo de delitos informáticos del Código Penal Federal (arts. 211 bis 1 a 211 bis 7) castiga conocer, copiar, modificar o destruir información en sistemas protegidos sin autorización. No existe una excepción para quien dice que "solo estaba probando".

Fuente: Código Penal Federal, arts. 211 bis 1 a 211 bis 7, texto vigente, diputados.gob.mx/LeyesBiblio/pdf/CPF.pdf.

---

## La pena varía según de quién es el sistema

| De quién es el sistema | Sin permiso: conocer o copiar | Sin permiso: modificar o destruir |
|---|---|---|
| De un particular | 3 meses a 1 año | 6 meses a 2 años |
| Del sistema financiero | 3 meses a 2 años | 6 meses a 4 años |
| Del Estado | 6 meses a 2 años | 1 a 4 años |

La tabla solo resume prisión; también hay sanciones económicas (días multa). No es asesoría legal.

Fuente: Código Penal Federal, arts. 211 bis 1 a 211 bis 5.

---

## No hay una sola "ley de ciberdelitos" en México

Distintas conductas caen en leyes distintas, y los estados tienen su propio código penal. Por ejemplo, el Código Penal para el Estado de Tamaulipas (art. 207 Quáter) castiga usar o modificar datos protegidos sin autorización con 2 a 5 años de prisión: más que el equivalente federal.

Fuentes: Código Penal para el Estado de Tamaulipas, congresotamaulipas.gob.mx; LFPDPPP, arts. 62 y 64, diputados.gob.mx/LeyesBiblio/pdf/LFPDPPP.pdf.

---

## El ransomware como extorsión

Exigir un pago por devolver el acceso a sistemas o datos cifrados se puede encuadrar en la Ley General en materia de Extorsión (arts. 15 y 17), con penas de 15 a 25 años, que aumentan de 5 a 12 años más si se cometió por medios electrónicos. Es una lectura de este curso: el delito y el fuero los define la autoridad.

Fuente: Ley General en materia de Extorsión, arts. 15 y 17, DOF 28-11-2025.

---

<!-- _class: lead -->

# Parte 3
## Ética profesional del hacker ético

---

## El código de ética en una frase

Tienes autorización por escrito, respetas el alcance acordado, reportas lo que encuentras de forma responsable, y nunca usas lo que descubriste en beneficio propio o ajeno.

Usar la información obtenida en provecho propio agrava la pena hasta en una mitad (Código Penal Federal, art. 211 bis 7).

---

## Divulgación responsable (responsible disclosure)

Si encuentras una vulnerabilidad real, el camino ético es reportarla de forma privada al dueño del sistema, darle tiempo razonable para corregirla, y solo después (si corresponde) hacerla pública.

Publicar una vulnerabilidad sin avisar antes al afectado expone a terceros a un riesgo real, aunque tu intención haya sido buena.

---

## Ejercicio: identifica la autorización

Para cada escenario que te presente el instructor, decide: ¿hay una autorización válida? ¿Qué le falta para serlo? Trabaja en equipo y justifica tu respuesta con un artículo o una condición concreta de las vistas en este módulo.

---

<!-- _class: lead -->

# Cierre

## Lo que te llevas de este módulo

- Ninguna técnica se practica sin laboratorio propio o autorización escrita
- El marco legal mexicano no perdona "solo estaba probando"
- La ética profesional incluye reportar, nunca explotar para beneficio propio

**Siguiente módulo:** Historia y tipos de hackers.
