using UnityEngine;

/// <summary>
/// Put this on any world object you want to act as a clue pickup point
/// (scatter several around the map - 8-9 is a good number). When the
/// player is close enough and presses E, this station either hands out
/// the next clue in the fixed sequence or shows "No clue found!" -
/// determined by an independent 50/50 roll made fresh each round.
///
/// Icon behavior: assign a child "icon" object (whatever visual you're
/// using - sprite, 3D model, whatever) to `iconVisual`. It spins
/// continuously while the station hasn't been interacted with yet, and
/// disappears the moment the player interacts with it (whether that
/// interaction was a real clue or a dud).
///
/// Also implements "clues get easier as time goes on": as the round's
/// elapsed time increases, this station's interact range grows and its
/// beacon light gets brighter/pulses faster, using hintCurve so you can
/// tune the ramp in the Inspector (X = 0-1 fraction of round elapsed,
/// Y = 0-1 difficulty-easing amount). This ramp applies whether or not
/// the station turns out to be a real clue - the player has no way to
/// tell in advance, which is the point.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ClueStation : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Player transform. If left empty, will try to find a GameObject tagged 'Player' at Start.")]
    public Transform player;
    public float baseInteractRange = 2.5f;
    public float maxInteractRange = 6f; // range once fully "eased"
    public KeyCode interactKey = KeyCode.E;

    [Header("Icon Visual (rotates until interacted, then disappears)")]
    [Tooltip("The child object (sprite/model/whatever) that spins and then hides on interact.")]
    public Transform iconVisual;
    public float rotationSpeedDegreesPerSecond = 120f;
    public Vector3 rotationAxis = Vector3.up;

    [Header("Random Clue Chance (re-rolled every round)")]
    [Range(0f, 1f)]
    [Tooltip("Chance this station is an actual clue this round. Re-rolled independently for every station at the start of every round - so a location that was a real clue last time might not be this time.")]
    public float clueChance = 0.5f;

    [Header("Easier-Over-Time Ramp")]
    [Tooltip("X = fraction of round elapsed (0-1), Y = 0-1 ease amount applied to range/beacon.")]
    public AnimationCurve hintCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Optional visuals (assign what you have, leave blank to skip)")]
    public Light beacon;               // a point light that brightens over time
    public float beaconMinIntensity = 0.5f;
    public float beaconMaxIntensity = 4f;
    public GameObject pressEPrompt;    // simple "Press E" world-space UI/sprite, shown only when in range

    /// <summary>Whether this station rolled "yes" for the current round. Read-only from other scripts, useful for debugging.</summary>
    public bool IsClueGiverThisRound { get; private set; }

    private bool consumed = false;
    private bool playerInRange = false;

    private void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (pressEPrompt != null) pressEPrompt.SetActive(false);

        // Cover the very first round here directly rather than relying
        // solely on the OnIntermissionStart event below, since Unity
        // doesn't guarantee this Start() runs before GameManager's
        // first-round event fires.
        RollForNewRound();

        if (ClueManager.Instance != null)
            ClueManager.Instance.OnAllCluesCollected += HandleAllCluesCollected;

        // Every round after the first is covered by this event, which
        // fires each time GameManager.StartNewGame() runs (Play Again).
        if (GameManager.Instance != null)
            GameManager.Instance.OnIntermissionStart += RollForNewRound;
    }

    private void OnDestroy()
    {
        if (ClueManager.Instance != null)
            ClueManager.Instance.OnAllCluesCollected -= HandleAllCluesCollected;

        if (GameManager.Instance != null)
            GameManager.Instance.OnIntermissionStart -= RollForNewRound;
    }

    /// <summary>Re-rolls the 50/50 and resets this station's visuals/state for a new round.</summary>
    private void RollForNewRound()
    {
        IsClueGiverThisRound = UnityEngine.Random.value < clueChance;

        consumed = false;
        playerInRange = false;

        if (iconVisual != null) iconVisual.gameObject.SetActive(true);
        if (beacon != null) beacon.enabled = true;
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
    }

    private void Update()
    {
        // Keep spinning right up until the player actually interacts.
        if (!consumed && iconVisual != null)
            iconVisual.Rotate(rotationAxis, rotationSpeedDegreesPerSecond * Time.deltaTime, Space.World);

        if (consumed || player == null) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        float ease = GetEase();
        float currentRange = Mathf.Lerp(baseInteractRange, maxInteractRange, ease);

        if (beacon != null)
            beacon.intensity = Mathf.Lerp(beaconMinIntensity, beaconMaxIntensity, ease);

        float dist = Vector3.Distance(transform.position, player.position);
        playerInRange = dist <= currentRange;

        if (pressEPrompt != null && pressEPrompt.activeSelf != playerInRange)
            pressEPrompt.SetActive(playerInRange);

        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            // If another ClueStation also in range already claimed this
            // exact frame's key press, back off - prevents one E press
            // from consuming two clue slots when stations overlap in range.
            if (ClueManager.Instance != null && ClueManager.Instance.TryClaimInteractionThisFrame())
                Interact();
        }
    }

    private void Interact()
    {
        string message;

        if (IsClueGiverThisRound && ClueManager.Instance != null)
        {
            bool gotClue = ClueManager.Instance.TryCollectNextClue(out string clueText);
            // gotClue can still be false here even on a "yes" station if
            // every clue had already been found via other stations first.
            message = gotClue ? clueText : ClueManager.NoClueFoundMessage;
        }
        else
        {
            message = ClueManager.NoClueFoundMessage;
        }

        if (ClueManager.Instance != null)
            ClueManager.Instance.ReportStationFeedback(message);

        Consume();
    }

    private float GetEase()
    {
        if (GameManager.Instance == null || GameManager.Instance.roundDuration <= 0f) return 0f;
        float elapsed = GameManager.Instance.roundDuration - GameManager.Instance.TimeRemaining;
        float frac = Mathf.Clamp01(elapsed / GameManager.Instance.roundDuration);
        return hintCurve.Evaluate(frac);
    }

    private void Consume()
    {
        consumed = true;
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (beacon != null) beacon.enabled = false;
        if (iconVisual != null) iconVisual.gameObject.SetActive(false);
    }

    private void HandleAllCluesCollected()
    {
        // No clues left to give anywhere - shut the station down entirely
        // so a late-found station just sits there quietly instead of
        // spinning forever with nothing to offer.
        if (consumed) return;
        Consume();
    }
}