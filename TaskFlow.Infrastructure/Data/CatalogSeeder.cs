using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Infrastructure.Data;

/// <summary>
/// Siembra el catálogo de cargos y hace el backfill de los cargos de las personas
/// a partir de los TaskGroups históricos. Ese backfill es obligatorio: sin él, la
/// validación estricta del TXT rechazaría todos los archivos existentes porque
/// ninguna persona tendría el cargo que el archivo le exige.
/// </summary>
public static class CatalogSeeder
{
    public const string DevCargo = "Dev";
    public const string TeamLeadCargo = "Team Lead";
    public const string QaCargo = "QA";

    private static readonly string[] DefaultCargos = { DevCargo, TeamLeadCargo, QaCargo };

    public static async Task SeedAsync(IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        var cargos = new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in DefaultCargos)
        {
            var role = await unitOfWork.Roles.GetByNameCaseInsensitiveAsync(name);

            if (role == null)
            {
                role = new Role { Name = name, IsActive = true };
                await unitOfWork.Roles.AddAsync(role);
            }
            else if (!role.IsActive)
            {
                role.IsActive = true;
                await unitOfWork.Roles.UpdateAsync(role);
            }

            cargos[name] = role;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await BackfillPersonRolesAsync(unitOfWork, cargos, cancellationToken);
    }

    private static async Task BackfillPersonRolesAsync(IUnitOfWork unitOfWork,
        Dictionary<string, Role> cargos,
        CancellationToken cancellationToken)
    {
        var taskGroups = await unitOfWork.TaskGroups.GetAllAsync();

        // Recolecta el cargo que cada persona ha ejercido históricamente, según los
        // bloques del TXT que ya están cargados.
        var derived = new Dictionary<Guid, HashSet<Guid>>();

        void Add(Guid personId, string cargo)
        {
            if (personId == Guid.Empty)
            {
                return;
            }

            if (!derived.TryGetValue(personId, out var set))
            {
                set = new HashSet<Guid>();
                derived[personId] = set;
            }

            set.Add(cargos[cargo].Id);
        }

        foreach (var group in taskGroups)
        {
            if (group.DevPersonId.HasValue) Add(group.DevPersonId.Value, DevCargo);
            if (group.TeamLeadPersonId.HasValue) Add(group.TeamLeadPersonId.Value, TeamLeadCargo);
            if (group.QaPersonId.HasValue) Add(group.QaPersonId.Value, QaCargo);
        }

        if (derived.Count == 0)
        {
            return;
        }

        var people = await unitOfWork.People.GetAllAsync();
        var knownPeople = people.Select(p => p.Id).ToHashSet();
        var added = 0;

        foreach (var (personId, roleIds) in derived)
        {
            if (!knownPeople.Contains(personId))
            {
                continue;
            }

            var current = (await unitOfWork.PersonRoles.GetRoleIdsByPersonAsync(personId)).ToHashSet();

            foreach (var roleId in roleIds.Except(current))
            {
                await unitOfWork.PersonRoles.AddAsync(new PersonRole
                {
                    PersonId = personId,
                    RoleId = roleId
                });
                added++;
            }
        }

        if (added > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
