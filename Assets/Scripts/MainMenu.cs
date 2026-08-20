using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Lives in your Main Menu scene. One "Play Game" button that loads the
/// gameplay scene.
///
/// This does NOT assume GameManager already exists - it might not, if
/// Main Menu is the very first scene the game ever loads (nothing creates
/// GameManager until the gameplay scene itself loads and runs its own
/// Awake()/Start()). So this script handles two different situations:
///
///  - First-ever launch (GameManager.Instance is null): loads the
///    gameplay scene directly. GameManager gets created fresh as part of
///    that scene loading, and its own Start() automatically kicks off
///    the first round - nothing else to do here.
///  - Returning from a finished round (GameManager.Instance already
///    exists, persisted via DontDestroyOnLoad from the Game Over scene's
///    Main Menu button): routes through GameManager.ReturnToGameplayAndRestart()
///    instead, because a persisted object's Start() does NOT fire again
///    on a plain scene load - GameManager needs to be told directly to
///    start a fresh round.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Tooltip("Must exactly match your gameplay scene's name and be added to File > Build Settings > Scenes In Build. Only used if GameManager doesn't exist yet (first-ever launch) - otherwise GameManager's own Gameplay Scene Name is used instead, so keep these two in sync if you ever rename the scene.")]
    public string gameplaySceneName = "Gameplay";

    public Button playGameButton;

    private void Start()
    {
        if (playGameButton != null)
            playGameButton.onClick.AddListener(HandlePlayGame);
    }

    private void HandlePlayGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToGameplayAndRestart();
        }
        else if (!string.IsNullOrEmpty(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
    }
}