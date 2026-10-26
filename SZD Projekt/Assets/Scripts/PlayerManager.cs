using UnityEngine;

/// <summary>Creates and provides access to the players in the current match.</summary>
public class PlayerManager : MonoBehaviour
{
    /// <summary>The active player manager instance.</summary>
    public static PlayerManager Instance { get; private set; }

    /// <summary>Player 1 in the current match.</summary>
    public Player Player1 { get; private set; }
    /// <summary>Player 2 in the current match.</summary>
    public Player Player2 { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Creates the two match players with their default names and colors.</summary>
    public void InitPlayers()
    {
        Player1 = new Player(PlayerID.Player1, "Player 1", Color.blue);
        Player2 = new Player(PlayerID.Player2, "Player 2", Color.red);
        Debug.Log($"Players initialized: {Player1.Name} vs {Player2.Name}");
    }

    /// <summary>Returns the player for an ID, or null if the ID is not a player.</summary>
    public Player GetPlayer(PlayerID id)
    {
        if (id == PlayerID.Player1) return Player1;
        if (id == PlayerID.Player2) return Player2;
        return null;
    }
}
