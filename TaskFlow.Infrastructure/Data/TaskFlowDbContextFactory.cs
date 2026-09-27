using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TaskFlow.Infrastructure.Data;

/// <summary>
/// Permite ejecutar las herramientas de EF Core contra Infrastructure sin arrancar
/// TaskFlow.Api. Necesario porque la API suele estar corriendo y bloquea el copiado
/// de las DLL en su propia carpeta bin.
/// </summary>
public class TaskFlowDbContextFactory : IDesignTimeDbContextFactory<TaskFlowDbContext>
{
    public TaskFlowDbContext CreateDbContext(string[] args)
    {
        var configPath = FindConfigurationFile(Directory.GetCurrentDirectory())
            ?? throw new InvalidOperationException(
                "No se encontró 'appsettings.json' con la cadena 'DefaultConnection' " +
                "en el proyecto actual ni en ninguno de sus directorios padre.");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                $"No se encontró la cadena de conexión 'DefaultConnection' en '{configPath}'.");

        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TaskFlowDbContext(options);
    }

    /// <summary>
    /// La herramienta de EF se puede ejecutar desde el proyecto Infrastructure, desde
    /// la raíz de la solución o desde la API, así que se busca hacia arriba.
    /// </summary>
    private static string? FindConfigurationFile(string startPath)
    {
        var current = new DirectoryInfo(startPath);

        while (current != null)
        {
            foreach (var relative in new[] { "appsettings.json", Path.Combine("TaskFlow.Api", "appsettings.json") })
            {
                var candidate = Path.Combine(current.FullName, relative);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            current = current.Parent;
        }

        return null;
    }
}
