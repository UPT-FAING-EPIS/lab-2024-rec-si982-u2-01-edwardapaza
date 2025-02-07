using Microsoft.EntityFrameworkCore;

namespace Shorten.Areas.Domain;
/// <summary>
/// Clase de infraestructura que representa el contexto de la base de datos
/// </summary>
public class ShortenContext : DbContext
{
    /// <summary>
    /// Constructor de la clase
    /// </summary>
    /// <param name="options">opciones de conexiòn de BD</param>
    public ShortenContext(DbContextOptions<ShortenContext> options) : base(options)
    {
    }
  
    /// <summary>
    /// Propiedad que representa la tabla de mapeo de urls
    /// </summary>
    /// <value>Conjunto de UrlMapping</value>
    public DbSet<UrlMapping> UrlMappings { get; set; }
}