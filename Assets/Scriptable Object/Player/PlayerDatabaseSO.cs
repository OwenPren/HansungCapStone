using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Player Database")]
public class PlayerDatabaseSO : ScriptableObject
{
    public List<PlayerDataSO> players = new();   // 인스턴스만 보관
    public PlayerDataSO Find(Fusion.PlayerRef p)
        => players.Find(d => d.Owner == p);
}
