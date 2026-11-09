# Práctica - S03 - RAG: que responda con sus documentos

**Aplicaciones con LLM: de la API al agente**
**Tiempo estimado:** 2 h 30 min

---

## Objetivo

Al proyecto `AsistenteCursos` que viene creciendo desde la sesión 1 le agregas un asistente que
responde preguntas sobre la documentación del curso, **cita el fragmento de donde sacó cada
respuesta** y, cuando la respuesta no está en los documentos, **dice "no lo sé"** en lugar de inventar.

Al final tienes:

- PostgreSQL 17 con pgvector y Ollama con el modelo `bge-m3`, en contenedores, solo en `127.0.0.1`.
- Un comando `ingestar` que parte los documentos, genera sus vectores y los guarda.
- Un comando `preguntar` que recupera, responde con salida estructurada y valida las citas.
- Pruebas sin costo (fragmentador e integración con la base) y una evaluación con casos.

---

## Antes de empezar

- La práctica de la sesión 2 en verde: `dotnet test --solution AsistenteCursos.slnx` sin fallas.
- Docker corriendo y unos 2 GB libres: la imagen de Ollama más el modelo `bge-m3` (1.2 GB).
- Tu llave del curso en la variable `ANTHROPIC_API_KEY`. Nunca en un archivo del repositorio.

> **Por qué Ollama y no la API de Anthropic.** Anthropic no ofrece un modelo de embeddings. El curso usa
> `bge-m3` en tu máquina: es multilingüe (los documentos están en español), no pide una segunda llave,
> no cuesta por llamada y tus documentos no salen de tu equipo. Todo pasa por la interfaz
> `IGeneradorEmbeddings`: cambiar a un proveedor hospedado es escribir otra clase y reindexar.

---

## Paso 1 - Levantar la base y el modelo de embeddings (15 min)

Crea `docker-compose.yml` en la raíz del repositorio:

```yaml
# Infraestructura local del curso. Las contraseñas salen de .env, que no se versiona.
services:
  db:
    image: pgvector/pgvector:0.8.7-pg17
    environment:
      POSTGRES_DB: asistente
      POSTGRES_USER: asistente
      POSTGRES_PASSWORD: ${ASISTENTE_DB_PASSWORD:?Define ASISTENTE_DB_PASSWORD en .env}
    ports:
      - "127.0.0.1:5433:5432"
    volumes:
      - datos-db:/var/lib/postgresql/data
  ollama:
    image: ollama/ollama:0.40.15
    ports:
      - "127.0.0.1:11434:11434"
    volumes:
      - modelos-ollama:/root/.ollama
volumes:
  datos-db:
  modelos-ollama:
```

Y `.env.example`, que sí se versiona, como plantilla del `.env`, que no:

```
# Copia este archivo a .env y pon tu valor. .env no se versiona: está en .gitignore.
ASISTENTE_DB_PASSWORD=<SU_CONTRASENA_LOCAL>
```

Levántalo y baja el modelo:

```bash
cp .env.example .env        # edita .env y pon una contraseña local tuya
docker compose up -d
docker compose exec ollama ollama pull bge-m3
docker compose ps
```

**Lo que debe pasar:** `docker compose ps` muestra `db` y `ollama` en estado `running`, y el `pull`
termina con `success`. Si el compose se niega a arrancar con "Define ASISTENTE_DB_PASSWORD en .env",
el `.env` no existe o le falta la variable: es a propósito, el compose no trae contraseña por omisión.

Ahora las variables de la terminal. **Usa comillas simples**: la cadena de conexión lleva `;` y sin
comillas la terminal la parte en varios comandos.

```bash
export ASISTENTE_DB='Host=localhost;Port=5433;Database=asistente;Username=asistente;Password=<SU_CONTRASENA_LOCAL>'
export ANTHROPIC_API_KEY='<SU_LLAVE>'
```

> No hagas `source .env` para cargar la cadena de conexión: por el mismo `;`, el error que verías
> culpa a la contraseña y lo que falla es la línea partida.

---

## Paso 2 - Las versiones, fijadas en un solo lugar (5 min)

Agrega Npgsql y Pgvector a `Directory.Packages.props`:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Una sola versión fijada por paquete, para todo el repositorio. -->
    <PackageVersion Include="Anthropic" Version="13.6.0" />
    <PackageVersion Include="Npgsql" Version="10.0.3" />
    <PackageVersion Include="Pgvector" Version="0.3.2" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.11.0" />
    <PackageVersion Include="xunit.v3" Version="4.0.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="4.0.1" />
  </ItemGroup>
</Project>
```

Y referéncialos desde `src/Asistente.Core/Asistente.Core.csproj` (sin versión: la versión vive arriba):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Anthropic" />
    <PackageReference Include="Npgsql" />
    <PackageReference Include="Pgvector" />
  </ItemGroup>
</Project>
```

**Lo que debe pasar:** `dotnet build AsistenteCursos.slnx` termina con 0 errores y 0 warnings.

---

## Paso 3 - La interfaz de embeddings y sus dos implementaciones (20 min)

La interfaz propia. El resto del sistema no sabe quién genera los vectores:

```csharp
namespace Asistente.Core.Rag;

/// <summary>Algunos modelos de embeddings tratan distinto un documento y una consulta.</summary>
public enum TipoTexto { Documento, Consulta }

/// <summary>
/// La interfaz propia: el resto del sistema no sabe qué proveedor genera los vectores.
/// Cambiar de proveedor obliga a reindexar todo: vectores de modelos distintos no se comparan.
/// </summary>
public interface IGeneradorEmbeddings
{
    string Modelo { get; }
    int Dimensiones { get; }
    Task<IReadOnlyList<float[]>> GenerarAsync(IReadOnlyList<string> textos, TipoTexto tipo, CancellationToken ct = default);
}
```

