namespace ApiSeguridad;

/// <summary>
/// Interruptores del laboratorio. Cada uno reintroduce, a proposito, una falla clasica en ESTA aplicacion
/// de practica para que la prueba correspondiente se vea fallar. Todos arrancan apagados: el codigo
/// seguro es el comportamiento por omision. No existe nada parecido en una aplicacion real.
/// </summary>
public sealed class LabOpciones
{
    public bool Bola { get; set; }               // modulo 3: sin comprobar de quien es el pedido
    public bool Bopla { get; set; }              // modulo 3: el cliente puede escribir propiedades que no le tocan
    public bool Bfla { get; set; }               // modulo 3: funcion de administracion sin politica
    public bool EntidadCompleta { get; set; }    // modulo 5: la respuesta devuelve la entidad, no un DTO
    public bool SqlConcatenado { get; set; }     // modulo 4: consulta armada con texto concatenado
    public bool ErroresDetallados { get; set; }  // modulo 4: la excepcion se devuelve tal cual
    public bool SinTopeDePagina { get; set; }    // modulo 4: el cliente decide cuantos registros pide
    public bool CuponRepetible { get; set; }     // modulo 5: el descuento se puede aplicar varias veces
    public bool SinListaDeDestinos { get; set; } // modulo 5: la API visita cualquier URL que le den
    public bool RutaOlvidada { get; set; }       // modulo 1: queda una ruta de depuracion sin documentar
    public bool ProvocarFalla { get; set; }      // modulo 4: el reporte falla para poder ver el manejo de errores
}
