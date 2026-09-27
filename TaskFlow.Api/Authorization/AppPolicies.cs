namespace TaskFlow.Api.Authorization;

/// <summary>
/// Políticas de autorización derivadas de las decisiones de negocio:
/// los maestros se curan aparte de la importación, y Developer no importa.
/// </summary>
public static class AppPolicies
{
    /// <summary>Curaduría de maestros: Admin y Planificación.</summary>
    public const string MasterDataWrite = nameof(MasterDataWrite);

    /// <summary>Importación y armado de TXT: Admin, Planificación, TeamLead y QA. Developer queda excluido.</summary>
    public const string ImportWrite = nameof(ImportWrite);
}
