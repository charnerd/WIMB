using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns the current BabyProfile's data into player-facing clue text.
///
/// Clue order is fully randomized every round - all 5 clue types
/// (Gender, Skin Tone, Accessory, Primary Color, Last Seen) are shuffled
/// at round start, and whichever "real" clue station the player happens
/// to find first hands out whatever landed first in that shuffle. Gender
/// is no longer guaranteed to be found first - it could come up 1st, 3rd,
/// or last, same as any other clue type. There's no "correct" order to
/// enforce; each real clue simply gets appended to the found list as it
/// comes in.
///
/// Scoring still depends on POSITION of discovery within the round (1st
/// clue found = flat +25, 2nd = score x2, 3rd = x3, 4th = x4, 5th = x5) -
/// that's about how many clues you've found so far, not which specific
/// type they are, so it works the same regardless of which type lands in
/// which position.
///
/// Design note on "different areas of the map" + replay variety:
/// ClueStations don't need to be hard-wired to a specific clue type.
/// Each ClueStation rolls its own 50/50 "am I a real clue or a dud this
/// round" chance (see ClueStation.cs). When the player interacts with a
/// station that rolled "yes", it calls TryCollectNextClue(), which hands
/// out whatever clue type is next in this round's shuffled order. Scatter
/// more ClueStation objects around the map than you need (e.g. 8-9), and
/// the player won't know in advance which ones matter each round.
/// </summary>
public class ClueManager : MonoBehaviour
{
    public static ClueManager Instance { get; private set; }

    public enum ClueType { Gender, SkinTone, Accessory, PrimaryColor, LastLocation }

    private static readonly ClueType[] AllTypes =
    {
        ClueType.Gender,
        ClueType.SkinTone,
        ClueType.Accessory,
        ClueType.PrimaryColor,
        ClueType.LastLocation
    };

    public const int MaxClues = 5;

    /// <summary>The exact message a dud/empty station reports. ClueStation fires this verbatim; NotebookUI checks against it to know when NOT to touch the list.</summary>
    public const string NoClueFoundMessage = "No clue found!";

    private BabyProfile currentBaby;
    private readonly List<string> collectedClueText = new List<string>();

    // This round's random order - built fresh every round, shrinks by one
    // (from the front) every time a real clue is handed out.
    private List<ClueType> remainingThisRound = new List<ClueType>();

    // Guards against two ClueStations both being in interact range at once
    // and both responding to the same single key press in the same frame -
    // without this, one E press could silently consume two clue slots,
    // with the first one's message getting overwritten before you ever see
    // it (this was the actual cause of clues seeming to "skip" or show the
    // wrong one).
    private int lastInteractionClaimFrame = -1;

    /// <summary>Plain "Label: Value" lines, one per clue found so far - this is exactly what the notebook lists.</summary>
    public IReadOnlyList<string> CollectedClues => collectedClueText;

    /// <summary>Fired whenever a new clue is revealed. Params: position found this round (0-based), plain "Label: Value" clue text.</summary>
    public event Action<int, string> OnClueCollected;

    /// <summary>Fired once all 5 clues have been found - ClueStations listen to this to disable themselves.</summary>
    public event Action OnAllCluesCollected;

    /// <summary>
    /// Fired for every station interaction that results in a message the
    /// player should see in the score tracker's subtext: either a combined
    /// "+points - Label: Value" line for a real clue, or "No clue found!"
    /// for a station that rolled "no" this round (or had nothing left to
    /// give). ScoreTrackerUI listens to this alongside ScoreManager's own
    /// event (for time/tries bonuses) so everything shows in one place.
    /// </summary>
    public event Action<string> OnStationFeedback;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ResetForNewRound(BabyProfile baby)
    {
        currentBaby = baby;
        collectedClueText.Clear();
        remainingThisRound = new List<ClueType>(AllTypes);
        Shuffle(remainingThisRound);
    }

