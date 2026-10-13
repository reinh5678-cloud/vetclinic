using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string PermissionsJson { get; set; } = string.Empty;
    public ICollection<User> Users { get; set; } = new List<User>();
}