using System;
using System.Collections.Generic;

namespace LightestDungeonCreator.Model;

public partial class Skill
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int EnergyCost { get; set; }

    public float Accuracy { get; set; }

    public int Hits { get; set; }

    public string TargetType { get; set; } = null!;

    public bool IsAoe { get; set; }

    public bool IsPassive { get; set; }

    public string ImageThumb { get; set; } = null!;

    public virtual ICollection<Effect> Effects { get; set; } = new List<Effect>();

    public virtual ICollection<Entity> Entities { get; set; } = new List<Entity>();
}
