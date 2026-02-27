using System;
using System.Collections.Generic;

namespace LightestDungeonCreator.Model;

public partial class Enemy
{
    public int EntityId { get; set; }

    public int PassiveId { get; set; }

    public virtual Entity Entity { get; set; } = null!;

    public virtual ICollection<Loottable> Loottables { get; set; } = new List<Loottable>();
}
