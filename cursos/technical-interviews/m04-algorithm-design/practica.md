# Práctica - Entrevista simulada de 45 minutos

## Objetivo

Vivir una entrevista técnica completa antes de la de verdad: 45 minutos con reloj, un problema que no conoces, alguien que entrevista con una rúbrica y una grabación para revisarte después. Lo que se mide no es si llegas a la solución óptima, sino cómo avanzas en un problema que no conocías sin quedarte en blanco.

## Organización

- **En parejas.** Cada persona es candidata una vez y entrevistadora otra. Si el tiempo de la sesión no alcanza para dos entrevistas, la segunda se hace fuera de clase, en la misma semana.
- **Quien entrevista elige una tarjeta** (A, B o C) que su pareja no haya visto, y lee antes la solución.
- **Se graba** solo audio y pantalla, con permiso de las dos personas. La grabación es para quien fue entrevistado y se borra al terminar el curso.
- **Herramientas:** un editor sin autocompletado de IA, o papel. Quien resuelve puede escribir pseudocódigo al principio, pero la versión final va en C#.

## El guion de los 45 minutos

| Minuto | Qué pasa | Qué hace quien entrevista |
|--------|----------|---------------------------|
| 0 a 5 | Presentación y el proyecto en dos minutos (módulo 1) | Escucha y hace una pregunta de seguimiento |
| 5 a 10 | Lee el enunciado y contesta aclaraciones | Contesta solo con lo que dice la tarjeta |
| 10 a 15 | Fuerza bruta, mejora y su costo | No da la respuesta; pregunta "¿y cuánto cuesta?" |
| 15 a 35 | Escribir el código | Avisa a los 30 minutos; da pistas solo tras dos minutos de silencio |
| 35 a 40 | Probar a mano y casos borde | Propone un caso borde si el candidato no encontró ninguno |
| 40 a 45 | Mejoras y preguntas del candidato | Contesta y cierra |

---

## Tarjeta A. Las salas de juntas

**Enunciado (se lee en voz alta).** Te doy la lista de reuniones de un día, cada una con hora de inicio y de fin. ¿Cuántas salas de juntas hacen falta, como mínimo, para que ninguna reunión se quede sin sala?

**Aclaraciones (contesta solo si preguntan).**
- Una reunión de 9 a 10 y otra de 10 a 11 **pueden** usar la misma sala.
- La lista no viene ordenada y puede venir vacía (0 salas).
- Puede haber hasta 100,000 reuniones.

**Pistas, en orden.**
1. ¿Qué número buscas en realidad? Piensa en el momento más ocupado del día.
2. Si recorres las reuniones por hora de inicio, ¿qué necesitas saber de las salas ya ocupadas?
3. Solo te importa la sala que se libera primero. ¿Qué estructura te da siempre el mínimo?

**Solución.**

```csharp
// Mínimo de salas: ordenar por inicio y llevar en un montículo la hora de fin de cada sala ocupada.
// Si la sala que se libera primero ya está libre, se reutiliza. O(n log n) en tiempo, O(n) en espacio.
public static int MinimoDeSalas(IEnumerable<Intervalo> reuniones)
{
    var finesPorSala = new PriorityQueue<int, int>();
    foreach (var r in reuniones.OrderBy(r => r.Inicio))
    {
        if (finesPorSala.Count > 0 && finesPorSala.Peek() <= r.Inicio)
            finesPorSala.Dequeue();          // esa sala ya se desocupó: se reutiliza
        finesPorSala.Enqueue(r.Fin, r.Fin);
    }
    return finesPorSala.Count;
}
```

**Lo que se espera oír.** La fuerza bruta revisa, para cada momento del día, cuántas reuniones están activas. La mejora ordena por inicio y guarda en una cola de prioridad la hora de fin de cada sala ocupada: si la que se libera primero ya terminó, se reutiliza; si no, hace falta otra. El número de salas al final es el máximo que se necesitó. Ordenar cuesta O(n log n) y cada operación del montículo, O(log n).