La implementación real, contra la API local de Ollama (`POST /api/embed` acepta varios textos a la vez):

```csharp
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Asistente.Core.Rag;

/// <summary>Embeddings con un modelo local servido por Ollama. Por omisión bge-m3: multilingüe, 1024 dimensiones.</summary>
public sealed class EmbeddingsOllama(HttpClient http, string modelo = "bge-m3", int dimensiones = 1024) : IGeneradorEmbeddings
{
    public string Modelo => modelo;
    public int Dimensiones => dimensiones;

    public async Task<IReadOnlyList<float[]>> GenerarAsync(
        IReadOnlyList<string> textos, TipoTexto tipo, CancellationToken ct = default)
    {
        // bge-m3 no distingue documento de consulta; otros modelos piden un prefijo o un campo distinto.
        using var respuesta = await http.PostAsJsonAsync("/api/embed", new { model = modelo, input = textos }, ct);
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaEmbed>(ct)
            ?? throw new InvalidOperationException("Ollama devolvió un cuerpo vacío.");

        if (cuerpo.Embeddings.Count != textos.Count)
            throw new InvalidOperationException($"Pedí {textos.Count} vectores y llegaron {cuerpo.Embeddings.Count}.");
        if (cuerpo.Embeddings.Any(v => v.Length != dimensiones))
            throw new InvalidOperationException($"El modelo {modelo} no devuelve {dimensiones} dimensiones.");
        return cuerpo.Embeddings;
    }

    private sealed record RespuestaEmbed([property: JsonPropertyName("embeddings")] List<float[]> Embeddings);
}
```

Y una implementación léxica que **no entiende nada**, pero que no necesita modelo ni red: sirve para
probar la tubería (fragmentar, guardar, buscar) en milisegundos.

```csharp
namespace Asistente.Core.Rag;

/// <summary>
/// Vectores léxicos por hashing de palabras: sin modelo, sin red y deterministas.
/// Sirven para probar la tubería (fragmentar, guardar, buscar). No entienden sinónimos.
/// </summary>
public sealed class EmbeddingsPorHash(int dimensiones = 1024) : IGeneradorEmbeddings
{
    public string Modelo => "hash-lexico";
    public int Dimensiones => dimensiones;

    public Task<IReadOnlyList<float[]>> GenerarAsync(
        IReadOnlyList<string> textos, TipoTexto tipo, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<float[]>>(textos.Select(Vectorizar).ToList());

    private float[] Vectorizar(string texto)
    {
        var v = new float[dimensiones];
        var palabras = Texto.Normalizar(texto)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => new string(p.Where(char.IsLetterOrDigit).ToArray()))
            .Where(p => p.Length >= 3);
        foreach (var palabra in palabras)
            v[Fnv1a(palabra) % (uint)dimensiones] += 1f;

        double norma = Math.Sqrt(v.Sum(x => (double)x * x));
        if (norma == 0) { v[0] = 1f; return v; }   // pgvector no define el coseno de un vector en cero
        for (int i = 0; i < v.Length; i++) v[i] = (float)(v[i] / norma);
        return v;
    }

    private static uint Fnv1a(string s)
    {
        uint h = 2166136261;
        foreach (char c in s) { h ^= c; h *= 16777619; }
        return h;
    }
}
```

Observa las dos validaciones de `EmbeddingsOllama`: cuántos vectores llegaron y de qué tamaño. Un
modelo distinto al esperado no truena al guardar; truena en la búsqueda, semanas después, con
resultados que no tienen sentido.

---

## Paso 4 - Fragmentar (20 min)

Crea `src/Asistente.Core/Rag/Fragmentador.cs`:

