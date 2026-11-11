# Práctica - M2 - Medir dos definiciones de equidad y verlas chocar

**IA, ética y criterio profesional**
**Tiempo estimado:** 75 minutos, individual o en parejas. Basta una calculadora o una hoja de cálculo.

> **Estudio individual.** Esta actividad se resuelve por tu cuenta: guarda tu trabajo y evalúalo tú mismo con la rúbrica del final. No hay foro ni entrega en la plataforma. Si llevas el curso en grupo, quien imparte te indica cómo se entrega.

---

## Objetivo

Con un conjunto **ficticio** de solicitudes de crédito, calcular dos (y luego cuatro) definiciones de equidad, comprobar que no pueden cumplirse a la vez, y escribir una decisión: qué medida priorizarías y a quién deja fuera.

---

## Aviso: todo es ficticio

**Caso hipotético.** La financiera, los grupos A y B y todas las cifras fueron inventados para esta práctica. Los números se eligieron para que las cuentas sean sencillas y se verificaron con un programa antes de publicarlas. **No representan a ningún grupo de personas real** ni a ninguna empresa.

En la vida real hay un obstáculo que aquí se deja de lado: de quien se rechaza un crédito normalmente no se observa si habría pagado. En el ejercicio se supone que sí lo sabemos, solo para poder enseñar el cálculo.

---

## Los datos

Cada grupo tiene 100 solicitudes. "Pagaría" quiere decir que, de haberse aprobado, habría pagado.

| | Grupo A | Grupo B |
|---|---|---|
| Pagarían y fueron aprobados (aciertos a favor) | 48 | 24 |
| Pagarían y fueron rechazados | 12 | 6 |
| No pagarían y fueron aprobados | 16 | 8 |
| No pagarían y fueron rechazados | 24 | 62 |

Comprueba antes de seguir: A suma 100 y B suma 100.

---

## Paso 1 - Paridad demográfica

Calcula, para cada grupo, la **tasa de aprobación**: aprobados entre solicitantes.

| Grupo | Aprobados | Solicitantes | Tasa |
|---|---|---|---|
| A | | | |
| B | | | |

Responde: ¿el sistema reparte el crédito en la misma proporción entre los grupos? Anota la diferencia en puntos porcentuales.

---

## Paso 2 - Igualdad de oportunidad

Calcula, para cada grupo, la proporción de **quienes sí pagarían** que fueron aprobados.

| Grupo | Pagarían y aprobados | Pagarían (total) | Proporción |
|---|---|---|---|
| A | | | |
| B | | | |

Responde: ¿quien merece el crédito tiene la misma probabilidad de recibirlo en los dos grupos?

---

## Paso 3 - Las otras dos medidas

Calcula con el mismo cuidado:

| Medida | Cómo | Grupo A | Grupo B |
|---|---|---|---|
| Falsos positivos | No pagarían y aprobados / No pagarían (total) | | |
| Paridad predictiva | Pagarían y aprobados / Todos los aprobados | | |

Llena la tabla resumen:

| Medida | A | B | ¿Se cumple? |
|---|---|---|---|
| Tasa de aprobación | | | |
| Igualdad de oportunidad | | | |
| Falsos positivos | | | |
| Paridad predictiva | | | |

Resultado esperado: dos medidas coinciden y dos no. Si tus resultados son distintos, revisa las sumas.

---

## Paso 4 - Por qué no se pueden cumplir a la vez

Observa las bases: en A pagarían 60 de 100; en B, 30 de 100. Con esas bases distintas:

1. Si se quiere la **misma** igualdad de oportunidad (80 % en los dos) y la **misma** paridad predictiva (75 % en los dos), ¿cuánto sale de falsos positivos en cada grupo?
2. Comprueba esta relación: falsos positivos = base / (1 - base) x (1 - paridad predictiva) / paridad predictiva x igualdad de oportunidad. Con la base en forma decimal (0.6 y 0.3).
3. Escribe con tus palabras por qué, con bases distintas, igualar dos medidas deja la tercera desigual.

Pista: es una relación aritmética, no un defecto del programa. Si las bases fueran iguales, ¿qué pasaría con el resultado del punto 1?

---

## Paso 5 - Verlo fallar: forzar la paridad demográfica

Ahora intenta "arreglar" el problema más visible: que la tasa de aprobación de B sea igual a la de A.

El sistema ficticio se reconfigura y en B pasan a aprobarse 64 de 100. Con esa configuración, la tabla de B queda así:

| Grupo B (reconfigurado) | Personas |
|---|---|
| Pagarían y aprobados | 27 |
| Pagarían y rechazados | 3 |
| No pagarían y aprobados | 37 |
| No pagarían y rechazados | 33 |

Calcula las cuatro medidas de B reconfigurado y compáralas con las de A y con las de B original.

| Medida de B | Original | Reconfigurado |
|---|---|---|
| Tasa de aprobación | | |
| Igualdad de oportunidad | | |
| Falsos positivos | | |
| Paridad predictiva | | |

Responde por escrito:

- ¿Qué medida mejoró?
- ¿Qué medidas empeoraron? ¿Cuántas veces creció la proporción de falsos positivos?
- Después del cambio, ¿"aprobado" significa lo mismo en A que en B? (Compara la paridad predictiva.)
- ¿Quién carga con el error nuevo: la financiera, el solicitante o los dos?

Lo que debes observar: cada ajuste cumple una definición y rompe otra. Valores esperados: tasa 64 %, igualdad de oportunidad 90 %, falsos positivos 52.9 %, paridad predictiva 42.2 %.

---

## Paso 6 - Elegir y decirlo

Escribe un párrafo de no más de diez líneas con esta estructura:

1. **La medida que priorizarías** para un sistema de crédito, y por qué.
2. **A quién deja fuera** esa decisión o a quién le pesa su error.
3. **Qué otra medida sacrificas** y cómo lo compensarías (por ejemplo, con revisión humana de los rechazos o con un canal para impugnar).
4. **Cómo se lo dirías** a una persona rechazada.

No hay una respuesta única correcta. Hay respuestas que **ven** lo que sacrifican y respuestas que no.

---

## Rúbrica de autoevaluación

| Criterio | Qué se busca | Puntos |
|---|---|---|
| Cálculos | Las cuatro medidas de A y B son correctas | 3 |
| Choque entre medidas | Muestras con números que no se cumplen a la vez y lo explicas con la relación del paso 4 | 2 |
| Verlo fallar | Calculas B reconfigurado y nombras lo que mejoró y lo que empeoró | 2 |
| Decisión | Priorizas una medida, nombras a quién deja fuera y qué sacrificas | 2.5 |
| Claridad | Sin jerga innecesaria; rotulas lo ficticio como ficticio | 0.5 |
| **Total** | | **10** |

Si sacas menos de 7 puntos, rehaz los pasos 1 a 3 con una hoja de cálculo y verifica que cada grupo suma 100.

---

## Checklist

- [ ] Comprobé que cada grupo suma 100 antes de calcular.
- [ ] Mis cuatro medidas por grupo están en una sola tabla.
- [ ] Expliqué por qué, con bases distintas, no se igualan todas.
- [ ] Completé el paso 5 y nombré al menos una medida que empeoró.
- [ ] Mi decisión del paso 6 dice a quién deja fuera.
- [ ] No usé datos de personas reales.
