# Glosario - Módulo 5
## C# y .NET en la entrevista

> Términos usados en el módulo 5. Ordenado alfabéticamente.

---

## A

### AggregateException
**Categoría:** C# / Excepciones
**Definición:** Excepción que agrupa una o varias excepciones internas. `.Result` y `.Wait()` la usan para envolver lo que lanzó la tarea; `await`, en cambio, entrega la excepción original.

---

### Árbol de expresión
**Categoría:** C# / LINQ
**Definición:** Representación de una lambda como datos en vez de como código ejecutable. `IQueryable<T>` guarda así sus filtros para que un proveedor como EF Core los traduzca a SQL.

---

### async void
**Categoría:** C# / Asincronía
**Definición:** Método asíncrono que no devuelve una `Task`. Nadie puede esperarlo ni atrapar sus excepciones, que pueden tirar el proceso. Solo se usa en manejadores de eventos, cuya firma exige `void`.

---

## C

### CancellationToken
**Categoría:** .NET / Asincronía
**Definición:** Señal que se pasa a las operaciones asíncronas para pedirles que se detengan. Al cancelarse, la operación lanza `OperationCanceledException`. En ASP.NET Core, el de la petición se cancela si el cliente se desconecta.

---

### Cierre (closure)
**Categoría:** C# / Lambdas
**Definición:** Lambda que usa una variable de fuera de ella. Captura la variable, no su valor: si la variable cambia después, la lambda ve el cambio. Por eso tres lambdas creadas en un `for` devuelven el mismo número.

---

### Covarianza y contravarianza
**Categoría:** C# / Genéricos
**Definición:** Covarianza (`out T`): una interfaz que solo entrega `T` acepta un tipo más específico, como `IEnumerable<string>` donde se pide `IEnumerable<object>`. Contravarianza (`in T`): lo contrario, para lo que solo recibe `T`, como `Action<object>` donde se pide `Action<string>`.

---

## D

### Dependencia cautiva
**Categoría:** .NET / Inyección de dependencias
**Definición:** Error en el que un servicio de vida larga (singleton) recibe uno de vida corta (scoped) y lo retiene para siempre. Con `ValidateScopes`, que ASP.NET Core activa en desarrollo, el contenedor lo detecta.

---

## E

### Ejecución diferida
**Categoría:** C# / LINQ
**Definición:** Propiedad de operadores como `Where` y `Select`: describen la consulta, pero no la ejecutan hasta que se recorre. Si la colección cambia antes, el resultado lo refleja; si se recorre dos veces, se ejecuta dos veces.

---

## N

### Nullable reference types
**Categoría:** C# / Nulabilidad
**Definición:** Función del compilador que distingue `string` de `string?` y advierte cuando un valor que puede ser nulo se usa sin revisar. Es solo análisis en compilación: en ejecución, `string?` sigue siendo `string`.

---

### `Nullable<T>`
**Categoría:** C# / Nulabilidad
**Definición:** Estructura que permite que un tipo por valor no tenga valor. `int?` es `Nullable<int>`, con las propiedades `HasValue` y `Value`. A diferencia de `string?`, sí es otro tipo en ejecución.

---

## O

### Operador null-forgiving (`!`)
**Categoría:** C# / Nulabilidad
**Definición:** Sufijo que le dice al compilador que un valor no es nulo y calla la advertencia. No hace nada en ejecución: si el valor sí era nulo, el `NullReferenceException` llega igual.

---

## R

### Record
**Categoría:** C# / Tipos
**Definición:** Tipo con igualdad por valor generada por el compilador (`Equals`, `GetHashCode`, `==`) y copia con `with`. Dos records con las mismas propiedades son iguales aunque sean instancias distintas.

---

## T

### Tiempo de vida (lifetime)
**Categoría:** .NET / Inyección de dependencias
**Definición:** Cuánto vive una instancia en el contenedor: Singleton (toda la aplicación), Scoped (un alcance, una petición en ASP.NET Core) o Transient (una nueva cada vez). Un servicio solo puede depender de otros que vivan igual o más que él.

---

### Tipo por valor y tipo por referencia
**Categoría:** C# / Tipos
**Definición:** Un tipo por valor (`struct`, `int`) se copia completo al asignarlo; uno por referencia (`class`) copia la referencia, y las dos variables apuntan al mismo objeto.
