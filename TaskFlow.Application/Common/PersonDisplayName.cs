namespace TaskFlow.Application.Common;

/// <summary>
/// Formato unico para mostrar a una persona. Existe como metodo estatico para que el
/// nombre completo se componga en un solo sitio: lo usan los DTOs, las proyecciones de
/// AutoMapper y la interfaz, y asi ningun selector se queda mostrando solo el nombre.
/// </summary>
public static class PersonDisplayName
{
    /// <summary>
    /// Devuelve "Nombre Apellidos", o solo "Nombre" cuando la persona no tiene apellidos.
    /// Las personas sin apellidos son legitimas: el TXT de planificacion solo trae un
    /// token por persona y las filas historicas se crearon antes de que existiera el campo.
    /// </summary>
    public static string For(string? name, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName.Trim();
        }

        return string.IsNullOrWhiteSpace(lastName)
            ? name.Trim()
            : $"{name.Trim()} {lastName.Trim()}";
    }
}
