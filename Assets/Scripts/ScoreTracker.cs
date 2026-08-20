using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Replaces the old popup-stack approach with a persistent score tracker:
/// one Text always showing the current total, and a second Text below it
/// that shows what just happened (points earned + what action caused it),
/// visible for 8 seconds after each scoring event before clearing.
///
/// This does NOT instantiate any prefab - both Text elements live
/// directly in your scene, so there's no prefab-drag step that can go
/// wrong. Just assign the two Text references below.
///
/// SETUP:
///  1. Under your Canvas, create a Text element for the running score
///     (e.g. "ScoreText") - assign it to Score Text below.
///  2. Create a second Text element just below/near it for the subtext
///     (e.g. "ScoreSubtext") - assign it to Action Subtext below.
///  3. Attach this script to either of those objects, or a shared parent.
/// </summary>
public class ScoreTrackerUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Always shows the current total, e.g. \"Score: 450\".")]
    public Text scoreText;
    [Tooltip("Shows what just happened, e.g. \"+25 - Gender: Girl\" or \"No clue found!\". Clears itself after Subtext Display Seconds.")]
    public Text actionSubtext;

    [Header("Timing")]
    [Tooltip("How long the subtext stays visible after each event before clearing.")]
    public float subtextDisplaySeconds = ScoreManager.PopupDisplaySeconds; // 8 seconds, matches the rest of the game

    private Coroutine clearSubtextCoroutine;

    private void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
            ScoreManager.Instance.OnScorePopup += HandleActionMessage;
            HandleScoreChanged(ScoreManager.Instance.CurrentScore);
        }

        if (ClueManager.Instance != null)
            ClueManager.Instance.OnStationFeedback += HandleActionMessage;

        if (actionSubtext != null) actionSubtext.text = "";
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
            ScoreManager.Instance.OnScorePopup -= HandleActionMessage;
        }

        if (ClueManager.Instance != null)
            ClueManager.Instance.OnStationFeedback -= HandleActionMessage;
    }

    private void HandleScoreChanged(int newScore)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {newScore}";
    }

    private void HandleActionMessage(string message)
    {
        if (actionSubtext == null) return;

        actionSubtext.text = message;

        // Restart the clear timer every time a new message comes in, so a
        // fast run of clue finds keeps refreshing the full 8 seconds
        // instead of the text disappearing mid-read.
        if (clearSubtextCoroutine != null) StopCoroutine(clearSubtextCoroutine);
        clearSubtextCoroutine = StartCoroutine(ClearSubtextAfterDelay());
    }

    private IEnumerator ClearSubtextAfterDelay()
    {
        yield return new WaitForSeconds(subtextDisplaySeconds);
        if (actionSubtext != null) actionSubtext.text = "";
        clearSubtextCoroutine = null;
    }
}