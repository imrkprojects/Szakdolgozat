using UnityEngine;

/// <summary>Stores the identity, display name, color, and score for one player.</summary>
[System.Serializable]
public class Player
{
    public PlayerID ID    { get; private set; }
    public string   Name  { get; private set; }
    public Color    Color { get; private set; }
    public int      Score { get; private set; }

    /// <summary>Creates a player with the supplied identity and presentation values.</summary>
    public Player(PlayerID id, string name, Color color)
    {
        ID    = id;
        Name  = name;
        Color = color;
        Score = 0;
    }
    
    /// <summary>Adds points to this player's score.</summary>
    public void AddScore(int amount) => Score += amount;
}
