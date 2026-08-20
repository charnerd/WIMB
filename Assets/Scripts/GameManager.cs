using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The brain of the round. Handles:
///  - 20s intermission before the timer starts
///  - the 12 minute countdown
///  - picking a random BabyProfile from Baby Pool each round as the
///    round's target. In the world, every GuessStation has a Baby
///    Profile assigned AND an "Is Correct Baby" checkbox - a station only
///    counts as correct if BOTH its Baby matches this round's target AND
///    its checkbox is ticked. Multiple stations can share the same Baby
///    Profile (e.g. two dolls that both "fit" Baby B's description) -
///    the checkbox is what decides which specific one actually wins when
///    that profile gets picked. See ValidateBabyStations() for the
///    startup sanity check on this setup.
///  - tracking guess tries (Max Tries, configurable in the Inspector) and
///    ending the game on timeout, tries exhausted, or a correct guess
///  - loading the "GameOver" scene whenever the round ends (any of the
///    three ways above), loading back to the gameplay scene + starting a
///    fresh round when the player clicks Play Again there, and loading
///    the main menu scene when the player clicks Main Menu there
///
/// This object survives scene loads (DontDestroyOnLoad) so the Game Over
/// scene's UI can read the final score/state after the transition, and so
/// there's exactly one persistent GameManager across the whole session
/// instead of rebuilding one every round.
///
/// Other scripts (ClueManager, ScoreManager, GuessManager, NotebookUI,
/// GameOverUI) all read GameManager.Instance and subscribe to its events
/// rather than talking to each other directly.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Intermission, Playing, GameOver }

    [Header("Round Setup")]
    [Tooltip("Drag in up to 3 BabyProfile assets. One is picked at random each round as the target - a GuessStation only counts as correct if its Baby matches this AND its Is Correct Baby checkbox is ticked.")]
    public BabyProfile[] babyPool;

    [Header("Timing")]
    public float intermissionDuration = 20f;
    public float roundDuration = 12f * 60f; // 12 minutes

    [Header("Tries")]
    public int maxTries = 2;

    [Header("Scene Flow")]
    [Tooltip("Scene loaded whenever the round ends, win or lose. Must be added to File > Build Settings > Scenes In Build.")]
    public string gameOverSceneName = "GameOver";
    [Tooltip("The main gameplay scene - loaded when the player clicks Play Again from the Game Over scene. Must also be in Build Settings.")]
    public string gameplaySceneName = "Gameplay";
    [Tooltip("The main menu scene - loaded when the player clicks Main Menu from the Game Over scene. Must also be in Build Settings.")]
    public string mainMenuSceneName = "MainMenu";

    // --- Runtime state ---
    public GameState CurrentState { get; private set; } = GameState.Intermission;
    public BabyProfile CurrentBaby { get; private set; }
    public float TimeRemaining { get; private set; }
    public int TriesRemaining { get; private set; }
    public bool Won { get; private set; }

    private int lastBabyIndex = -1;

    // --- Events other scripts subscribe to ---
    public event Action OnIntermissionStart;
    public event Action<float> OnIntermissionTick;   // seconds left in intermission
    public event Action OnRoundStart;                 // fires once the 12-min timer begins
    public event Action<float> OnTimerTick;            // seconds left in round
    public event Action<int> OnTriesChanged;           // tries remaining
    public event Action<bool> OnGameOver;              // true = won, false = lost

    private void Awake()
    {
        // Simple singleton - fine for a single-scene game.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Survive the load into the Game Over scene (and back) so
        // GameOverUI can read the final score/state after the transition,
        // and so this object doesn't get rebuilt every round.
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ValidateBabyStations();
        StartNewGame();
    }

    /// <summary>
    /// Runs once at startup, purely as a sanity check - this doesn't
    /// assign anything (unlike the old auto-fill approach), it just warns
    /// you in the Console about setup mistakes before they turn into a
    /// confusing round: a profile in Baby Pool with no GuessStation
    /// checked as its correct baby (that profile could never be won if
    /// picked), or a profile with more than one station checked (not
    /// necessarily wrong, but worth double-checking you meant it).
    /// </summary>
    private void ValidateBabyStations()
    {
        if (babyPool == null || babyPool.Length == 0) return;

        GuessStation[] allStations = FindObjectsOfType<GuessStation>();

        foreach (var profile in babyPool)
        {
            if (profile == null)
            {
                Debug.LogError("GameManager: Baby Pool has an empty (None) slot - drag an actual BabyProfile asset into every slot.");
                continue;
            }

            int correctCount = 0;
            foreach (var station in allStations)
            {
                if (station.baby == profile && station.isCorrectBaby)
                    correctCount++;
            }

            if (correctCount == 0)
                Debug.LogError($"GameManager: no GuessStation is checked as 'Is Correct Baby' for the profile '{profile.displayName}' - if this profile gets picked as the round's target, there'd be no way to win. Find a station showing this profile and tick its 'Is Correct Baby' box.");
            else if (correctCount > 1)
                Debug.LogWarning($"GameManager: {correctCount} GuessStations are checked as 'Is Correct Baby' for the profile '{profile.displayName}'. Not necessarily a mistake - any of them would count as correct - but double check that's what you intended.");
        }
    }

    /// <summary>
    /// Resets everything and kicks off a brand new round (fresh random
    /// target baby, fresh score, fresh tries). Call this from your Restart
    /// / Play Again button on the Game Over screen as well as on first launch.
    /// </summary>
    public void StartNewGame()
    {
        StopAllCoroutines();

        TriesRemaining = maxTries;
        Won = false;
        CurrentState = GameState.Intermission;

        PickRandomTargetBaby();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetForNewRound();

        if (ClueManager.Instance != null)
            ClueManager.Instance.ResetForNewRound(CurrentBaby);

        OnTriesChanged?.Invoke(TriesRemaining);

        StartCoroutine(IntermissionRoutine());
    }

    private void PickRandomTargetBaby()
    {
        if (babyPool == null || babyPool.Length == 0)
        {
            Debug.LogError("GameManager: babyPool is empty. Assign up to 3 BabyProfile assets in the Inspector.");
            return;
        }

        // Re-seed so replays in the same session don't produce the same
        // sequence of "random" picks.
        UnityEngine.Random.InitState(Environment.TickCount);

        int index;
        if (babyPool.Length == 1)
        {
            index = 0;
        }
        else
        {
            // Avoid picking the exact same target twice in a row when possible.
            do
            {
                index = UnityEngine.Random.Range(0, babyPool.Length);
            } while (index == lastBabyIndex);
        }

        lastBabyIndex = index;
        CurrentBaby = babyPool[index];

        // Nothing else to do here - GuessManager checks each interacted
        // station's own Baby + Is Correct Baby checkbox against this
        // value directly, no station assignment needed.
    }

    private IEnumerator IntermissionRoutine()
    {
        OnIntermissionStart?.Invoke();

        float t = intermissionDuration;
        while (t > 0f)
        {
            OnIntermissionTick?.Invoke(t);
            yield return null;
            t -= Time.deltaTime;
        }
        OnIntermissionTick?.Invoke(0f);

        BeginRound();
    }

    private void BeginRound()
    {
        CurrentState = GameState.Playing;
        TimeRemaining = roundDuration;
        OnRoundStart?.Invoke();

        // No clue is granted automatically anymore - even the Gender
        // clue now has to be found via a ClueStation like the rest. The
        // 12-minute timer starting is what "unlocks" station interaction
        // (see ClueStation.Update(), which gates on CurrentState.Playing);
        // ClueManager hands out this round's randomly-ordered clues one
        // at a time as the player finds real (non-dud) stations.

        StartCoroutine(RoundTimerRoutine());
    }

    private IEnumerator RoundTimerRoutine()
    {
        while (TimeRemaining > 0f && CurrentState == GameState.Playing)
        {
            OnTimerTick?.Invoke(TimeRemaining);
            yield return null;
            TimeRemaining -= Time.deltaTime;
        }

        if (CurrentState == GameState.Playing)
        {
            TimeRemaining = 0f;
            OnTimerTick?.Invoke(0f);
            EndGame(false); // ran out of time
        }
    }

    /// <summary>Call this from GuessManager when the player picks the correct baby.</summary>
    public void PlayerGuessedCorrectly()
    {
        if (CurrentState != GameState.Playing) return;

        if (ScoreManager.Instance != null)
        {
            // Remainder-of-time bonus (whole seconds left, converted from the timer).
            ScoreManager.Instance.AwardTimeBonus(TimeRemaining);
            // +100 per try the player still had left when they got it right.
            ScoreManager.Instance.AwardTriesBonus(TriesRemaining);
        }

        EndGame(true);
    }

    /// <summary>Call this from GuessManager when the player picks a wrong baby.</summary>
    public void PlayerGuessedIncorrectly()
    {
        if (CurrentState != GameState.Playing) return;

        TriesRemaining = Mathf.Max(0, TriesRemaining - 1);
        OnTriesChanged?.Invoke(TriesRemaining);

        if (TriesRemaining <= 0)
        {
            EndGame(false);
        }
    }

    private void EndGame(bool won)
    {
        Won = won;
        CurrentState = GameState.GameOver;
        StopAllCoroutines();
        OnGameOver?.Invoke(won);

        if (!string.IsNullOrEmpty(gameOverSceneName))
            SceneManager.LoadScene(gameOverSceneName);
    }

    /// <summary>
    /// Call this from the Game Over scene's Play Again button. Loads back
    /// into the gameplay scene and immediately starts a fresh round - this
    /// object survives the scene load, so its own Start() won't fire again
    /// on the way back in, which is why this calls StartNewGame() directly.
    /// </summary>
    public void ReturnToGameplayAndRestart()
    {
        if (!string.IsNullOrEmpty(gameplaySceneName))
            SceneManager.LoadScene(gameplaySceneName);

        StartNewGame();
    }

    /// <summary>
    /// Call this from the Game Over scene's Main Menu button. Just loads
    /// the menu scene - no round state to reset, that only happens when
    /// the player actually chooses to play again from the menu.
    /// </summary>
    public void LoadMainMenu()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }
}