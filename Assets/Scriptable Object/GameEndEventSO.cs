using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/GameEndEvent")]
public class GameEndEventSO : ScriptableObject
{
    public UnityAction OnGameEnd;

    public void Raise()
    {
        OnGameEnd?.Invoke();
    }
}