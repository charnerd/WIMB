using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lives in the dedicated "GameOver" scene (loaded automatically by
/// GameManager.EndGame() whenever the round ends - win, timeout, or out
/// of tries). No panel to show/hide anymore - the whole scene IS the
/// game over screen, so this just reads the final state directly in
/// Start() and populates itself. GameManager and ScoreManager persist
/// across the scene load (DontDestroyOnLoad), so their data is still
/// valid here.
///
/// Two buttons: Play Again loads back into the gameplay scene and starts
/// a fresh round; Main Menu just loads the menu scene, no round reset
/// needed until the player actually chooses to play again from there.
///
/// This satisfies "take these score updates to keep track of these
/// actions so they can be presented on the game over screen" - the full
/// line-by-line history comes straight from ScoreManager.History.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    public Text resultTitleText;      // "You found the baby!" / "Time's up!" / "Out of tries!"
    public Text finalScoreText;
    [Tooltip("A single multi-line text listing every scoring event. Swap for an instantiated-row list if you want fancier styling.")]
    public Text scoreBreakdownText;
    public Button playAgainButton;
    public Button mainMenuButton;

    private void Start()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(HandlePlayAgain);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(HandleMainMenu);

        DisplayResult();
    }

    private void DisplayResult()
    {
        if (GameManager.Instance == null) return;

        if (resultTitleText != null)
        {
            if (GameManager.Instance.Won)
                resultTitleText.text = "You found the baby!";
            else if (GameManager.Instance.TriesRemaining <= 0)
                resultTitleText.text = "Out of tries!";
            else
                resultTitleText.text = "Time's up!";
        }

        if (ScoreManager.Instance != null)
        {
            if (finalScoreText != null)
                finalScoreText.text = $"Final Score: {ScoreManager.Instance.CurrentScore}";

            if (scoreBreakdownText != null)
            {
                var sb = new StringBuilder();
                foreach (var entry in ScoreManager.Instance.History)
                {
                    string sign = entry.pointsDelta >= 0 ? "+" : "";
                    sb.AppendLine($"{entry.label}: {sign}{entry.pointsDelta}  (total: {entry.runningTotal})");
                }
                scoreBreakdownText.text = sb.ToString();
            }
        }
    }

    private void HandlePlayAgain()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToGameplayAndRestart();
    }

    private void HandleMainMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadMainMenu();
    }
}