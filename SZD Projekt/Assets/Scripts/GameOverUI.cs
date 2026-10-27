using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Displays the game result and handles the game-over menu buttons.</summary>
/// <remarks>Attach this to an always-active object so it can subscribe before the overlay is shown.</remarks>
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject overlay; // GameOverOverlay
    [SerializeField] private TMP_Text resultText; // ResultText
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        overlay.SetActive(false);
    }

    private void OnEnable()
    {
        TurnManager.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        TurnManager.OnGameOver -= HandleGameOver;
    }

    /// <summary>Shows the winner when the turn manager reports the end of the match.</summary>
    private void HandleGameOver(PlayerID winner)
    {
        // EndTurn skips OnTurnChanged on game over, so clear the selection and highlights here
        SelectionManager.Instance.Deselect();
        resultText.text = $"{PlayerManager.Instance.GetPlayer(winner).Name} wins!";
        // TODO: Show scores once the score system exists
        overlay.SetActive(true);
    }

    /// <summary>Loads the game scene again.</summary>
    public void OnPlayAgainClicked() => SceneManager.LoadScene(gameSceneName);

    /// <summary>Returns to the main menu.</summary>
    public void OnMainMenuClicked() => SceneManager.LoadScene(mainMenuSceneName);
}