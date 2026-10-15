using UnityEngine;

[System.Serializable]
public class Player
{
    public PlayerID ID    { get; private set; }
    public string   Name  { get; private set; }
    public Color    Color { get; private set; }
    public int      Score { get; private set; }

    public Player(PlayerID id, string name, Color color)
    {
        ID    = id;
        Name  = name;
        Color = color;
        Score = 0;
    }
    
    // Placeholder
    public void AddScore(int amount) => Score += amount;
}
