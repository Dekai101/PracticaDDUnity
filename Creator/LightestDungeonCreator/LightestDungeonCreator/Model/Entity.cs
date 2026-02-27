using System;
using System.Collections.Generic;

namespace LightestDungeonCreator.Model;

public partial class Entity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Level { get; set; }

    public int Hp { get; set; }

    public int HpMax { get; set; }

    public int Energy { get; set; }

    public int EnergyMax { get; set; }

    public int Attack { get; set; }

    public int Defense { get; set; }

    public int Speed { get; set; }

    public float CritChance { get; set; }

    public float CritDamage { get; set; }

    public float AccuracyMultiplier { get; set; }

    public string ImageThumb { get; set; } = null!;

    public string ImageFull { get; set; } = null!;

    public string? Description { get; set; }

    public virtual Enemy? Enemy { get; set; }

    public virtual Player? Player { get; set; }

    public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();
}
