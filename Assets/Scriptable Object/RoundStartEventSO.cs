using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Events/RoundStartEvent")]
public class RoundStartEventSO : ScriptableObject
{
    private readonly List<Action>             actionListeners    = new();
    private readonly List<Func<IEnumerator>>  coroutineListeners = new();

    public void AddListener(Action listener)
    {
        if (listener != null && !actionListeners.Contains(listener))
            actionListeners.Add(listener);
    }
    public void RemoveListener(Action listener)
    {
        actionListeners.Remove(listener);
    }

    public void AddListener(Func<IEnumerator> listener)
    {
        if (listener != null && !coroutineListeners.Contains(listener))
            coroutineListeners.Add(listener);
    }
    public void RemoveListener(Func<IEnumerator> listener)
    {
        coroutineListeners.Remove(listener);
    }

    public void Raise(MonoBehaviour coroutineRunner)
    {
        foreach (var act in actionListeners)
            act?.Invoke();

        foreach (var cr in coroutineListeners)
            coroutineRunner.StartCoroutine(cr());
    }
}
