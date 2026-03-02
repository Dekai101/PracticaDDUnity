using System;
using System.Collections.Generic;

namespace LightestDungeonCreator.Models;

public partial class Effect
{
    public int Id { get; set; }

    public int? StatId { get; set; }

    public int? MinFlatPower { get; set; }

    public int? MaxFlatPower { get; set; }

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
