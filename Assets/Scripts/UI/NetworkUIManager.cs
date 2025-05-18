using TMPro;
using UnityEngine;

public class NetworkUIManager : MonoBehaviour
{
    [SerializeField] private TMP_Text timerLabel;

    private void OnEnable () => GameManager.TimerChanged += UpdateTimer;
    private void OnDisable() => GameManager.TimerChanged -= UpdateTimer;

    void UpdateTimer(float value)
    {
        timerLabel.text = Mathf.CeilToInt(value).ToString();
    }
}