    private static void Shuffle(List<ClueType> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Called once per real key press, by whichever ClueStation's Update()
    /// happens to run first that frame - claims this frame so any other
    /// station also in range at the same moment backs off instead of also
    /// firing. Returns true exactly once per frame.
    /// </summary>
    public bool TryClaimInteractionThisFrame()
    {
        if (Time.frameCount == lastInteractionClaimFrame) return false;
        lastInteractionClaimFrame = Time.frameCount;
        return true;
    }

    /// <summary>
    /// Called by a ClueStation when the player presses E on a station that
    /// rolled "yes" for this round. Returns true and outputs a combined
    /// "+points - Label: Value" feedback message if a clue was actually
    /// handed out; returns false (with feedbackMessage left null) if
    /// there's nothing left to give - either the round hasn't started yet,
    /// or all 5 clues were already found via other stations before this
    /// one got interacted with.
    /// </summary>
    public bool TryCollectNextClue(out string feedbackMessage)
    {
        feedbackMessage = null;

        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return false;

        if (remainingThisRound.Count == 0) return false;

        feedbackMessage = CollectClue();
        return true;
    }

    /// <summary>Call this from ClueStation after it decides what message the player should see (real clue or "No clue found!").</summary>
    public void ReportStationFeedback(string message)
    {
        OnStationFeedback?.Invoke(message);
    }

    private string CollectClue()
    {
        ClueType type = remainingThisRound[0];
        remainingThisRound.RemoveAt(0);

        string attributeText = BuildClueText(type, currentBaby); // e.g. "Gender: Girl" - exactly what the notebook shows
        collectedClueText.Add(attributeText);

        int justFoundPosition = collectedClueText.Count - 1; // 0-based position within THIS round's discovery order

        OnClueCollected?.Invoke(justFoundPosition, attributeText);

        // Scoring is based on discovery POSITION this round, not clue type:
        // 1st found = flat 25pts, 2nd-5th multiply the running score by
        // (position+1). ScoreManager still logs this to its History for
        // the Game Over breakdown - it just no longer fires its own popup
        // message for these two, since we build one combined "points +
        // what was found" message here instead.
        string feedbackMessage = attributeText;
        if (ScoreManager.Instance != null)
        {
            int delta = justFoundPosition == 0
                ? ScoreManager.Instance.AwardFirstClue(attributeText)
                : ScoreManager.Instance.AwardClueMultiplier(justFoundPosition + 1, attributeText);

            feedbackMessage = $"+{delta} - {attributeText}";
        }

        if (remainingThisRound.Count == 0)
            OnAllCluesCollected?.Invoke();

        return feedbackMessage;
    }

    private string BuildClueText(ClueType type, BabyProfile baby)
    {
        if (baby == null) return "???";

        switch (type)
        {
            case ClueType.Gender:
                return $"Gender: {(baby.gender == BabyProfile.Gender.Boy ? "Boy" : "Girl")}";
            case ClueType.SkinTone:
                return $"Skin Tone: {Capitalize(baby.skinTone)}";
            case ClueType.Accessory:
                return $"Accessory: {Capitalize(baby.accessory)}";
            case ClueType.PrimaryColor:
                return $"Primary Color: {Capitalize(baby.primaryClothingColor)}";
            case ClueType.LastLocation:
                return $"Last Seen: {Capitalize(baby.lastSeenLocation)}";
            default:
                return "???";
        }
    }

    /// <summary>
    /// Capitalizes just the first letter, so a BabyProfile field typed as
    /// "light" displays as "Light". If the field was left empty in the
    /// Editor, returns "(not set)" instead of silently producing a blank
    /// value like "Skin Tone: " with nothing after the colon - if you see
    /// "(not set)" in a clue, go fill in that field on the BabyProfile asset.
    /// </summary>
    private static string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "(not set)";
        return char.ToUpper(s[0]) + s.Substring(1);
    }
}