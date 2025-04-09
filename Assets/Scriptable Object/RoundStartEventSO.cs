using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Events/RoundStartEvent")]
public class RoundStartEventSO : ScriptableObject
{
    private readonly List<Func<IEnumerator>> listeners = new List<Func<IEnumerator>>();

    public void AddListener(Func<IEnumerator> listener)
    {
        if (!listeners.Contains(listener))
            listeners.Add(listener);
    }

    public void RemoveListener(Func<IEnumerator> listener)
    {
        if (listeners.Contains(listener))
            listeners.Remove(listener);
    }

    public void Raise(MonoBehaviour coroutineRunner)
    {
        foreach (var listener in listeners)
        {
            coroutineRunner.StartCoroutine(listener());
        }
    }
}