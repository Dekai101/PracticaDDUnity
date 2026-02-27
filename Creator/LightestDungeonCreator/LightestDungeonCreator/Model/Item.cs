using System;
using System.Collections.Generic;

namespace LightestDungeonCreator.Model;

public partial class Item
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Quality { get; set; } = null!;

    public bool Consumable { get; set; }

    public int? MaxUses { get; set; }

    public string ImageThumb { get; set; } = null!;

    public virtual ICollection<Lootentry> Lootentries { get; set; } = new List<Lootentry>();

    public virtual ICollection<Effect> Effects { get; set; } = new List<Effect>();
}
