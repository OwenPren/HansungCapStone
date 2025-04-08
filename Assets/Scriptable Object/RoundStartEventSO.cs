using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/RoundStartEvent")]
public class RoundStartEventSO : ScriptableObject
{
    public UnityAction OnRoundStart;

    public void Raise()
    {
        OnRoundStart?.Invoke();
    }
}