using System;
using System.Collections.Generic;

namespace LightestDungeonCreator.Model;

public partial class Loottable
{
    public int Id { get; set; }

    public int EnemyId { get; set; }

    public virtual Enemy Enemy { get; set; } = null!;

    public virtual ICollection<Lootentry> Lootentries { get; set; } = new List<Lootentry>();
}
