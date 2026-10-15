using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public Player Player1 { get; private set; }
    public Player Player2 { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Called by MatchManager on init
    public void InitPlayers()
    {
        Player1 = new Player(PlayerID.Player1, "Player 1", Color.blue);
        Player2 = new Player(PlayerID.Player2, "Player 2", Color.red);
        Debug.Log($"Players initialized: {Player1.Name} vs {Player2.Name}");
    }

    public Player GetPlayer(PlayerID id)
    {
        if (id == PlayerID.Player1) return Player1;
        if (id == PlayerID.Player2) return Player2;
        return null;
    }
}