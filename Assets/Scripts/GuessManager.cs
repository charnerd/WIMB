using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives guessing now that it works like the clue stations: the player
/// walks up to a baby in the world and presses Enter (handled by
/// GuessStation.cs, one per baby), which pops up a single shared
/// "Submit Guess" button here. Clicking it submits the guess for
/// whichever baby was last interacted with.
///
/// Correctness needs BOTH of these true: the interacted station's Baby
/// matches GameManager.CurrentBaby (this round's randomly-picked target),
/// AND that station's Is Correct Baby checkbox is ticked. Multiple
/// stations can share the same Baby Profile - the checkbox is what
/// disambiguates which one actually wins, so a decoy showing the same
/// profile as the target still won't read as correct unless you've
/// checked its box too.
///
///  - Correct -> button flashes green, then the game ends (win), routed
///    through GameManager.PlayerGuessedCorrectly().
///  - Incorrect -> button flashes red, then hides again, one try is
///    spent, and that specific GuessStation is marked "consumed" so the
///    player can't guess the same wrong baby again.
///
/// SETUP: put GuessStation on each baby GameObject in the world, assign
/// each one's Baby Profile, and tick Is Correct Baby on exactly one
/// station per profile (see GameManager.ValidateBabyStations() for a
/// startup sanity check on this). Assign the shared Confirm Guess Button
/// here.
/// </summary>
public class GuessManager : MonoBehaviour
{
    public static GuessManager Instance { get; private set; }

    [Header("Shared Guess-Confirmation UI")]
    [Tooltip("The single button that pops up when the player interacts with any baby. Needs an Image component for the red-flash feedback.")]
    public Button confirmGuessButton;
    [Tooltip("Optional - shows something like 'Guess: <name>?' on the button. Leave blank to skip.")]
    public Text confirmButtonLabel;
    public Color normalButtonColor = Color.white;
    public Color correctGuessColor = Color.green;
    public Color wrongGuessColor = Color.red;
    [Tooltip("How long the button stays green before the game ends on a correct guess.")]
    public float correctFlashSeconds = 0.6f;
    [Tooltip("How long the button stays red before disappearing after a wrong guess.")]
    public float wrongFlashSeconds = 0.6f;

    [Header("Optional UI feedback")]
    public Text triesRemainingText;
    public Text wrongGuessFeedbackText; // e.g. "Not quite - try again!"

    private BabyProfile pendingBaby;
    private GuessStation pendingStation;
    private Image confirmButtonImage;

    // Same guard as ClueManager's - stops two GuessStations both in range
    // at once from both responding to a single Enter press in the same
    // frame (which would just make ShowConfirmButton immediately overwrite
    // itself with whichever ran second, showing the wrong pending baby).
    private int lastInteractionClaimFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Called once per real key press, by whichever GuessStation's Update()
    /// happens to run first that frame - claims this frame so any other
    /// station also in range at the same moment backs off. Returns true
    /// exactly once per frame.
    /// </summary>
    public bool TryClaimInteractionThisFrame()
    {
        if (Time.frameCount == lastInteractionClaimFrame) return false;
        lastInteractionClaimFrame = Time.frameCount;
        return true;
    }

    private void Start()
    {
        if (confirmGuessButton != null)
        {
            confirmButtonImage = confirmGuessButton.GetComponent<Image>();
            confirmGuessButton.onClick.AddListener(HandleConfirmClicked);
            confirmGuessButton.gameObject.SetActive(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTriesChanged += HandleTriesChanged;
            GameManager.Instance.OnIntermissionStart += HandleNewRound;
            HandleTriesChanged(GameManager.Instance.TriesRemaining);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTriesChanged -= HandleTriesChanged;
            GameManager.Instance.OnIntermissionStart -= HandleNewRound;
        }
    }

    /// <summary>Called by a GuessStation when the player presses Enter within range of it.</summary>
    public void ShowConfirmButton(BabyProfile baby, GuessStation station)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        pendingBaby = baby;
        pendingStation = station;

        if (wrongGuessFeedbackText != null) wrongGuessFeedbackText.text = "";

        if (confirmGuessButton != null)
        {
            confirmGuessButton.gameObject.SetActive(true);
            if (confirmButtonImage != null) confirmButtonImage.color = normalButtonColor;
        }

        if (confirmButtonLabel != null)
            confirmButtonLabel.text = baby != null ? $"Guess: {baby.displayName}?" : "Submit Guess";
    }

    private void HandleConfirmClicked()
    {
        if (pendingBaby == null || pendingStation == null) return;

        // Both conditions required: this station's Baby must match this
        // round's target, AND this specific station must be the one
        // checked as correct for that profile (handles multiple stations
        // sharing the same Baby Profile - only the checked one wins).
        bool isCorrect = pendingBaby == GameManager.Instance.CurrentBaby && pendingStation.isCorrectBaby;

        if (isCorrect)
        {
            StartCoroutine(CorrectGuessFeedback());
        }
        else
        {
            StartCoroutine(WrongGuessFeedback(pendingStation));
        }
    }

    private IEnumerator CorrectGuessFeedback()
    {
        if (confirmButtonImage != null) confirmButtonImage.color = correctGuessColor;

        yield return new WaitForSeconds(correctFlashSeconds);

        HideConfirmButton();
        GameManager.Instance.PlayerGuessedCorrectly();
    }

    private IEnumerator WrongGuessFeedback(GuessStation station)
    {
        if (confirmButtonImage != null) confirmButtonImage.color = wrongGuessColor;
        if (wrongGuessFeedbackText != null) wrongGuessFeedbackText.text = "Not quite - try again!";

        // Spend the try right away so the tries counter updates immediately.
        GameManager.Instance.PlayerGuessedIncorrectly();

        yield return new WaitForSeconds(wrongFlashSeconds);

        HideConfirmButton();

        if (station != null)
            station.MarkConsumed(); // that baby can't be guessed again this round
    }

    private void HideConfirmButton()
    {
        pendingBaby = null;
        pendingStation = null;
        if (confirmGuessButton != null) confirmGuessButton.gameObject.SetActive(false);
    }

    private void HandleTriesChanged(int triesRemaining)
    {
        if (triesRemainingText != null)
            triesRemainingText.text = $"Tries left: {triesRemaining}";
    }

    private void HandleNewRound()
    {
        HideConfirmButton();
        if (wrongGuessFeedbackText != null) wrongGuessFeedbackText.text = "";
    }
}