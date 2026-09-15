using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Infrastructure.Identity;

/// <summary>
/// Modelo roles acordes al dominio y vincula personas del planificador con usuarios Identity.
/// </summary>
public static class IdentitySeeder
{
    public const string DeveloperRole = "Developer";
    public const string TeamLeadRole = "TeamLead";
    public const string QaRole = "QA";

    private static readonly string[] Roles = { DeveloperRole, TeamLeadRole, QaRole };

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
}