using Microsoft.AspNetCore.Identity;

namespace TeacherDiary.Infrastructure.Auth;

public class AppUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public Guid? OrganizationId { get; set; }
}
