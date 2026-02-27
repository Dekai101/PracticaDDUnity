using System;
using System.Collections.Generic;

namespace LightestDungeonCreator.Model;

public partial class Status
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int MaxLevel { get; set; }

    public string? Description { get; set; }

    public string? ScalingFormula { get; set; }

    public virtual ICollection<Effect> Effects { get; set; } = new List<Effect>();
}
