namespace Asistente.Core.Llm;

/// <summary>Lo que costó y tardó una llamada. Se registra en cada una, no solo cuando falla.</summary>
public sealed record Medicion(
    string Modelo,
    long TokensEntrada,
    long TokensSalida,
    long TokensCacheLeidos,
    long TokensCacheEscritos,
    decimal CostoUsd,
    TimeSpan PrimerToken,
    TimeSpan Total,
    string StopReason);
