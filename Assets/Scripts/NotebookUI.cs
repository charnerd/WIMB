using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Always-visible clue list. No panel, no icon, no toggle - just one Text
/// element that stays on screen the whole time and lists every REAL clue
/// found so far as a numbered list, plain "Label: Value" with no points
/// attached (points-and-action stays over in ScoreTrackerUI's subtext),
/// e.g.:
///   1. Gender: Girl
///   2. Skin Tone: Light
///
/// Uses ClueManager.OnStationFeedback purely as the "something happened,
/// go refresh" trigger (the same signal ScoreTrackerUI's subtext reacts
/// to, so it's confirmed to fire reliably), but reads the actual text to
/// display straight from ClueManager.CollectedClues - the plain,
/// points-free list ClueManager already maintains. A dud station reports
/// ClueManager.NoClueFoundMessage ("No clue found!") - this script checks
/// for that exact message and ignores it, leaving the list untouched.
/// </summary>
public class NotebookUI : MonoBehaviour
{
    [Tooltip("The Text element the clue list gets written into. Stays visible at all times.")]
    public Text clueListText;

    private void Start()
    {
        if (ClueManager.Instance != null)
            ClueManager.Instance.OnStationFeedback += HandleStationFeedback;

        if (GameManager.Instance != null)
            GameManager.Instance.OnIntermissionStart += HandleNewRound;

        RefreshText();
    }

    private void OnDestroy()
    {
        if (ClueManager.Instance != null)
            ClueManager.Instance.OnStationFeedback -= HandleStationFeedback;

        if (GameManager.Instance != null)
            GameManager.Instance.OnIntermissionStart -= HandleNewRound;
    }

    private void HandleStationFeedback(string message)
    {
        // A dud station (or a station with nothing left to give) reports
        // exactly this - leave the list exactly as it was.
        if (message == ClueManager.NoClueFoundMessage) return;

        RefreshText();
    }

    private void HandleNewRound()
    {
        RefreshText(); // ClueManager's list is already cleared by this point, this just updates the display
    }

    private void RefreshText()
    {
        if (clueListText == null || ClueManager.Instance == null) return;

        var clues = ClueManager.Instance.CollectedClues;
        if (clues.Count == 0)
        {
            clueListText.text = "No clues found yet.";
            return;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < clues.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {clues[i]}");
        }
        clueListText.text = sb.ToString();
    }
}