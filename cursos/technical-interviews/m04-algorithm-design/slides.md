<!-- _class: lead -->

# Módulo 4 - Diseñar un algoritmo bajo presión

### Preparación para entrevistas técnicas

**Rogelio Arriaga - Raptor Dev Services**

---

## Hoy

> Quien entrevista califica cómo piensa, no solo si llega.

- **Parte 1:** el método en voz alta: aclarar, proponer, probar y mejorar
- **Parte 2:** divide y vencerás
- **Parte 3:** algoritmos voraces, y cómo saber si se puede confiar en uno
- **Parte 4:** programación dinámica
- **Parte 5:** la entrevista simulada de 45 minutos
- **Práctica:** entrevista simulada con rúbrica, grabada

---

## Lo que se califica

| Eje | Qué observa quien entrevista |
|-----|------------------------------|
| Comunicación | ¿Piensa en voz alta? ¿Aclara antes de escribir? ¿Se le entiende? |
| Resolución | ¿Parte de una solución correcta y la mejora? ¿Sabe su costo? |
| Código | ¿Es legible, con nombres claros, y compila en su cabeza? |
| Pruebas | ¿Recorre su código con un ejemplo? ¿Encuentra los casos borde solo? |

Llegar a la solución óptima en silencio califica bien en un eje. **Llegar a una buena solución hablando califica en los cuatro.**

---

<!-- _class: lead -->

# Parte 1
## El método en voz alta

---

## Cuatro pasos

```text
1. Aclarar    Repite el problema con tus palabras. Pregunta tamaños, casos borde y qué devolver.
              Escribe dos o tres ejemplos de entrada y salida.

2. Proponer   Di la fuerza bruta y su costo. Busca el trabajo que se repite.
              Propón la mejora y su costo ANTES de escribirla. Pide luz verde.

3. Probar     Escribe el código y recórrelo a mano con un ejemplo pequeño.
              Luego con los casos borde: vacío, uno, todos iguales, extremos.

4. Mejorar    Di qué cambiarías con más tiempo, con otros supuestos o con más datos.
```

---

## Aclarar: las preguntas que casi siempre aplican

- **Entrada:** ¿qué tipo?, ¿puede venir vacía o nula?, ¿hay repetidos?, ¿negativos?, ¿ya viene ordenada?
- **Tamaño:** ¿cuántos elementos, más o menos? Diez mil y diez millones piden soluciones distintas.
- **Salida:** ¿qué devuelvo si no hay respuesta?, ¿una o todas?, ¿importa el orden?
- **Restricciones:** ¿puedo modificar la entrada?, ¿hay límite de memoria?

Cada pregunta que haces **es un punto** en el eje de comunicación. Y a veces la respuesta cambia el algoritmo.

---

## Proponer: la fuerza bruta primero

1. **Dila aunque sea lenta**, con su costo: "probar todos los pares es O(n²)".
2. **Pregúntate qué se repite:** ¿busco lo mismo varias veces?, ¿recalculo algo que ya sabía?
3. **Prueba las herramientas en orden:** ¿ordenar primero?, ¿un diccionario?, ¿dos índices?, ¿una pila?, ¿un grafo?, ¿dividir el problema?
4. **Di la mejora y su costo antes de escribirla.** Quien entrevista puede decirte "adelante" o "¿y si fuera más grande?".

Nunca escribas la fuerza bruta completa si ya sabes que la vas a tirar; dila y sigue.

---

## Frases para pensar en voz alta

- "Déjame repetir el problema para ver si lo entendí."
- "Voy a probar con un ejemplo chico: tres elementos."
- "La solución obvia es O(n²) porque compara todos contra todos."
- "Lo que se repite es la búsqueda del complemento; si lo guardo en un diccionario..."
- "Antes de escribir, ¿te parece bien este enfoque?"
- "Aquí asumo que la entrada no viene vacía; lo reviso al final."
- "Voy a recorrer el código con el ejemplo para ver si hace lo que digo."

---

## Cuando te atoras

- **Un ejemplo más chico:** resuélvelo a mano y fíjate qué hiciste.
- **Un caso más simple:** sin repetidos, sin negativos, con todo ordenado. Luego generaliza.
- **Ordena primero** y mira qué cambia.
- **Cambia de estructura:** ¿y con una pila?, ¿con un diccionario?, ¿como grafo?
- **Piensa al revés:** desde la respuesta hacia la entrada.
- **Pide una pista.** No es reprobar: quedarse callado diez minutos sí lo es.

