/// <summary>Which cells an ability may be used on, on top of its targeting rule's range.</summary>
public enum TargetFilter
{
    /// <summary>A cell holding an enemy unit.</summary>
    Enemy,

    /// <summary>A cell holding a friendly unit, the caster included if its targeting rule allows its own cell.</summary>
    Ally,

    /// <summary>Only the caster's own cell (its targeting rule must not exclude it).</summary>
    Self,

    /// <summary>A cell holding any unit.</summary>
    AnyUnit,

    /// <summary>An empty cell.</summary>
    EmptyCell
}
