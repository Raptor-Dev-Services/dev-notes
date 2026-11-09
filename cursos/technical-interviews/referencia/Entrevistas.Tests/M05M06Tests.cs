using Entrevistas.M05;
using Entrevistas.M06;
using Microsoft.Extensions.DependencyInjection;

namespace Entrevistas.Tests;

public class PreguntasCSharpTests
{
    [Fact]
    public void P01_valor_y_referencia()
    {
        Assert.Equal((1, 99), P01.ConStruct());
        Assert.Equal((99, 99), P01.ConClase());
    }

    [Fact]
    public void P02_record_compara_por_valor()
    {
        var (iguales, misma, copia) = P02.Comparar();
        Assert.True(iguales);
        Assert.False(misma);
        Assert.Equal(new Dinero("MXN", 250m), copia);
    }

    [Fact]
    public void P03_string_compara_contenido()
        => Assert.Equal((true, false), P03.Comparar());

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(25)]
    public void P04_las_dos_formas_dan_el_mismo_texto(int n)
    {
        Assert.Equal(P04.UnirConcatenando(n), P04.UnirConBuilder(n));
        Assert.Equal(n, P04.UnirConBuilder(n).Length);
    }

    [Fact]
    public async Task P05_una_tras_otra_nunca_hay_dos_a_la_vez()
        => Assert.Equal(1, await P05.UnaTrasOtraAsync(new Medidor()));

    [Fact]
    public async Task P05_whenall_las_tres_esperan_juntas()
        => Assert.Equal(3, await P05.JuntasAsync(new Medidor()));

    [Fact]
    public async Task P05_el_tiempo_total_sigue_al_numero_de_esperas_simultaneas()
    {
        var reloj = System.Diagnostics.Stopwatch.StartNew();
        await P05.UnaTrasOtraAsync(new Medidor());
        long secuencial = reloj.ElapsedMilliseconds;
        reloj.Restart();
        await P05.JuntasAsync(new Medidor());
        long juntas = reloj.ElapsedMilliseconds;
        Assert.InRange(secuencial, 290, 2000); // tres esperas de 100 ms, una tras otra
        Assert.InRange(juntas, 95, 280);       // las tres esperas se enciman
    }

    [Fact]
    public async Task P06_await_entrega_la_excepcion_original()
    {
        Assert.Equal("falló el cálculo", await P06.ConAwaitAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(P06.CalcularAsync);
    }

    [Fact]
    public void P07_result_la_envuelve_en_AggregateException()
    {
        var ex = P07.ConResult();
        var agregada = Assert.IsType<AggregateException>(ex);
        Assert.IsType<InvalidOperationException>(agregada.InnerException);
    }

    [Fact]
    public async Task P08_cancelar()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => P08.EsperarAsync(TimeSpan.FromSeconds(30), cts.Token));
        Assert.IsType<TaskCanceledException>(ex);
    }

    [Fact]
    public async Task P08_sin_cancelar_termina()
        => await P08.EsperarAsync(TimeSpan.FromMilliseconds(1), CancellationToken.None);

    [Fact]
    public void P09_tiempos_de_vida()
    {
        using var proveedor = P09.Construir();
        Guid singletonA, scopedA1, scopedA2, scopedB;
        using (var alcanceA = proveedor.CreateScope())
        {
            singletonA = alcanceA.ServiceProvider.GetRequiredService<ServicioSingleton>().Id;
            scopedA1 = alcanceA.ServiceProvider.GetRequiredService<ServicioScoped>().Id;
            scopedA2 = alcanceA.ServiceProvider.GetRequiredService<ServicioScoped>().Id;
            var t1 = alcanceA.ServiceProvider.GetRequiredService<ServicioTransient>().Id;
            var t2 = alcanceA.ServiceProvider.GetRequiredService<ServicioTransient>().Id;
            Assert.NotEqual(t1, t2);                 // transient: uno nuevo cada vez
        }
        using (var alcanceB = proveedor.CreateScope())
        {
            Assert.Equal(singletonA, alcanceB.ServiceProvider.GetRequiredService<ServicioSingleton>().Id);
            scopedB = alcanceB.ServiceProvider.GetRequiredService<ServicioScoped>().Id;
        }
        Assert.Equal(scopedA1, scopedA2);            // scoped: el mismo dentro del alcance
        Assert.NotEqual(scopedA1, scopedB);          // y otro en el siguiente alcance
    }

    [Fact]
    public void P10_dependencia_cautiva_se_detecta()
    {
        using var proveedor = P10.ConValidacion();
        var ex = Assert.Throws<InvalidOperationException>(() => proveedor.GetRequiredService<CacheGlobal>());
        Assert.Contains("scoped", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void P10_sin_validacion_el_singleton_captura_al_scoped()
    {
        var servicios = new ServiceCollection();
        servicios.AddScoped<Repositorio>();
        servicios.AddSingleton<CacheGlobal>();
        using var proveedor = servicios.BuildServiceProvider(); // validación apagada
        Repositorio deA, deB;
        using (var a = proveedor.CreateScope()) deA = a.ServiceProvider.GetRequiredService<CacheGlobal>().Repo;
        using (var b = proveedor.CreateScope()) deB = b.ServiceProvider.GetRequiredService<CacheGlobal>().Repo;
        Assert.Same(deA, deB); // el "scoped" quedó vivo para siempre dentro del singleton
    }

    [Fact]
    public void P11_restriccion_generica()
    {
        Assert.Equal(9, P11.Mayor([3, 9, 2]));
        Assert.Equal("pera", P11.Mayor(["kiwi", "pera", "fresa"]));
        Assert.Equal(new DateOnly(2026, 11, 8), P11.Mayor([new DateOnly(2025, 1, 1), new DateOnly(2026, 11, 8)]));
        Assert.Throws<InvalidOperationException>(() => P11.Mayor(Array.Empty<int>()));
    }

    [Fact]
    public void P12_varianza()
    {
        Assert.Equal(2, P12.Contar());
        Assert.Equal("ok", P12.Contravarianza());
    }

    [Fact]
    public void P13_nullable_de_valor_y_de_referencia()
    {
        Assert.Equal((true, typeof(string)), P13.Revisar());
        Assert.Equal(0, P13.Longitud(null));
        Assert.Equal(4, P13.Longitud("hola"));
    }

    [Fact]
    public void P14_el_signo_de_admiracion_no_protege()
    {
        Assert.Equal(4, P14.LongitudForzada("hola"));
        Assert.Throws<NullReferenceException>(() => P14.LongitudForzada(null));
    }

    [Fact]
    public void P15_ejecucion_diferida() => Assert.Equal(3, P15.Contar());

    [Fact]
    public void P16_iqueryable_guarda_un_arbol()
        => Assert.Equal(("Where", typeof(Func<int, bool>)), P16.Inspeccionar());

    [Fact]
    public void P17_first_y_single()
    {
        Assert.Equal(8, P17.Primero());
        Assert.Equal(0, P17.PrimeroOCero());
        Assert.Throws<InvalidOperationException>(() => P17.Unico());
    }

    [Fact]
    public void P18_throw_conserva_el_rastro_y_throw_ex_lo_pierde()
    {
        Assert.Contains("Fallar", P18.ConThrow(), StringComparison.Ordinal);
        Assert.DoesNotContain("Fallar", P18.ConThrowEx(), StringComparison.Ordinal);
    }

    [Fact]
    public void P19_using_libera_aunque_falle()
    {
        var conexion = new Conexion();
        Assert.Throws<InvalidOperationException>(() => P19.UsarYFallar(conexion));
        Assert.True(conexion.Cerrada);
    }

    [Fact]
    public void P20_cierres_en_ciclos()
    {
        Assert.Equal([3, 3, 3], P20.ConFor());
        Assert.Equal([0, 1, 2], P20.ConForeach());
    }
}

public class SistemasTests
{
    [Theory]
    [InlineData(0L, "0")]
    [InlineData(9L, "9")]
    [InlineData(10L, "a")]
    [InlineData(61L, "Z")]
    [InlineData(62L, "10")]
    [InlineData(3_843L, "ZZ")]
    [InlineData(3_521_614_606_207L, "ZZZZZZZ")]
    [InlineData(long.MaxValue, "aZl8N0y58M7")]
    public void Base62_codifica(long id, string esperado)
    {
        Assert.Equal(esperado, Base62.Codificar(id));
        Assert.Equal(id, Base62.Decodificar(esperado));
    }

    [Fact]
    public void Base62_ida_y_vuelta_y_longitudes()
    {
        var azar = new Random(2);
        for (int i = 0; i < 10_000; i++)
        {
            long id = azar.NextInt64(0, long.MaxValue);
            Assert.Equal(id, Base62.Decodificar(Base62.Codificar(id)));
        }
        Assert.Equal(7, Base62.Codificar((long)Math.Pow(62, 6)).Length);       // el primero de 7
        Assert.Equal(6, Base62.Codificar((long)Math.Pow(62, 6) - 1).Length);   // el último de 6
        Assert.Equal(56_800_235_584L, (long)Math.Pow(62, 6));
        Assert.Equal(3_521_614_606_208L, (long)Math.Pow(62, 7));
    }

    [Fact]
    public void Base62_rechaza_lo_invalido()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Base62.Codificar(-1));
        Assert.Throws<FormatException>(() => Base62.Decodificar(""));
        Assert.Throws<FormatException>(() => Base62.Decodificar("ab-c"));
        Assert.Throws<OverflowException>(() => Base62.Decodificar("ZZZZZZZZZZZZ"));
    }

    [Fact]
    public void Consumidor_idempotente_no_cuenta_dos_veces()
    {
        var consumidor = new ConsumidorIdempotente();
        var id = Guid.NewGuid();
        Assert.True(consumidor.Procesar(id));
        Assert.False(consumidor.Procesar(id));   // la cola lo entregó otra vez
        Assert.True(consumidor.Procesar(Guid.NewGuid()));
        Assert.Equal(2, consumidor.Clics);
    }

    [Fact]
    public void Consumidor_idempotente_bajo_concurrencia()
    {
        var consumidor = new ConsumidorIdempotente();
        var ids = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToArray();
        Parallel.For(0, 5000, i => consumidor.Procesar(ids[i % ids.Length])); // cada id llega 10 veces
        Assert.Equal(500, consumidor.Clics);
    }

    [Fact]
    public void Agenda_solo_un_cliente_gana_el_mismo_horario()
    {
        var agenda = new AgendaEnMemoria();
        var horario = new DateTime(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc);
        int ganadores = 0;
        Parallel.For(0, 200, i =>
        {
            if (agenda.TryReservar(1, horario, $"cliente-{i}"))
                Interlocked.Increment(ref ganadores);
        });
        Assert.Equal(1, ganadores);
        Assert.StartsWith("cliente-", agenda.Quien(1, horario), StringComparison.Ordinal);
        Assert.True(agenda.TryReservar(2, horario, "otro"));                 // otro recurso, mismo horario
        Assert.True(agenda.TryReservar(1, horario.AddHours(1), "otro"));     // mismo recurso, otro horario
        Assert.Null(agenda.Quien(3, horario));
    }
}
