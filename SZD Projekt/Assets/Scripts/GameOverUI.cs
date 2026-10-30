using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Displays the game result and handles the game-over menu buttons.</summary>
public class GameOverUI : MonoBehaviour
{
    private Match _match;
    private SelectionManager _selection;

    [SerializeField] private GameObject overlay; // GameOverOverlay
    [SerializeField] private TMP_Text resultText; // ResultText
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        overlay.SetActive(false);
    }

    /// <summary>Links the overlay to the match so it shows when the match ends.</summary>
    public void Init(Match match, SelectionManager selection)
    {
        _match = match;
        _selection = selection;
        _match.Turns.GameOver += HandleGameOver;
    }

    private void OnDestroy()
    {
        if (_match != null) _match.Turns.GameOver -= HandleGameOver;
    }

    /// <summary>Shows the winner when the turn order reports the end of the match.</summary>
    private void HandleGameOver(PlayerID winner)
    {
        // EndTurn skips OnTurnChanged on game over, so clear the selection and highlights here
        _selection.Deselect();
        resultText.text = $"{_match.GetPlayer(winner).Name} wins!";
        // TODO: Show scores once the score system exists
        overlay.SetActive(true);
    }

    /// <summary>Loads the game scene again.</summary>
    public void OnPlayAgainClicked() => SceneManager.LoadScene(gameSceneName);

    /// <summary>Returns to the main menu.</summary>
    public void OnMainMenuClicked() => SceneManager.LoadScene(mainMenuSceneName);
}