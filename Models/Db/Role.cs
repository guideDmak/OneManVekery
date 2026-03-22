using System;
using System.Collections.Generic;

namespace OneManVekery.Models.Db;

public partial class Role
{
    public int Id { get; set; }

    public string RoleKey { get; set; } = null!;

    public string RoleName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
