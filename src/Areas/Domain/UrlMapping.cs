namespace Shorten.Areas.Domain;
/// <summary>
/// Clase de dominio que representa una acortaciòn de url
/// </summary>
public class UrlMapping
{
    /// <summary>
    /// Identificador del mapeo de url
    /// </summary>
    /// <value>Entero</value>
    public int Id { get; set; }
    /// <summary>
    /// Valor original de la url
    /// </summary>
    /// <value>Cadena</value>
    public string OriginalUrl { get; set; } = string.Empty;
    /// <summary>
    /// Valor corto de la url
    /// </summary>
    /// <value>Cadena</value>
    public string ShortenedUrl { get; set; } = string.Empty;
}