using System;
using System.Collections.Generic;

namespace LightestDungeonCreator.Model;

public partial class Lootentry
{
    public int LootTableId { get; set; }

    public int Num { get; set; }

    public int ItemId { get; set; }

    public float DropChance { get; set; }

    public string MinQuality { get; set; } = null!;

    public string MaxQuality { get; set; } = null!;

    public virtual Item Item { get; set; } = null!;

    public virtual Loottable LootTable { get; set; } = null!;
}