```csharp
using System.Text;

namespace Asistente.Core.Rag;

public sealed record FragmentoNuevo(string Fuente, string Seccion, string Contenido);

/// <summary>Parte un Markdown por secciones y, si una sección es larga, por párrafos con solape.</summary>
public static class Fragmentador
{
    public static IReadOnlyList<FragmentoNuevo> Partir(string fuente, string markdown, int maxCaracteres = 1500, int solape = 200)
    {
        var resultado = new List<FragmentoNuevo>();
        foreach (var (seccion, cuerpo) in Secciones(markdown))
        {
            var actual = new StringBuilder();
            foreach (var pedazo in Parrafos(cuerpo).SelectMany(p => Trocear(p, maxCaracteres)))
            {
                if (actual.Length > 0 && actual.Length + pedazo.Length + 2 > maxCaracteres)
                {
                    resultado.Add(Nuevo(fuente, seccion, actual.ToString()));
                    string cola = Cola(actual.ToString(), solape);
                    actual.Clear().Append(cola);
                }
                if (actual.Length > 0) actual.Append("\n\n");
                actual.Append(pedazo);
            }
            if (actual.ToString().Trim().Length > 0) resultado.Add(Nuevo(fuente, seccion, actual.ToString()));
        }
        return resultado;
    }

    // Cada fragmento lleva el título de su sección: sin él, "la tabla de abajo" no significa nada.
    private static FragmentoNuevo Nuevo(string fuente, string seccion, string texto) =>
        new(fuente, seccion, $"{seccion}\n\n{texto.Trim()}");

    private static IEnumerable<(string Seccion, string Cuerpo)> Secciones(string markdown)
    {
        string titulo = "Introducción";
        var cuerpo = new StringBuilder();
        bool enCodigo = false;
        foreach (var linea in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            if (linea.TrimStart().StartsWith("```")) enCodigo = !enCodigo;
            bool esEncabezado = !enCodigo && linea.StartsWith('#');
            if (esEncabezado)
            {
                if (cuerpo.ToString().Trim().Length > 0) yield return (titulo, cuerpo.ToString());
                titulo = linea.TrimStart('#').Trim();
                cuerpo.Clear();
            }
            else cuerpo.AppendLine(linea);
        }
        if (cuerpo.ToString().Trim().Length > 0) yield return (titulo, cuerpo.ToString());
    }

    // Párrafos separados por línea en blanco; un bloque de código cuenta como un solo párrafo.
    private static IEnumerable<string> Parrafos(string cuerpo)
    {
        var actual = new StringBuilder();
        bool enCodigo = false;
        foreach (var linea in cuerpo.Split('\n'))
        {
            if (linea.TrimStart().StartsWith("```")) enCodigo = !enCodigo;
            if (!enCodigo && linea.Trim().Length == 0)
            {
                if (actual.Length > 0) { yield return actual.ToString().TrimEnd(); actual.Clear(); }
                continue;
            }
            actual.AppendLine(linea);
        }
        if (actual.Length > 0) yield return actual.ToString().TrimEnd();
    }

    private static IEnumerable<string> Trocear(string parrafo, int max)
    {
        for (int i = 0; i < parrafo.Length; i += max)
            yield return parrafo.Substring(i, Math.Min(max, parrafo.Length - i));
    }

    // Los últimos caracteres del fragmento anterior, cortados en un espacio para no partir palabras.
    private static string Cola(string texto, int solape)
    {
        if (texto.Length <= solape) return texto;
        var cola = texto[^solape..];
        int espacio = cola.IndexOf(' ');
        return espacio >= 0 ? cola[(espacio + 1)..] : cola;
    }
}
```

Las tres decisiones que importan:

1. **Se parte por sección**, no cada N caracteres: una sección suele hablar de una sola cosa.
2. **Cada fragmento lleva el título de su sección** al principio. Sin él, un fragmento que dice
   "la tabla de abajo muestra los costos" no se parece a ninguna pregunta sobre costos.
3. **Las secciones largas se parten por párrafos, con solape**: el fragmento siguiente arranca con
   la cola del anterior, para que una idea partida a la mitad esté completa en alguno de los dos.

---

## Paso 5 - El almacén vectorial en PostgreSQL (25 min)

Crea `src/Asistente.Core/Rag/AlmacenVectorial.cs`:

```csharp
using Npgsql;
using Pgvector;

namespace Asistente.Core.Rag;

public sealed record FragmentoRecuperado(long Id, string Fuente, string Seccion, string Contenido, double Distancia)
{
    /// <summary>La clave con la que el modelo cita el fragmento.</summary>
    public string Clave => $"F{Id}";
}

/// <summary>Fragmentos y vectores en PostgreSQL con pgvector. Todo el SQL va parametrizado.</summary>
public sealed class AlmacenVectorial(NpgsqlDataSource fuente)
{
    public static NpgsqlDataSource CrearFuente(string cadenaDeConexion)
    {
        var constructor = new NpgsqlDataSourceBuilder(cadenaDeConexion);
        constructor.UseVector();   // registra el tipo vector de pgvector en Npgsql
        return constructor.Build();
    }