<!--
Nota del instructor: cuenta una entrevista en la que te hayas atorado y qué hiciste. Normaliza que atorarse es parte del ejercicio; lo que se califica es qué haces cuando pasa.
-->

---

<!-- _class: lead -->

# Parte 2
## Divide y vencerás

---

## Dividir, resolver, combinar

```csharp
// Merge sort: log n niveles de división y O(n) de trabajo por nivel: O(n log n) en tiempo, O(n) en espacio.
// Es estable: ante empate toma primero el de la izquierda.
public static int[] MergeSort(int[] datos)
{
    if (datos.Length <= 1) return (int[])datos.Clone();
    int medio = datos.Length / 2;
    int[] izq = MergeSort(datos[..medio]);
    int[] der = MergeSort(datos[medio..]);
    return Mezclar(izq, der);
}
```

```csharp
private static int[] Mezclar(int[] a, int[] b)
{
    var salida = new int[a.Length + b.Length];
    int i = 0, j = 0, k = 0;
    while (i < a.Length && j < b.Length)
        salida[k++] = a[i] <= b[j] ? a[i++] : b[j++];
    while (i < a.Length) salida[k++] = a[i++];
    while (j < b.Length) salida[k++] = b[j++];
    return salida;
}
```

---

## Por qué es n log n

```text
nivel 0:   [ 8 elementos ]                     8 de trabajo al mezclar
nivel 1:   [ 4 ]         [ 4 ]                 4 + 4 = 8
nivel 2:   [2]  [2]      [2]  [2]              2 + 2 + 2 + 2 = 8
nivel 3:   [1][1][1][1]  [1][1][1][1]          casos base

log2(8) = 3 niveles con trabajo, n por nivel:  n log n
```

Se escribe como T(n) = 2 T(n/2) + O(n). Para la entrevista basta con el dibujo: **cuántos niveles y cuánto trabajo por nivel.**

<!--
Nota del instructor: dibuja el árbol en el pizarrón con 8 elementos y pide al grupo que sume cada nivel. Luego pregunta qué pasaría si se partiera en tres en vez de en dos: siguen siendo log niveles (en base 3) con n de trabajo cada uno, así que el orden no cambia.
-->

---

## Otro ejemplo: potencia rápida

```csharp
// Potencia rápida: b^e mod m con O(log e) multiplicaciones. Pide b >= 0, e >= 0 y 1 <= m < 3e9,
// para que el producto de dos residuos quepa en long.
public static long PotenciaModular(long b, long e, long m)
{
    long resultado = 1 % m;
    b %= m;
    while (e > 0)
    {
        if ((e & 1) == 1) resultado = resultado * b % m;
        b = b * b % m;
        e >>= 1;
    }
    return resultado;
}
```

b^e = (b^(e/2))². Partir el exponente a la mitad en cada vuelta da O(log e) en vez de O(e). Las pruebas la comparan contra `BigInteger.ModPow`.

---

<!-- _class: lead -->

# Parte 3
## Algoritmos voraces

---

## Elegir lo mejor de ahora

"¿Cuántas reuniones, como máximo, caben en una sala sin traslaparse?"

```csharp
// Ordenar por hora de fin y tomar cada reunión que empiece cuando terminó la anterior.
// O(n log n) por el ordenamiento. La elección voraz es segura: terminar antes nunca estorba.
public static int MaximoDeReuniones(IEnumerable<Intervalo> reuniones)
{
    int cuenta = 0;
    int finUltima = int.MinValue;
    foreach (var r in reuniones.OrderBy(r => r.Fin))
    {
        if (r.Inicio >= finUltima)
        {
            cuenta++;
            finUltima = r.Fin;
        }
    }
    return cuenta;
}
```

**Por qué funciona:** si una solución óptima empieza con otra reunión, cambiarla por la que termina antes no quita espacio a ninguna de las siguientes. Ese argumento se llama **intercambio**.

---

## Un voraz que falla

```csharp
// Cambio voraz: siempre la moneda más grande que quepa. Rápido, pero NO siempre óptimo.
public static int CambioVoraz(int[] monedas, int monto)
{
    int cuenta = 0;
    foreach (int moneda in monedas.OrderByDescending(m => m))
    {
        cuenta += monto / moneda;
        monto %= moneda;
    }
    return monto == 0 ? cuenta : -1;
}
```

| Monedas | Monto | Voraz | Óptimo |
|---------|-------|-------|--------|
| 1, 2, 5, 10, 20 | cualquiera de 0 a 200 | igual al óptimo | (la prueba revisa los 201 montos) |
| 1, 3, 4 | 6 | 4 + 1 + 1: tres monedas | 3 + 3: dos monedas |

