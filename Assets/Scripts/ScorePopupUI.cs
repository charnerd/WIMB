using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Listens for ScoreManager's popup event AND ClueManager's station
/// feedback event (clue text / "No clue found!"), showing each message
/// in a screen corner for 8 seconds. Supports several popups stacking at
/// once (e.g. finding a clue triggers both a clue-text popup and a
/// separate score popup) by spawning one row per message instead of
/// overwriting a single label.
///
/// SETUP:
///  1. Create a Canvas -> empty child "ScorePopupContainer" positioned in
///     whichever corner you want (e.g. anchored top-right), with a
///     Vertical Layout Group so rows stack automatically.
///  2. Create a small prefab: a UI Text (or TMP_Text) on its own
///     GameObject, and assign it to popupTextPrefab below.
///  3. Assign popupContainer to the ScorePopupContainer transform.
/// </summary>
public class ScorePopupUI : MonoBehaviour
{
    [Tooltip("A prefab containing a single UI Text component.")]
    public Text popupTextPrefab;
    [Tooltip("Parent transform the popups get instantiated under (ideally has a Vertical Layout Group).")]
    public Transform popupContainer;

    private void OnEnable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScorePopup += HandlePopup;

        if (ClueManager.Instance != null)
            ClueManager.Instance.OnStationFeedback += HandlePopup;
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScorePopup -= HandlePopup;

        if (ClueManager.Instance != null)
            ClueManager.Instance.OnStationFeedback -= HandlePopup;
    }

    private void HandlePopup(string message)
    {
        if (popupTextPrefab == null || popupContainer == null)
        {
            Debug.LogWarning("ScorePopupUI: assign popupTextPrefab and popupContainer in the Inspector.");
            return;
        }

        Text instance = Instantiate(popupTextPrefab, popupContainer);
        instance.text = message;
        instance.gameObject.SetActive(true);
        StartCoroutine(DespawnAfterDelay(instance.gameObject, ScoreManager.PopupDisplaySeconds));
    }

    private IEnumerator DespawnAfterDelay(GameObject go, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (go != null) Destroy(go);
    }
}