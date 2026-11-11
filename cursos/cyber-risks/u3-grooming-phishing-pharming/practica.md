# Práctica - U3 - Ocho consejos de ciberprevención

**Riesgos actuales en ciberseguridad**
**Tiempo estimado:** 2 horas (30 minutos de ejercicio con el mensaje, 45 de tus consejos y 45 de contraste con otras posturas).

> **Estudio individual.** Esta actividad se resuelve por tu cuenta: guarda tu trabajo y evalúalo tú mismo con la rúbrica o el checklist del final. No hay foro ni entrega en la plataforma. Si llevas el curso en grupo, el instructor te indica dónde se entrega.

---

## Objetivo

Identificar consejos de ciberprevención que de verdad funcionen contra el grooming, el phishing y el pharming, y saber explicar **contra qué protege cada uno y por qué**. Un consejo sin mecanismo ("usa antivirus y ya") no previene nada: lo que se ejercita aquí es pasar de la receta a la razón.

---

## Antes de empezar

- Lee las tres notas de la unidad: [07 - Grooming](../../../12-seguridad/02-riesgos/07-grooming.md), [08 - Phishing](../../../12-seguridad/02-riesgos/08-phishing.md) y [09 - Pharming](../../../12-seguridad/02-riesgos/09-pharming.md).
- Ten a la mano al menos dos fuentes confiables para citar en APA 7: las mismas notas traen guías de la UNODC, el FBI y la CISA.
- Una regla de convivencia para este tema: **no compartas casos reales de menores identificables**, ni propios ni ajenos. Si algo de lo que se trata te toca de cerca, puedes hablarlo con un adulto de confianza.

---

## El caso: una familia, tres pantallas

Una familia que vive en Puebla. Nada de lo que sigue es extraordinario; justo por eso sirve.

1. **La hija, de 12 años,** juega todas las tardes un videojuego en línea. Desde hace tres semanas, un jugador que dice tener 14 le regala artículos del juego, le dice que es la única que lo entiende y ahora le propone pasar la plática a una app de mensajería "donde tus papás no nos lean".
2. **La abuela** recibe en su celular el mensaje de la sección siguiente. Tiene un paquete en camino de verdad, porque su nieta le compró un regalo en línea.
3. **El papá** lleva la contabilidad de la ferretería familiar. El técnico que instaló el internet dejó el router con su contraseña de fábrica. Un día, al teclear a mano la dirección del banco, el navegador le muestra una advertencia de certificado. Él da clic en "continuar", porque está seguro de haber escrito bien la dirección.

Preguntas guía:

- ¿Qué ataque hay en cada situación y en qué etapa o variante va?
- ¿Qué señal pudo haber prendido la alarma en cada caso, y quién estaba en posición de verla?
- En la situación 3, ¿por qué escribir bien la dirección no lo protegió? ¿En qué eslabón (equipo, router, DNS, certificado) se rompió la cadena?
- En la situación 1, ¿qué tendría que pasar en casa para que la niña se lo contara a alguien sin miedo a que le quiten el juego?

---

## Ejercicio: desmontar un mensaje

Tienes 10 minutos para encontrar **todas** las señales de engaño en este mensaje ficticio. El dominio `.example` está reservado para ejemplos y no lleva a ningún lado.

```text
De: +52 55 0000 0000
AVISO DE ENVÍO: Tu paquete MX-48213 está retenido por un saldo pendiente
de $19.90 MXN. Si no se cubre hoy antes de las 23:59 será devuelto al
remitente. Regulariza tu entrega aquí:
https://entregas-paqueteria-mx.example/pago?guia=48213
```

Para revisar lo que encontraste:

- ¿Qué emoción busca y con qué palabras? ¿Por qué el monto es tan pequeño?
- ¿Qué variante de phishing es? ¿Cambiaría si llegara por una app de mensajería o como código QR?
- ¿El dominio pertenece a alguna paquetería? ¿Cómo lo comprobarías **sin** abrir el enlace?
- Si la página tuviera candado HTTPS, ¿eso cambia algo? ¿Por qué?
- ¿Qué tiene que hacer la abuela, paso por paso, si ya capturó los datos de su tarjeta?

---

## Momento 1 - Ocho consejos, ocho razones

Escribe **ocho consejos de prevención digital** con estas condiciones:

- Al menos **dos por cada ataque** (grooming, phishing y pharming); los otros dos, libres.
- Al menos uno responde directamente a cada situación del caso.
- Cada consejo lleva cuatro partes:

| Parte | Qué escribes |
|---|---|
| Consejo | Una frase en imperativo, concreta y verificable |
| Contra qué protege | El ataque y, si aplica, la variante o la etapa |
| Por qué funciona | El mecanismo, en una o dos frases |
| Para quién es | Menor, familia, escuela, usuario o empresa |

La diferencia entre un consejo débil y uno sólido:

- **Débil:** "Usa antivirus para estar protegido." No dice contra qué ni cómo.
- **Sólido:** "Cambia la contraseña de fábrica del router y actualiza su firmware. Protege contra el drive-by pharming: ese ataque entra al router con la contraseña que nadie cambió y le pone un DNS del atacante, que desvía a toda la casa. Para cualquier hogar o pyme."

Extensión: entre 400 y 600 palabras, con las referencias al final en APA 7.

---

## Momento 2 - Contraste con otras posturas

Busca en las notas de la unidad o en las guías que citan **dos posturas distintas a la tuya** sobre cómo prevenir estos ataques (por ejemplo, un consejo que las guías priorizan y tú no, o uno que tú diste y ellas matizan). Para cada una, escribe de 80 a 150 palabras que digan si estás de acuerdo o no y lo **justifiquen con lo visto en la unidad**, aportando un dato nuevo. Elige al menos dos de estos movimientos:

- **Revisar el mecanismo:** ¿el consejo protege de verdad contra lo que dice? Hay errores frecuentes que vale la pena señalar: "el candado garantiza que el sitio es legítimo", o "escribir la dirección a mano te protege del pharming".
- **Precisar el término:** si la postura confunde grooming con cyberbullying, o smishing con vishing, propón el término correcto y explica la diferencia.
- **Mejorar el consejo:** hazlo más concreto o agrégale para quién es.
- **Comparar:** di en qué se parece y en qué difiere de un consejo tuyo, y cuál te parece más eficaz y por qué.

"Estoy de acuerdo, es una buena postura" no cuenta como contraste.

---

## Lo que debes tener al terminar

1. Las señales que encontraste en el mensaje ficticio, con la respuesta a las preguntas de revisión.
2. Tu aportación del Momento 1: ocho consejos con sus cuatro partes y las referencias.
3. Tus dos contrastes del Momento 2.

---

## Cómo te autoevalúas

| Criterio | Qué se busca | Puntos |
|---|---|---|
| Cobertura | Ocho consejos, al menos dos por ataque, y al menos uno por situación del caso | 1.5 |
| Justificación | Cada consejo dice contra qué protege y por qué funciona, sin errores de concepto | 2 |
| Análisis del mensaje | Identificas la variante, las señales de engaño y los pasos si ya se cayó | 1 |
| Contraste con otras posturas | Dos contrastes con postura y argumento de la unidad, no solo acuerdo | 2 |
| Fuentes y redacción | Al menos dos fuentes en APA 7, ortografía cuidada, tono respetuoso | 1 |
| **Total** | | **7.5** |

Suma tus puntos. Con menos de 5.25 (70 %), vuelve a leer la nota indicada y rehaz el apartado más débil.