    public async Task CrearEsquemaAsync(int dimensiones, CancellationToken ct = default)
    {
        await using var conexion = await fuente.OpenConnectionAsync(ct);
        // El DDL no admite parámetros: la dimensión es un entero de configuración, nunca texto de un usuario.
        await using (var cmd = new NpgsqlCommand($"""
            CREATE EXTENSION IF NOT EXISTS vector;
            CREATE TABLE IF NOT EXISTS fragmentos (
                id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                fuente     text        NOT NULL,
                seccion    text        NOT NULL,
                contenido  text        NOT NULL,
                embedding  vector({dimensiones}) NOT NULL,
                creado_utc timestamptz NOT NULL DEFAULT now()
            );
            CREATE INDEX IF NOT EXISTS fragmentos_fuente ON fragmentos (fuente);
            CREATE INDEX IF NOT EXISTS fragmentos_embedding
                ON fragmentos USING hnsw (embedding vector_cosine_ops);
            """, conexion))
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }
        await conexion.ReloadTypesAsync(ct);   // la extensión recién creada trae el tipo vector
    }

    /// <summary>Reingestar un documento reemplaza sus fragmentos, en una sola transacción.</summary>
    public async Task ReemplazarFuenteAsync(
        string fuenteDoc, IReadOnlyList<FragmentoNuevo> fragmentos, IReadOnlyList<float[]> vectores, CancellationToken ct = default)
    {
        if (fragmentos.Count != vectores.Count)
            throw new ArgumentException("Cada fragmento necesita su vector.", nameof(vectores));

        await using var conexion = await fuente.OpenConnectionAsync(ct);
        await using var tx = await conexion.BeginTransactionAsync(ct);
        await using (var borrar = new NpgsqlCommand("DELETE FROM fragmentos WHERE fuente = @fuente", conexion, tx))
        {
            borrar.Parameters.AddWithValue("fuente", fuenteDoc);
            await borrar.ExecuteNonQueryAsync(ct);
        }
        for (int i = 0; i < fragmentos.Count; i++)
        {
            await using var insertar = new NpgsqlCommand("""
                INSERT INTO fragmentos (fuente, seccion, contenido, embedding)
                VALUES (@fuente, @seccion, @contenido, @embedding)
                """, conexion, tx);
            insertar.Parameters.AddWithValue("fuente", fuenteDoc);
            insertar.Parameters.AddWithValue("seccion", fragmentos[i].Seccion);
            insertar.Parameters.AddWithValue("contenido", fragmentos[i].Contenido);
            insertar.Parameters.AddWithValue("embedding", new Vector(vectores[i]));
            await insertar.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    /// <summary>Los k fragmentos más cercanos por distancia coseno (operador &lt;=&gt;): 0 es idéntico, 2 es opuesto.</summary>
    public async Task<IReadOnlyList<FragmentoRecuperado>> BuscarAsync(float[] consulta, int k, CancellationToken ct = default)
    {
        await using var cmd = fuente.CreateCommand("""
            SELECT id, fuente, seccion, contenido, embedding <=> @consulta AS distancia
            FROM fragmentos
            ORDER BY embedding <=> @consulta
            LIMIT @k
            """);
        cmd.Parameters.AddWithValue("consulta", new Vector(consulta));
        cmd.Parameters.AddWithValue("k", k);

        var resultado = new List<FragmentoRecuperado>();
        await using var lector = await cmd.ExecuteReaderAsync(ct);
        while (await lector.ReadAsync(ct))
            resultado.Add(new(lector.GetInt64(0), lector.GetString(1), lector.GetString(2), lector.GetString(3), lector.GetDouble(4)));
        return resultado;
    }
}
```

Fíjate en:

- `UseVector()` registra el tipo `vector` en Npgsql; sin eso, `new Vector(...)` no se puede enviar.
- `ReloadTypesAsync` después de `CREATE EXTENSION`: la conexión ya había cargado los tipos de la base
  y el tipo `vector` recién creado no estaba entre ellos.
- El índice HNSW con `vector_cosine_ops` corresponde al operador `<=>` (distancia coseno). Si usas
  otro operador en la consulta, el índice no se usa.
- Reingestar un documento borra sus fragmentos e inserta los nuevos **en una transacción**: o queda
  la versión nueva completa o queda la vieja, nunca la mitad.

---

## Paso 6 - El asistente que cita y sabe decir "no lo sé" (25 min)

El contrato de salida, la respuesta y las opciones en `src/Asistente.Core/Rag/SalidaRag.cs`:

```csharp
using Anthropic.Helpers;

namespace Asistente.Core.Rag;

/// <summary>El contrato de salida del asistente: responder, o decir que no sabe.</summary>
[SchemaClass("Respuesta a una pregunta usando solo los documentos entregados.")]
public sealed class SalidaRag
{
    [SchemaProperty("true solo si los documentos contienen la respuesta.")]
    public bool Sabe { get; set; }

    [SchemaProperty("La respuesta en español. Si sabe es false, una cadena vacía.")]
    public string Respuesta { get; set; } = "";

    [SchemaProperty("Las claves de los documentos usados, por ejemplo F12. Vacía si sabe es false.")]
    public List<string> Fuentes { get; set; } = [];
}

public sealed record RespuestaRag(
    bool Sabe,
    string Texto,
    IReadOnlyList<FragmentoRecuperado> Citadas,
    IReadOnlyList<FragmentoRecuperado> Recuperadas,
    decimal CostoUsd,
    string? Motivo);

/// <summary>K y el umbral de distancia se calibran con el juego de preguntas, no a ojo.</summary>
public sealed record OpcionesRag(int K = 5, double DistanciaMaxima = 0.55);
```

El prompt, versionado como en la sesión 2, en `prompts/asistente-documentos.v1.md`:

```markdown
Eres el asistente de documentación de una academia de cursos en línea. Respondes preguntas de
alumnos usando únicamente los documentos que vienen en el mensaje, cada uno marcado con una clave
entre corchetes, por ejemplo [F12].

Reglas:

1. Responde solo con lo que dicen los documentos. Si los documentos no contienen la respuesta,
   o solo la contienen a medias, pon sabe en false: decir que no sabes es una respuesta correcta,
   inventar es un error grave.
2. No uses lo que sabes por tu cuenta, aunque estés seguro: el alumno necesita la versión del curso.
3. En fuentes pon las claves de todos los documentos que usaste, y solo esas.
4. Responde en español de México, en un máximo de 150 palabras, con un ejemplo si el documento lo trae.
```

Y el asistente, en `src/Asistente.Core/Rag/AsistenteDocumentos.cs`:

```csharp
using Anthropic;
using Anthropic.Helpers;
using Anthropic.Models.Messages;
using Asistente.Core.Llm;
using Asistente.Core.Prompts;

namespace Asistente.Core.Rag;

/// <summary>Responde con los documentos indexados y cita la fuente; si no está, dice que no sabe.</summary>
public sealed class AsistenteDocumentos(
    AnthropicClient cliente,
    IGeneradorEmbeddings embeddings,
    AlmacenVectorial almacen,
    PromptVersionado prompt,
    string modelo,
    OpcionesRag opciones)
{
    public const string NoLoSe = "No lo sé: eso no está en los documentos del curso.";

    public async Task<RespuestaRag> PreguntarAsync(string pregunta, CancellationToken ct = default)
    {
        var consulta = (await embeddings.GenerarAsync([pregunta], TipoTexto.Consulta, ct))[0];
        var recuperadas = await almacen.BuscarAsync(consulta, opciones.K, ct);
        var relevantes = recuperadas.Where(f => f.Distancia <= opciones.DistanciaMaxima).ToList();

        // Primera defensa contra inventar: si nada se parece, no se le pregunta al modelo.
        if (relevantes.Count == 0)
            return SinRespuesta("Ningún fragmento pasó el umbral de distancia.", recuperadas, 0m);

        var respuesta = await cliente.Messages.Create(new MessageCreateParams
        {
            Model = modelo,
            MaxTokens = 4096,
            System = new List<TextBlockParam> { new() { Text = prompt.Texto, CacheControl = new CacheControlEphemeral() } },
            OutputConfig = new OutputConfig
            {
                Effort = Effort.Low,
                Format = StructuredOutput.CreateJsonFormat<SalidaRag>(),
            },
            Messages = [new UserMessageParam { Content = ConstruirContexto(relevantes) + $"\n\nPregunta: {pregunta}" }],
        }, ct);
        decimal costo = TablaDePrecios.Costo(modelo, respuesta.Usage);

        if (respuesta.StopReason != StopReason.EndTurn)
            return SinRespuesta($"La llamada terminó con stop_reason {respuesta.StopReason}.", recuperadas, costo);

        var salida = StructuredOutput.Parse<SalidaRag>(
            string.Concat(respuesta.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text)));
        if (!salida.Sabe)
            return SinRespuesta("El modelo indicó que los documentos no lo contienen.", recuperadas, costo);

        // Segunda defensa: toda cita tiene que ser un fragmento que de verdad se le entregó.
        var citadas = relevantes.Where(f => salida.Fuentes.Contains(f.Clave)).ToList();
        if (citadas.Count == 0 || citadas.Count != salida.Fuentes.Distinct().Count())
            return SinRespuesta("La respuesta no citó fuentes o citó claves que no se recuperaron.", recuperadas, costo);

        return new RespuestaRag(true, salida.Respuesta, citadas, recuperadas, costo, null);
    }

    public static string ConstruirContexto(IReadOnlyList<FragmentoRecuperado> fragmentos) =>
        "Documentos:\n\n" + string.Join("\n\n", fragmentos.Select(f =>
            $"[{f.Clave}] {f.Fuente} > {f.Seccion}\n{f.Contenido}"));

    private static RespuestaRag SinRespuesta(string motivo, IReadOnlyList<FragmentoRecuperado> recuperadas, decimal costo) =>
        new(false, NoLoSe, [], recuperadas, costo, motivo);
}
```

Las dos capas del "no lo sé":

1. **El umbral**: si ningún fragmento está lo bastante cerca, no se llama al modelo. Cuesta cero y
   no depende del modelo.
2. **El modelo**: con `sabe` en `false` dice que los documentos no lo contienen. Y aunque diga que sí
   sabe, la respuesta se rechaza si cita una clave que no se le entregó.

> La API tiene citas nativas (documentos con `citations` activado), pero no se pueden combinar con
> salida estructurada: la petición devuelve 400. Por eso aquí cada fragmento lleva una clave propia,
> como `F12`, y el código valida que exista.

---

## Paso 7 - Los comandos (15 min)

La configuración sale del entorno, en `src/Asistente.Cli/Servicios.cs`:

```csharp
using Asistente.Core.Rag;

