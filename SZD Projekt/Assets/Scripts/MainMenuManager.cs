using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";

    public void OnPlayButtonClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
