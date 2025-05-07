using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Player Default Data")]
public class PlayerDefaultDataSO : ScriptableObject
{
    public int startMoney = 5_000_000;

    [Header("초기 주식 보유량")]
    public List<Stock> startStocks;

    [System.Serializable]
    public struct Stock
    {
        public SectorType sector;
        public int amount;
    }
}