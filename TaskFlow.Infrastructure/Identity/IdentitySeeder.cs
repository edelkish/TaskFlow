using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Infrastructure.Identity;

/// <summary>
/// Modelo roles acordes al dominio: crea roles, siembra el administrador del sistema
/// y un usuario por rol, y vincula personas del planificador con usuarios Identity.
/// </summary>
public static class IdentitySeeder
{
    public const string AdminRole = "Admin";
    public const string DeveloperRole = "Developer";
    public const string TeamLeadRole = "TeamLead";
    public const string QaRole = "QA";

    private static readonly string[] Roles = { AdminRole, DeveloperRole, TeamLeadRole, QaRole };

    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Usuario administrador con acceso total (todos los roles).
        await EnsureUserAsync(userManager, "admin@taskflow.local", "@Admin123/*-+",
            new[] { AdminRole, DeveloperRole, TeamLeadRole, QaRole });

        // Un usuario por cada rol operativo.
        await EnsureUserAsync(userManager, "developer@taskflow.local", "@User123/*-+", new[] { DeveloperRole });
        await EnsureUserAsync(userManager, "teamlead@taskflow.local", "@User123/*-+", new[] { TeamLeadRole });
        await EnsureUserAsync(userManager, "qa@taskflow.local", "@User123/*-+", new[] { QaRole });

        // Vinculación de People (nombre exacto) con usuarios Identity registrados.
        var people = await unitOfWork.People.GetAllAsync();
        var users = userManager.Users.ToList();

        foreach (var person in people)
        {
            if (!string.IsNullOrWhiteSpace(person.UserId))
            {
                continue;
            }

            var normalizedName = userManager.NormalizeName(person.Name.Trim());
            var match = users.FirstOrDefault(u =>
                u.NormalizedUserName == normalizedName
                || string.Equals(u.Email?.Trim(), person.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                person.UserId = match.Id;
                await unitOfWork.People.UpdateAsync(person);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static async Task EnsureUserAsync(UserManager<IdentityUser> userManager,
        string email, string password, string[] roles)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    $"No se pudo crear el usuario seed '{email}': {string.Join("; ", created.Errors.Select(e => e.Description))}");
            }
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var missing = roles.Where(r => !currentRoles.Contains(r)).ToList();
        if (missing.Count > 0)
        {
            await userManager.AddToRolesAsync(user, missing);
        }
    }
}