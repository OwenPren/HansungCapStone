using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/GameStartEvent")]
public class GameStartEventSO : ScriptableObject
{
    public UnityAction OnGameStart;

    public void Raise()
    {
        OnGameStart?.Invoke();
    }
}