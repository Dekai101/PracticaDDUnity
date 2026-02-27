using System;
using System.Collections.Generic;

namespace LightestDungeonCreator.Model;

public partial class Effect
{
    public int Id { get; set; }

    public int? StatId { get; set; }

    public float? StatMultiplier { get; set; }

    public int? StatusId { get; set; }

    public int? EffectLevel { get; set; }

    public float Probability { get; set; }

    public int DurationTurns { get; set; }

    public virtual Statistic? Stat { get; set; }

    public virtual Status? Status { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();
}
