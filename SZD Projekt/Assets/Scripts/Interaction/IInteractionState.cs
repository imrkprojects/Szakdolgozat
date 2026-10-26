using System.Collections.Generic;

/// <summary>
/// Interface for interaction modes within SelectionManager.
/// Each state owns its own highlighting logic and click behavior.
/// </summary>
public interface IInteractionState
{
    /// <summary>Called when this state becomes active. Set up highlights here.</summary>
    void Enter(Unit selectedUnit);

    /// <summary>Called when leaving this state. Clean up highlights here.</summary>
    void Exit();

    /// <summary>Handle a tile click while this state is active.</summary>
    void HandleTileClicked(Tile clickedTile);
}
