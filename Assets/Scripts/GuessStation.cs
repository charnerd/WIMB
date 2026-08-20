using UnityEngine;

/// <summary>
/// Put this on each baby GameObject in the world. Every station gets a
/// Baby Profile assigned - multiple stations CAN share the same profile
/// (e.g. two dolls that both "fit" Baby B's description). The Is Correct
/// Baby checkbox is what decides which specific one actually counts as
/// the winner when that profile is picked as the round's target - only
/// one station per profile should have it checked. When the player is
/// close enough and presses Enter, it tells GuessManager to pop up the
/// shared "Submit Guess" button for this specific baby.
///
/// After a wrong guess, GuessManager calls MarkConsumed() on this
/// station so the player can't guess the same wrong baby again this
/// round (by process of elimination, that helps narrow things down).
/// Every station resets back to guessable at the start of each new round.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GuessStation : MonoBehaviour
{
    [Tooltip("Which BabyProfile this world object represents. Multiple stations can share the same profile - Is Correct Baby below is what actually decides the winner.")]
    public BabyProfile baby;

    [Tooltip("Tick this if THIS specific baby object should count as correct when its Baby Profile is picked as the round's target. If multiple stations share the same profile, check this on exactly one of them.")]
    public bool isCorrectBaby;

    [Header("Interaction")]
    [Tooltip("Player transform. If left empty, will try to find a GameObject tagged 'Player' at Start.")]
    public Transform player;
    public float interactRange = 2.5f;
    public KeyCode interactKey = KeyCode.Return; // Enter
    [Tooltip("Optional 'Press Enter' world-space prompt, shown only when in range.")]
    public GameObject pressEnterPrompt;

    private bool consumed = false;
    private bool playerInRange = false;

    private void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (pressEnterPrompt != null) pressEnterPrompt.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.OnIntermissionStart += ResetForNewRound;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnIntermissionStart -= ResetForNewRound;
    }

    private void Update()
    {
        if (consumed || player == null) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        float dist = Vector3.Distance(transform.position, player.position);
        playerInRange = dist <= interactRange;

        if (pressEnterPrompt != null && pressEnterPrompt.activeSelf != playerInRange)
            pressEnterPrompt.SetActive(playerInRange);

        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            // If another GuessStation also in range already claimed this
            // exact frame's key press, back off.
            if (GuessManager.Instance != null && GuessManager.Instance.TryClaimInteractionThisFrame())
                GuessManager.Instance.ShowConfirmButton(baby, this);
        }
    }

    /// <summary>Called by GuessManager after a wrong guess on this baby - disables it for the rest of the round.</summary>
    public void MarkConsumed()
    {
        consumed = true;
        if (pressEnterPrompt != null) pressEnterPrompt.SetActive(false);
    }

    private void ResetForNewRound()
    {
        consumed = false;
        playerInRange = false;
        if (pressEnterPrompt != null) pressEnterPrompt.SetActive(false);
    }
}