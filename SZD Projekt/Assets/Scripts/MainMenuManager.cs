using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Handles actions available from the main menu.</summary>
public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";

    /// <summary>Loads the configured game scene. Wire this to the Play button.</summary>
    public void OnPlayButtonClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