**Casos para probar:** lista vacía (0), una reunión (1), reuniones encadenadas que se tocan (1), una larga con dos cortas adentro (2), cuatro reuniones con tres simultáneas (3). La prueba del proyecto compara 300 listas al azar contra el pico de reuniones simultáneas contado minuto a minuto.

---

## Tarjeta B. La racha creciente más larga

**Enunciado.** Te doy las ventas diarias de una tienda como un arreglo de enteros. Quiero el largo de la subsecuencia **estrictamente creciente** más larga: los días no tienen que ser consecutivos, pero sí en orden.

**Aclaraciones.**
- `[10, 9, 2, 5, 3, 7, 101, 18]` da 4 (por ejemplo, 2, 3, 7, 18).
- Valores iguales no cuentan como crecientes: `[7, 7, 7]` da 1.
- Arreglo vacío: 0. Puede haber hasta 2,500 días en la primera versión; luego pregunta qué pasa con un millón.

**Pistas, en orden.**
1. ¿Cuál es la racha creciente más larga que **termina** en el día i?
2. Para calcularla, ¿qué días anteriores te sirven?
3. (Para la mejora) De todas las rachas de largo k, ¿cuál conviene recordar?

**Solución, primera versión.**

```csharp
// Subsecuencia estrictamente creciente más larga.
// Primera versión: largo[i] = la más larga que termina en i. O(n²) en tiempo, O(n) en espacio.
public static int LongitudCuadratica(int[] datos)
{
    if (datos.Length == 0) return 0;
    var largo = new int[datos.Length];
    int mejor = 0;
    for (int i = 0; i < datos.Length; i++)
    {
        largo[i] = 1;
        for (int j = 0; j < i; j++)
            if (datos[j] < datos[i])
                largo[i] = Math.Max(largo[i], largo[j] + 1);
        mejor = Math.Max(mejor, largo[i]);
    }
    return mejor;
}
```

**Solución mejorada.**

```csharp
// Mejora: colas[k] = el menor final posible de una subsecuencia creciente de largo k + 1.
// colas siempre está ordenada, así que se busca con búsqueda binaria. O(n log n) en tiempo, O(n) en espacio.
public static int LongitudNLogN(int[] datos)
{
    var colas = new List<int>();
    foreach (int x in datos)
    {
        int i = colas.BinarySearch(x);
        if (i < 0) i = ~i;                   // no está: ~i es donde debería ir
        if (i == colas.Count) colas.Add(x);  // extiende la subsecuencia más larga
        else colas[i] = x;                   // mejora un final: más chico deja más espacio
    }
    return colas.Count;
}
```

**Lo que se espera oír.** La fuerza bruta prueba todas las subsecuencias, que son 2^n: exponencial. La primera mejora es programación dinámica con el estado "la más larga que termina en i": O(n²), suficiente para 2,500 días. Con un millón no alcanza, y la mejora guarda, para cada largo, el final más chico posible; esa lista siempre está ordenada y se actualiza con búsqueda binaria. Un detalle que vale puntos: `colas` **no** es una subsecuencia válida, solo sus largos lo son.

**Casos para probar:** vacío, un elemento, el ejemplo del enunciado, todos iguales, todo decreciente (1), todo creciente (n). La prueba del proyecto compara las dos versiones en 300 arreglos al azar.

---

## Tarjeta C. Juntar los horarios

**Enunciado.** Un consultorio guarda sus bloques ocupados como intervalos `[inicio, fin]`, pero se capturaron desordenados y algunos se enciman. Devuélveme la lista limpia: ordenada y sin intervalos que se enciman.

**Aclaraciones.**
- `[1, 3]` y `[3, 5]` **se tocan** y se juntan en `[1, 5]`.
- Un intervalo puede quedar dentro de otro: `[1, 10]` y `[2, 3]` dan `[1, 10]`.
- La lista puede venir vacía. Hay que devolver una lista nueva.

