using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Domain.Entities;

public class Organization : BaseEntity
{
    public string Name { get; set; }

    public OrganizationType Type { get; set; } = OrganizationType.Teacher;

    public string SubscriptionPlan { get; set; } = "Free";

    public DateTime? SubscriptionStart { get; set; }

    public DateTime? SubscriptionEnd { get; set; }

    public ICollection<Class> Classes { get; set; } = new List<Class>();
}
