using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small HUD script: shows the 20-second intermission countdown, then
/// switches to the mm:ss round timer once play begins. Keep this
/// separate from NotebookUI/ScorePopupUI so each script has one job.
/// </summary>
public class GameHUD : MonoBehaviour
{
    public Text timerText;
    [Tooltip("Optional separate label shown only during intermission, e.g. 'Get Ready!'")]
    public GameObject intermissionLabel;

    private void Start()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnIntermissionStart += HandleIntermissionStart;
        GameManager.Instance.OnIntermissionTick += HandleIntermissionTick;
        GameManager.Instance.OnRoundStart += HandleRoundStart;
        GameManager.Instance.OnTimerTick += HandleTimerTick;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnIntermissionStart -= HandleIntermissionStart;
        GameManager.Instance.OnIntermissionTick -= HandleIntermissionTick;
        GameManager.Instance.OnRoundStart -= HandleRoundStart;
        GameManager.Instance.OnTimerTick -= HandleTimerTick;
    }

    private void HandleIntermissionStart()
    {
        if (intermissionLabel != null) intermissionLabel.SetActive(true);
    }

    private void HandleIntermissionTick(float secondsLeft)
    {
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(secondsLeft).ToString();
    }

    private void HandleRoundStart()
    {
        if (intermissionLabel != null) intermissionLabel.SetActive(false);
    }

    private void HandleTimerTick(float secondsLeft)
    {
        if (timerText == null) return;
        int totalSeconds = Mathf.CeilToInt(secondsLeft);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