---

## ¿Puedo confiar en mi voraz?

1. **Busca un contraejemplo chico** antes de escribirlo: dos o tres elementos, valores raros.
2. **Busca el argumento de intercambio:** si la mejor solución no hace mi elección, ¿puedo cambiarla por la mía sin empeorar?
3. **Compáralo contra la fuerza bruta** en entradas chicas al azar. Así se prueban en este curso: el máximo de reuniones se compara contra probar todos los subconjuntos.

Si no encuentras el argumento y el contraejemplo aparece, el problema suele pedir **programación dinámica**.

<!--
Nota del instructor: pide que propongan otro voraz para las reuniones (ordenar por inicio o por duración) y que encuentren su contraejemplo en dos minutos. Por inicio: una reunión de 8 a 18 tapa a todas. Por duración: una corta de 11 a 13 choca con dos que sí cabían juntas, de 9 a 12 y de 12 a 15.
-->

---

<!-- _class: lead -->

# Parte 4
## Programación dinámica

---

## Subproblemas que se repiten

```csharp
// Sin memoria: cada llamada abre dos más. El número de llamadas crece de forma exponencial (cota O(2^n)).
public static long FibIngenuo(int n) => n < 2 ? n : FibIngenuo(n - 1) + FibIngenuo(n - 2);
```

```csharp
// De arriba hacia abajo con memoria: cada valor se calcula una vez. O(n) en tiempo y en espacio.
public static long FibMemo(int n, Dictionary<int, long>? memo = null)
{
    memo ??= [];
    if (n < 2) return n;
    if (memo.TryGetValue(n, out long hecho)) return hecho;
    long valor = FibMemo(n - 1, memo) + FibMemo(n - 2, memo);
    memo[n] = valor;
    return valor;
}
```

La versión ingenua hace exactamente 2 F(n + 1) - 1 llamadas: para n = 25, más de 240 mil. La prueba las cuenta.

---

## De abajo hacia arriba

```csharp
// De abajo hacia arriba: solo hacen falta los dos anteriores. O(n) en tiempo, O(1) en espacio.
public static long FibTabla(int n)
{
    long anterior = 0, actual = 1;
    if (n == 0) return 0;
    for (int i = 2; i <= n; i++)
        (anterior, actual) = (actual, anterior + actual);
    return actual;
}
```

| | Memoización | Tabulación |
|---|---|---|
| Dirección | de arriba hacia abajo, recursiva | de abajo hacia arriba, con un ciclo |
| Ventaja | se escribe igual que la recursión | sin riesgo de desbordar la pila; a veces O(1) de espacio |
| Cuándo | para llegar rápido a algo correcto | para mejorar después |

---

## La receta

1. **Estado:** ¿qué número describe un subproblema? ("el mínimo de monedas para formar x").
2. **Transición:** ¿cómo se calcula a partir de subproblemas más chicos?
3. **Caso base:** ¿cuál se sabe sin calcular? (formar 0 cuesta 0 monedas).
4. **Orden:** ¿en qué orden llenarlos para que lo que necesito ya esté calculado?
5. **Respuesta:** ¿qué celda contesta la pregunta original?

Dilo en voz alta en ese orden. Si el estado está bien elegido, el código sale casi solo.

<!--
Nota del instructor: aplica la receta en voz alta al cambio mínimo antes de pasar a la siguiente lámina: estado minimo[x], transición minimo[x - moneda] + 1, caso base minimo[0] = 0, orden de 1 al monto, respuesta minimo[monto]. Que el grupo la dicte.
-->

---

## El cambio mínimo, bien hecho

```csharp
// Mínimo de monedas para formar el monto, o -1 si no se puede.
// Estado: minimo[x] = menos monedas para formar x. O(monto × k) en tiempo y O(monto) en espacio.
public static int CambioMinimo(int[] monedas, int monto)
{
    ArgumentOutOfRangeException.ThrowIfNegative(monto);
    const int Imposible = int.MaxValue;
    var minimo = new int[monto + 1];
    Array.Fill(minimo, Imposible);
    minimo[0] = 0;

    for (int x = 1; x <= monto; x++)
        foreach (int moneda in monedas)
            if (moneda <= x && minimo[x - moneda] != Imposible)
                minimo[x] = Math.Min(minimo[x], minimo[x - moneda] + 1);

    return minimo[monto] == Imposible ? -1 : minimo[monto];
}
```