namespace Asistente.Cli;

/// <summary>La configuración sale del entorno. Nada de cadenas de conexión ni llaves en el código.</summary>
public static class Servicios
{
    public static string CadenaDeConexion() =>
        Environment.GetEnvironmentVariable("ASISTENTE_DB")
        ?? throw new InvalidOperationException("Falta la variable ASISTENTE_DB con la cadena de conexión de PostgreSQL.");

    public static IGeneradorEmbeddings Embeddings() =>
        Environment.GetEnvironmentVariable("ASISTENTE_EMBEDDINGS") == "hash"
            ? new EmbeddingsPorHash()
            : new EmbeddingsOllama(new HttpClient
            {
                BaseAddress = new Uri(Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://localhost:11434"),
                Timeout = TimeSpan.FromMinutes(2),
            });
}
```

El comando que indexa, en `src/Asistente.Cli/Comandos/Ingestar.cs`:

```csharp
using Asistente.Core.Rag;

namespace Asistente.Cli.Comandos;

/// <summary>Indexa todos los .md de una carpeta: fragmentar, generar vectores, guardar.</summary>
public static class Ingestar
{
    public static async Task<int> EjecutarAsync(string[] args, CancellationToken ct)
    {
        if (args.Length == 0) { Console.Error.WriteLine("Uso: ingestar <carpeta>"); return 1; }
        var carpeta = Path.GetFullPath(args[0]);
        var embeddings = Servicios.Embeddings();
        await using var fuente = AlmacenVectorial.CrearFuente(Servicios.CadenaDeConexion());
        var almacen = new AlmacenVectorial(fuente);
        await almacen.CrearEsquemaAsync(embeddings.Dimensiones, ct);

        int total = 0;
        foreach (var archivo in Directory.EnumerateFiles(carpeta, "*.md", SearchOption.AllDirectories).Order())
        {
            var relativa = Path.GetRelativePath(carpeta, archivo).Replace('\\', '/');
            var fragmentos = Fragmentador.Partir(relativa, await File.ReadAllTextAsync(archivo, ct));
            if (fragmentos.Count == 0) continue;
            var vectores = new List<float[]>();
            foreach (var lote in fragmentos.Chunk(16))
                vectores.AddRange(await embeddings.GenerarAsync(lote.Select(f => f.Contenido).ToList(), TipoTexto.Documento, ct));
            await almacen.ReemplazarFuenteAsync(relativa, fragmentos, vectores, ct);
            total += fragmentos.Count;
            Console.WriteLine($"{relativa}: {fragmentos.Count} fragmentos");
        }
        Console.WriteLine($"Listo: {total} fragmentos con {embeddings.Modelo} ({embeddings.Dimensiones} dimensiones).");
        return 0;
    }
}
```

El que pregunta, en `src/Asistente.Cli/Comandos/Preguntar.cs`:

```csharp
using Anthropic;
using Asistente.Core.Llm;
using Asistente.Core.Prompts;
using Asistente.Core.Rag;

namespace Asistente.Cli.Comandos;

public static class Preguntar
{
    public static async Task<int> EjecutarAsync(AnthropicClient cliente, string[] args, CancellationToken ct)
    {
        if (args.Length == 0) { Console.Error.WriteLine("Uso: preguntar <pregunta>"); return 1; }
        await using var fuente = AlmacenVectorial.CrearFuente(Servicios.CadenaDeConexion());
        var asistente = new AsistenteDocumentos(
            cliente,
            Servicios.Embeddings(),
            new AlmacenVectorial(fuente),
            RepositorioPrompts.DelRepositorio().Cargar("asistente-documentos", 1),
            Modelos.PorOmision,
            new OpcionesRag());

        var r = await asistente.PreguntarAsync(string.Join(' ', args), ct);
        Console.WriteLine(r.Texto);
        foreach (var f in r.Citadas) Console.WriteLine($"  [{f.Clave}] {f.Fuente} > {f.Seccion}");
        if (r.Motivo is not null) Console.WriteLine($"  (motivo: {r.Motivo})");
        Console.WriteLine("  Recuperados: " + string.Join(", ", r.Recuperadas.Select(f => $"{f.Clave} {f.Distancia:0.000}")));
        Console.WriteLine($"  Costo: {r.CostoUsd:0.000000} USD");
        return 0;
    }
}
```

Y regístralos en el `switch` de `src/Asistente.Cli/Program.cs`:

```csharp
    return args[0] switch
    {
        "comparar" => await Comparar.EjecutarAsync(cliente, args[1..], cancelar.Token),
        "extraer" => await Extraer.EjecutarAsync(cliente, args[1..], cancelar.Token),
        "ingestar" => await Ingestar.EjecutarAsync(args[1..], cancelar.Token),
        "preguntar" => await Preguntar.EjecutarAsync(cliente, args[1..], cancelar.Token),
        _ => Desconocido(args[0]),
    };
```

**Lo que debe pasar:** `dotnet build AsistenteCursos.slnx` con 0 errores y 0 warnings.

---

## Paso 8 - Ingestar el corpus (10 min)

El corpus son las notas de la unidad de vibe coding de dev-notes. Cópialas a `docs/`:

```bash
mkdir -p docs
cp <ruta-a-tu-clon-de-dev-notes>/11-vibe-coding/*.md docs/
dotnet run --project src/Asistente.Cli -- ingestar docs
```

**Lo que debe pasar:** una línea por archivo con su número de fragmentos y, al final, algo como
`Listo: N fragmentos con bge-m3 (1024 dimensiones).` Compruébalo en la base:

```bash
docker compose exec db psql -U asistente -d asistente \
  -c "SELECT fuente, count(*) FROM fragmentos GROUP BY fuente ORDER BY fuente;"
```

Corre `ingestar docs` una segunda vez: el conteo por fuente **no se duplica**, porque cada documento
reemplaza sus propios fragmentos.

---

## Paso 9 - Preguntar (15 min)

```bash
dotnet run --project src/Asistente.Cli -- preguntar "¿Por qué hay que versionar los prompts?"
```

**Lo que debe pasar:** una respuesta en español, debajo las fuentes citadas con su clave, archivo y
sección, la lista de lo recuperado con su distancia y el costo. La forma es esta (tus claves,
distancias y costo serán otros):

```text
<la respuesta del asistente>
  [F23] 05-llmops.md > Gestión y versionado de prompts
  Recuperados: F23 0.312, F24 0.356, F61 0.402, F8 0.447, F70 0.489
  Costo: 0.000412 USD
```

Haz tres preguntas más cuya respuesta sí esté en las notas y anota, para cada una, la distancia del
primer fragmento recuperado. Ese número es tu primera pista para calibrar el umbral.

---

## Paso 10 - Pruebas sin costo (15 min)

Las pruebas que necesitan base se marcan como omitidas si falta `ASISTENTE_DB`, nunca como aprobadas.
Actualiza `tests/Asistente.Pruebas/Requiere.cs`:

```csharp
using Npgsql;

namespace Asistente.Pruebas;

/// <summary>Lo que necesita red, dinero o una base se marca como omitido si falta, nunca como aprobado.</summary>
public static class Requiere
{
    public static void LlaveDeApi() =>
        Assert.SkipWhen(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")),
            "Sin ANTHROPIC_API_KEY: la evaluación no corrió.");

    public static string BaseDeDatos()
    {
        var cadena = Environment.GetEnvironmentVariable("ASISTENTE_DB");
        Assert.SkipWhen(string.IsNullOrEmpty(cadena), "Sin ASISTENTE_DB: la prueba de integración no corrió.");
        return cadena!;
    }

    /// <summary>
    /// Las pruebas de integración escriben en su propio esquema, "pruebas": sus vectores léxicos
    /// nunca se mezclan con los del índice real, que son de otro modelo.
    /// </summary>
    public static string BaseDePruebas()
    {
        var cadena = BaseDeDatos();
        using (var conexion = new NpgsqlConnection(cadena))
        {
            conexion.Open();
            // La extensión va en public para que la vean todos los esquemas.
            using var cmd = new NpgsqlCommand(
                "CREATE EXTENSION IF NOT EXISTS vector WITH SCHEMA public; CREATE SCHEMA IF NOT EXISTS pruebas;", conexion);
            cmd.ExecuteNonQuery();
        }
        return new NpgsqlConnectionStringBuilder(cadena) { SearchPath = "pruebas, public" }.ConnectionString;
    }
}

/// <summary>Las clases que tocan la base corren una tras otra, no en paralelo.</summary>
[CollectionDefinition("BaseDeDatos", DisableParallelization = true)]
public sealed class ColeccionBaseDeDatos;
```

Dos detalles de este archivo:

- `BaseDePruebas()` manda las pruebas de integración a su propio esquema, `pruebas`. Esas pruebas
  usan vectores léxicos; si los guardaran en la misma tabla que `ingestar` llena con bge-m3, la
  tabla tendría vectores de dos modelos y una pregunta real podría recuperar un fragmento de prueba.
  Las evaluaciones, en cambio, usan `BaseDeDatos()`: necesitan el índice real.
- La colección `BaseDeDatos` hace que las clases que tocan la base corran una tras otra. xUnit corre
  las clases en paralelo, y dos `CREATE TABLE IF NOT EXISTS` al mismo tiempo pueden chocar.

Las pruebas del fragmentador, en `tests/Asistente.Pruebas/FragmentadorPruebas.cs`:

```csharp
using Asistente.Core.Rag;

namespace Asistente.Pruebas;

public class FragmentadorPruebas
{
    [Fact]
    public void Parte_por_secciones_y_cada_fragmento_lleva_su_titulo()
    {
        var md = "# Nota\n\nIntro.\n\n## Costos\n\nEl costo se mide por token.\n\n## Latencia\n\nSe mide en milisegundos.";
        var fragmentos = Fragmentador.Partir("nota.md", md);
        Assert.Equal(["Nota", "Costos", "Latencia"], fragmentos.Select(f => f.Seccion));
        Assert.StartsWith("Costos", fragmentos[1].Contenido);
    }

    [Fact]
    public void Un_encabezado_dentro_de_un_bloque_de_codigo_no_parte_la_seccion()
    {
        var md = "## Ejemplo\n\n```bash\n# esto es un comentario de bash\necho hola\n```\n";
        var fragmentos = Fragmentador.Partir("nota.md", md);
        Assert.Single(fragmentos);
        Assert.Contains("# esto es un comentario de bash", fragmentos[0].Contenido);
    }

    [Fact]
    public void Una_seccion_larga_se_parte_sin_perder_texto_y_con_solape()
    {
        var parrafos = Enumerable.Range(1, 30).Select(i => $"Párrafo {i}: " + new string('x', 100)).ToList();
        var md = "## Larga\n\n" + string.Join("\n\n", parrafos);
        var fragmentos = Fragmentador.Partir("nota.md", md, maxCaracteres: 600, solape: 120);

        Assert.True(fragmentos.Count > 1);
        foreach (var p in parrafos)
            Assert.Contains(fragmentos, f => f.Contenido.Contains(p));
        Assert.All(fragmentos, f => Assert.True(f.Contenido.Length <= 600 + 120 + "Larga\n\n".Length));
    }
}
```

Y la de integración contra pgvector, con vectores léxicos: sin modelo y sin costo.

```csharp
using Asistente.Core.Rag;

namespace Asistente.Pruebas;

/// <summary>Contra un PostgreSQL con pgvector de verdad, con vectores léxicos: sin modelo y sin costo.</summary>
[Trait("Categoria", "Integracion")]
[Collection("BaseDeDatos")]
public class AlmacenVectorialIntegracion
{
    [Fact]
    public async Task Guarda_y_recupera_primero_el_fragmento_mas_parecido()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fuente = AlmacenVectorial.CrearFuente(Requiere.BaseDePruebas());
        var almacen = new AlmacenVectorial(fuente);
        var embeddings = new EmbeddingsPorHash();
        await almacen.CrearEsquemaAsync(embeddings.Dimensiones, ct);

        var fuenteDoc = $"prueba-{Guid.NewGuid():N}.md";
        var fragmentos = Fragmentador.Partir(fuenteDoc,
            "## Índices\n\nUn índice GIN acelera búsquedas de texto completo y arreglos.\n\n" +
            "## Respaldos\n\nEl respaldo diario se guarda siete días en el almacenamiento de objetos.");
        var vectores = await embeddings.GenerarAsync(fragmentos.Select(f => f.Contenido).ToList(), TipoTexto.Documento, ct);
        await almacen.ReemplazarFuenteAsync(fuenteDoc, fragmentos, vectores, ct);

        var consulta = (await embeddings.GenerarAsync(["¿Cuántos días se guarda el respaldo diario?"], TipoTexto.Consulta, ct))[0];
        var encontrados = await almacen.BuscarAsync(consulta, 5, ct);

        var primero = encontrados.First(f => f.Fuente == fuenteDoc);
        Assert.Equal("Respaldos", primero.Seccion);
        await almacen.ReemplazarFuenteAsync(fuenteDoc, [], [], ct);   // limpia lo que sembró
    }
}
```

```bash
dotnet test --solution AsistenteCursos.slnx -- --filter-not-trait "Categoria=Eval"
```

**Lo que debe pasar:** todas en verde, incluida `Guarda_y_recupera_primero_el_fragmento_mas_parecido`
(revisa que diga `succeeded` y no `skipped`: si dice `skipped`, te falta exportar `ASISTENTE_DB`).

---

## Paso 11 - Verlo fallar (20 min)

Regla del curso: **si no lo viste fallar, no lo probaste.** Tres fallas que tienes que provocar.

**1. La pregunta que no tiene respuesta.**

```bash
dotnet run --project src/Asistente.Cli -- preguntar "¿Cuál es la capital de Australia?"
dotnet run --project src/Asistente.Cli -- preguntar "¿Cuántos alumnos se inscribieron al curso el año pasado?"
```

Las dos deben responder `No lo sé: eso no está en los documentos del curso.` Anota el motivo de
cada una. Si el motivo es el umbral, el costo es `0.000000 USD`: no hubo llamada al modelo.

**2. Quitar la primera capa.** Cambia temporalmente las opciones en `Preguntar.cs`:

```diff
-            new OpcionesRag());
+            new OpcionesRag(DistanciaMaxima: 2.0));
```

Con 2.0 todo pasa el umbral (la distancia coseno va de 0 a 2), así que el modelo recibe los cinco
fragmentos más cercanos aunque no tengan nada que ver. Repite las dos preguntas. Ahora el costo ya no
es cero y la decisión depende del modelo: anota si respondió `sabe: false` (la segunda capa
funcionó) o si contestó con lo que sabe por su cuenta. Lo segundo es exactamente el error que la
evaluación tiene que atrapar. **Regresa el cambio** al terminar.

**3. Romper la búsqueda y ver caer la prueba.** Invierte el orden de la consulta en
`AlmacenVectorial.BuscarAsync`:

```diff
-            ORDER BY embedding <=> @consulta
+            ORDER BY embedding <=> @consulta DESC
```

Corre otra vez las pruebas sin costo. **Lo que debe pasar:**
`Guarda_y_recupera_primero_el_fragmento_mas_parecido` falla con
`InvalidOperationException: Sequence contains no matching element`: el fragmento correcto ya ni
siquiera aparece entre los cinco recuperados. Regresa el cambio y confirma que vuelve a verde.

---

## Paso 12 - La evaluación con casos (15 min)

Los casos, uno por línea, en `casos/rag.jsonl`. Los que tienen `"sabe": false` son igual de
importantes que los otros:

```json
{"id":"rag-01","pregunta":"¿Por qué hay que versionar los prompts?","sabe":true,"fuente":"05-llmops.md"}
{"id":"rag-02","pregunta":"¿Qué es un guardrail en una aplicación con LLM?","sabe":true,"fuente":"05-llmops.md"}
{"id":"rag-03","pregunta":"¿Qué diferencia hay entre una regla y una skill en el catálogo de agentes?","sabe":true,"fuente":"06-arquitectura-agentica.md"}
{"id":"rag-04","pregunta":"¿Cuál es la capital de Australia?","sabe":false}
{"id":"rag-05","pregunta":"¿Cuántos alumnos se inscribieron al curso el año pasado?","sabe":false}
```

La evaluación, en `tests/Asistente.Pruebas/EvalRag.cs`. Cuenta como acierto decir "no lo sé" cuando
toca, y responder **citando el archivo esperado** cuando sí está:

```csharp
using System.Text.Json;
using Anthropic;
using Asistente.Core;
using Asistente.Core.Llm;
using Asistente.Core.Prompts;
using Asistente.Core.Rag;

namespace Asistente.Pruebas;

public sealed record CasoRag(string Id, string Pregunta, bool Sabe, string? Fuente);

/// <summary>Tasa de aciertos sobre casos/rag.jsonl. Necesita llave, base indexada y Ollama.</summary>
[Trait("Categoria", "Eval")]
[Collection("BaseDeDatos")]
public class EvalRag(ITestOutputHelper salida)
{
    private const double Umbral = 0.85;

    [Fact]
    public async Task El_asistente_acierta_y_sabe_decir_no_lo_se()
    {
        Requiere.LlaveDeApi();
        var ct = TestContext.Current.CancellationToken;
        await using var fuente = AlmacenVectorial.CrearFuente(Requiere.BaseDeDatos());
        var embeddings = new EmbeddingsOllama(new HttpClient
        {
            BaseAddress = new Uri(Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://localhost:11434"),
        });
        var prompt = RepositorioPrompts.DelRepositorio().Cargar("asistente-documentos", 1);
        var asistente = new AsistenteDocumentos(new AnthropicClient(), embeddings, new AlmacenVectorial(fuente),
            prompt, Modelos.PorOmision, new OpcionesRag());

        var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var casos = File.ReadLines(Path.Combine(Rutas.Buscar("casos"), "rag.jsonl"))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => JsonSerializer.Deserialize<CasoRag>(l, opciones)!)
            .ToList();

        int aciertos = 0;
        foreach (var caso in casos)
        {
            var r = await asistente.PreguntarAsync(caso.Pregunta, ct);
            bool ok = r.Sabe == caso.Sabe
                && (!caso.Sabe || r.Citadas.Any(f => f.Fuente.EndsWith(caso.Fuente!, StringComparison.Ordinal)));
            if (ok) aciertos++;
            else salida.WriteLine($"FALLA {caso.Id}: sabe={r.Sabe}, citó {string.Join(",", r.Citadas.Select(f => f.Fuente))}, motivo {r.Motivo}");
        }
        double tasa = (double)aciertos / casos.Count;
        salida.WriteLine($"{prompt.Id} ({prompt.Huella}): {aciertos}/{casos.Count} = {tasa:P0}");
        Assert.True(tasa >= Umbral, $"La tasa bajó a {tasa:P0}; el umbral es {Umbral:P0}.");
    }
}
```

```bash
dotnet test --solution AsistenteCursos.slnx -- --filter-trait "Categoria=Eval"
```

**Lo que debe pasar:** la prueba imprime la versión del prompt, su huella y la tasa (algo como
`5/5 = 100%`) y pasa si es de 85 % o más. Con cinco casos, un solo error ya la tumba: por eso la tarea los sube a 20.

---

## Evidencia que entregas

- [ ] Captura de `docker compose ps` con `db` y `ollama` corriendo, y del conteo de fragmentos por fuente.
- [ ] La salida de `preguntar` para cuatro preguntas que sí están en las notas, con sus fuentes citadas.
- [ ] La salida de las dos preguntas sin respuesta, con su motivo y su costo.
- [ ] Lo que pasó con `DistanciaMaxima: 2.0`: qué respondió el modelo en cada caso y cuánto costó.
- [ ] La salida de la prueba de integración fallando con el `DESC` y en verde sin él.
- [ ] La salida de `EvalRag` con la tasa de aciertos.
- [ ] El commit con todo lo anterior, sin `.env` ni llaves en el diff (`git diff --cached` antes de confirmar).