**Pistas, en orden.**
1. ¿Qué pasaría si estuvieran ordenados por inicio?
2. Ya ordenados, ¿con qué intervalo tienes que comparar el actual?
3. Cuidado con el fin al juntar: ¿siempre es el fin del actual?

**Solución.**

```csharp
// Combinar intervalos que se traslapan o se tocan: [1, 3] y [3, 5] quedan como [1, 5].
// O(n log n) por el ordenamiento, más una pasada O(n).
public static List<Intervalo> Combinar(IEnumerable<Intervalo> intervalos)
{
    var resultado = new List<Intervalo>();
    foreach (var actual in intervalos.OrderBy(i => i.Inicio))
    {
        if (resultado.Count > 0 && actual.Inicio <= resultado[^1].Fin)
        {
            var ultimo = resultado[^1];
            resultado[^1] = ultimo with { Fin = Math.Max(ultimo.Fin, actual.Fin) };
        }
        else
        {
            resultado.Add(actual);
        }
    }
    return resultado;
}
```

**Lo que se espera oír.** La fuerza bruta compara cada par y repite hasta que nada cambie: O(n²) o peor. Ordenados por inicio, basta comparar con el último intervalo del resultado: si el actual empieza antes de que ese termine, se juntan. El fin nuevo es el **mayor** de los dos, porque el actual puede quedar adentro. Las aclaraciones importan: con intervalos semiabiertos, `[1, 3)` y `[3, 5)` no se juntarían y la comparación sería `<` en vez de `<=`.

**Casos para probar:** vacío, desordenados que se enciman, dos que se tocan, uno dentro de otro, dos separados que llegan al revés.

---

## Rúbrica de la entrevista

Califica cada eje del 1 al 4 y escribe **una frase de evidencia**: lo que el candidato dijo o hizo que justifica la calificación. Sin evidencia, la calificación no cuenta.

| Eje | 1 | 2 | 3 | 4 |
|-----|---|---|---|---|
| Comunicación | Trabajó en silencio; no se supo qué pensaba | Habló, pero empezó a escribir sin decir su plan | Explicó su plan antes de escribir y narró lo principal | Aclaró, narró cada decisión y pidió luz verde antes de escribir |
| Resolución | No llegó a una solución correcta | Llegó a la fuerza bruta y supo su costo | Llegó a una mejora correcta y supo su costo | Llegó a la mejor solución y explicó por qué no se puede mejorar mucho más |
| Código | No compilaría o no corresponde a lo que explicó | Compila con errores menores; nombres poco claros | Compilaría, legible, con nombres claros | Compilaría, legible, sin repetición y con los bordes cubiertos desde el principio |
| Pruebas | No probó | Probó solo el ejemplo del enunciado | Recorrió el código con un ejemplo y un caso borde | Encontró solo los casos borde, encontró un error y lo corrigió |
| Pistas | Necesitó todas y aun así no avanzó | Necesitó dos o tres | Necesitó una | No necesitó |

**Cómo se lee:** de 5 a 20 puntos. Más que el total, sirve el eje más bajo: es lo que se ensaya primero.

## Revisión de la grabación

Quien fue entrevistado ve su grabación en la semana y llena esto:

```text
Minuto en que empecé a escribir código:
¿Había dicho mi plan antes? (sí / no)
Silencios de más de un minuto (minuto y qué estaba pensando):
Casos borde que encontré yo:
Casos borde que me señalaron:
Muletilla que más repito:
Una sola cosa que voy a cambiar en la siguiente entrevista:
```

## Checklist de la práctica

- [ ] Fui candidato en una entrevista de 45 minutos con una tarjeta que no conocía.
- [ ] Fui entrevistador y llené la rúbrica con evidencia en cada eje.
- [ ] La entrevista quedó grabada, con permiso de las dos personas.
- [ ] Vi mi grabación y llené la revisión.
- [ ] Anoté una sola cosa concreta para cambiar.
- [ ] Leí las tres soluciones y puedo explicar la de mi tarjeta sin verla.
