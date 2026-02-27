using System;
using System.Collections.Generic;

namespace LightestDungeonCreator.Model;

public partial class Statistic
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Effect> Effects { get; set; } = new List<Effect>();
}
