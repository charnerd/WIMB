using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All scoring rules live here, decoupled from any UI. UI scripts
/// (ScoreTrackerUI, GameOverUI) subscribe to the events instead of
/// computing anything themselves.
///
/// Scoring rules implemented exactly as specified:
///  - Clue 1 (boy/girl):        flat +25 points
///  - Clue 2 (skin tone):       score *= 2
///  - Clue 3 (accessory):       score *= 3
///  - Clue 4 (primary color):   score *= 4
///  - Clue 5 (last location):   score *= 5
///  - Correct guess before time runs out: + (whole seconds left on the clock)
///  - +100 for every guess try still remaining when the correct guess is made
///
/// AwardFirstClue/AwardClueMultiplier return the points added (the delta)
/// instead of firing their own popup message - ClueManager combines that
/// number with the clue's "Label: Value" text into one message itself.
/// AwardTimeBonus/AwardTriesBonus still fire OnScorePopup directly since
/// there's no competing message to combine with at guess time.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Serializable]
    public struct ScoreEntry
    {
        public string label;
        public int pointsDelta;   // can be positive (add) - multipliers are logged with resulting total
        public int runningTotal;
        public ScoreEntry(string label, int pointsDelta, int runningTotal)
        {
            this.label = label;
            this.pointsDelta = pointsDelta;
            this.runningTotal = runningTotal;
        }
    }

    public int CurrentScore { get; private set; }
    private readonly List<ScoreEntry> history = new List<ScoreEntry>();
    public IReadOnlyList<ScoreEntry> History => history;

    /// <summary>How long the tracker's subtext line stays visible after a scoring event.</summary>
    public const float PopupDisplaySeconds = 8f;

    /// <summary>Fired whenever the score changes, with the new total - drives the persistent "Score: X" text.</summary>
    public event Action<int> OnScoreChanged;

    /// <summary>Fired directly for time/tries bonuses (no competing message to combine with) - ScoreTrackerUI shows this as the subtext line for 8 seconds.</summary>
    public event Action<string> OnScorePopup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ResetForNewRound()
    {
        CurrentScore = 0;
        history.Clear();
        OnScoreChanged?.Invoke(CurrentScore);
    }

    /// <summary>Clue 1 - flat 25 points. Returns the points added.</summary>
    public int AwardFirstClue(string clueText)
    {
        CurrentScore += 25;
        Log($"Clue found: {clueText}", 25);
        return 25;
    }

    /// <summary>Clues 2-5 - multiplies the running score by the given multiplier (2,3,4,5). Returns the points added.</summary>
    public int AwardClueMultiplier(int multiplier, string clueText)
    {
        int before = CurrentScore;
        CurrentScore *= multiplier;
        int delta = CurrentScore - before;
        Log($"Clue found: {clueText} (score x{multiplier})", delta);
        return delta;
    }

    /// <summary>Whole seconds left on the clock, added as bonus points for a correct guess.</summary>
    public void AwardTimeBonus(float secondsRemaining)
    {
        int bonus = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
        CurrentScore += bonus;
        Log("Time remaining bonus", bonus);
        Popup($"+{bonus} - time remaining bonus!");
    }

    /// <summary>100 points per guess try still unused.</summary>
    public void AwardTriesBonus(int triesRemaining)
    {
        int bonus = Mathf.Max(0, triesRemaining) * 100;
        CurrentScore += bonus;
        Log($"{triesRemaining} tries remaining bonus", bonus);
        Popup($"+{bonus} - {triesRemaining} tries left!");
    }

    private void Log(string label, int delta)
    {
        history.Add(new ScoreEntry(label, delta, CurrentScore));
        OnScoreChanged?.Invoke(CurrentScore);
    }

    private void Popup(string message)
    {
        OnScorePopup?.Invoke(message);
    }
}