using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json.Linq;

[System.Serializable]
public class JObjectEvent : UnityEvent<JObject> {}

[CreateAssetMenu(menuName = "Events/FunctionCallArgumentsEvents")]
public class FunctionCallArgumentsEvents : ScriptableObject
{
    public JObjectEvent OnEventRaised;

    public void RaiseEvent(JObject args)
    {
        if (OnEventRaised != null)
            OnEventRaised.Invoke(args);
    }
}