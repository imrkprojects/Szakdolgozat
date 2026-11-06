/// <summary>Visual state used to choose a cell's highlight color.</summary>
public enum TileHighlightState
{
    None, // No overlay, plain ground
    Reachable, // Valid move target — green
    Selected, // Unit is standing here — yellow
    AbilityRange, // Cell is within ability range — orange
    AbilityTarget // Cell is the chosen ability target — red
}
