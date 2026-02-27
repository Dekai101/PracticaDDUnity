using System;
using System.Collections.Generic;

namespace LightestDungeonCreator.Model;

public partial class Player
{
    public int EntityId { get; set; }

    public int XpPoints { get; set; }

    public int SkillPoints { get; set; }

    public virtual Entity Entity { get; set; } = null!;
}