Con monedas 1, 3 y 4 y monto 6, da 2. El voraz daba 3.

---

## Otro clásico: la mejor racha

"Dado un arreglo con positivos y negativos, ¿cuál es la mayor suma de elementos consecutivos?"

```csharp
// Kadane: la mejor suma que TERMINA en i es el elemento solo o el elemento más la mejor anterior.
// O(n) en tiempo, O(1) en espacio. El arreglo no puede venir vacío.
public static long MaximaSumaSubarreglo(int[] datos)
{
    if (datos.Length == 0) throw new ArgumentException("El arreglo no puede venir vacío.", nameof(datos));
    long terminaAqui = datos[0], mejor = datos[0];
    for (int i = 1; i < datos.Length; i++)
    {
        terminaAqui = Math.Max(datos[i], terminaAqui + datos[i]);
        mejor = Math.Max(mejor, terminaAqui);
    }
    return mejor;
}
```

El estado es "la mejor suma que termina exactamente en i". Elegir bien el estado es la mitad del problema.

---

## Cómo reconocer cada técnica

| Señal | Técnica |
|-------|---------|
| El problema se parte en mitades independientes que luego se combinan | divide y vencerás |
| Ordenar y tomar siempre "el mejor de ahora" parece funcionar, y hay argumento de intercambio | voraz |
| "Mínimo", "máximo" o "cuántas formas", y las decisiones de ahora afectan a las de después | programación dinámica |
| La fuerza bruta recalcula los mismos subproblemas muchas veces | programación dinámica |
| Un contraejemplo chico tumba al voraz | programación dinámica |

---

<!-- _class: lead -->

# Parte 5
## La entrevista simulada

---

## 45 minutos

| Minuto | Qué pasa |
|--------|----------|
| 0 a 5 | Presentación y el proyecto en dos minutos |
| 5 a 10 | Enunciado y aclaraciones; ejemplos de entrada y salida |
| 10 a 15 | Fuerza bruta, mejora y su costo, en voz alta |
| 15 a 35 | Escribir el código |
| 35 a 40 | Probar a mano con un ejemplo y con casos borde |
| 40 a 45 | Mejoras y preguntas del candidato |

Quien entrevista lleva el reloj y avisa a los 30 y a los 40 minutos.

---

## La rúbrica, en corto

Cuatro ejes, del 1 al 4, con evidencia escrita:

- **Comunicación:** de "trabajó en silencio" a "narró cada decisión y pidió luz verde".
- **Resolución:** de "no llegó a una solución correcta" a "llegó a la óptima y explicó por qué lo es".
- **Código:** de "no compilaría" a "compilaría, con nombres claros y sin repetir".
- **Pruebas:** de "no probó" a "encontró solo los casos borde y corrigió".

La rúbrica completa, con los niveles intermedios, está en la práctica.

---

## Grabar y revisar

- **Pide permiso** a las dos personas antes de grabar, y borra la grabación cuando termine el curso.
- Basta con el audio y la pantalla compartida.
- Al verla, busca:
  - silencios de más de un minuto,
  - el momento en que empezaste a escribir sin haber dicho tu plan,
  - casos borde que te señaló otra persona y no encontraste tú,
  - muletillas que se repiten.
- Anota **una sola cosa** para cambiar en la siguiente.

---

## Ejercicio en vivo: el instructor en el banquillo

El grupo propone un problema de la lista de la práctica y el instructor lo resuelve en voz alta, con reloj.

Mientras tanto, cada quien llena la rúbrica **como si estuviera entrevistando**. Al final se comparan las calificaciones: ¿en qué eje no coincidimos y por qué?

<!--
Nota del instructor: elige uno de los problemas de la práctica que no hayas preparado de memoria, para que se vea el proceso real, con dudas incluidas. 15 minutos máximo. La discusión sobre en qué no coincidieron las calificaciones es la parte más valiosa: muestra que la rúbrica necesita evidencia, no impresiones.
-->

---

<!-- _class: lead -->

# Cierre

## Lo que te llevas

- Un método de cuatro pasos para no quedarte en blanco
- Divide y vencerás, voraces y programación dinámica, y cómo reconocerlos
- Una entrevista simulada calificada con evidencia, y una cosa concreta que cambiar

**Libros:** *50 Algorithms Every Programmer Should Know* (Ahmad, 2023), cap. 4, "Designing Algorithms". *C# Interview Guide* (Semenenko, 2024), cap. 7.

**Siguiente módulo:** C# y .NET en la entrevista.
