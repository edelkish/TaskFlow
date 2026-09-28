using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(120)
            .UseCollation("Latin1_General_CI_AI");

        builder.Property(p => p.LastName)
            .HasMaxLength(120)
            .UseCollation("Latin1_General_CI_AI");

        builder.Property(p => p.UserName)
            .HasMaxLength(120)
            .UseCollation("Latin1_General_CI_AI");

        builder.Property(p => p.UserId)
            .HasMaxLength(450);

        // La identidad de una persona es el par (Apellidos, Nombre): por eso dos personas
        // pueden llamarse igual mientras los apellidos las distingan.
        //
        // En SQL Server un indice unico trata los NULL como iguales, asi que (Angel, NULL)
        // solo admite una fila. Es intencional y es la garantia que ya existia cuando el
        // indice era sobre Name: no se admiten homonimos sin apellidos. Con apellidos si se
        // admiten ("(Angel, Perez)" y "(Angel, Soto)" conviven).
        //
        // LastName se marca como required solo a efectos de configuracion del indice, para
        // que EF no anada automaticamente el filtro "[LastName] IS NOT NULL". Ese filtro
        // permitiria (Angel, NULL) junto a (Angel, Soto), que es justo lo que la regla
        // anterior impide. La columna sigue siendo nullable en la migracion: las personas
        // sin apellidos son validas.
        builder.HasIndex(p => new { p.LastName, p.Name })
            .IsUnique()
            .HasFilter(null);

        // Efecto secundario de lo anterior: hay que restaurar la columna a nullable, que es
        // como está en la entidad. Se hace fuera de la cadena de índices para que la
        // propiedad quede declarada una sola vez y en su sitio.
        builder.Property(p => p.LastName).IsRequired(false);

        // El Usuario si es clave natural, pero no puede ser obligatorio: la mayoria de
        // personas no tienen login de Identity. El filtro es imprescindible, porque sin
        // el un indice unico sobre una columna nullable solo admite una unica fila NULL.
        builder.HasIndex(p => p.UserName)
            .IsUnique()
            .HasFilter("[UserName] IS NOT NULL");
    }
}