using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 주식 데이터 구조체 또는 클래스 (기존 코드 유지)
[System.Serializable]
public class StockData
{
    public string stockName; // 이름
    public float currentPrice; // 가격
}

public class StockMarketManager : MonoBehaviour
{
    // 유니티 에디터에서 주식 목록 설정
    public List<StockData> allStocks = new List<StockData>();
    
    void Awake()
    {
        Debug.Log("주식 시장 목록 초기화...");
        // 구현 미완료 : 주식 시장 10개의 항목에 대한 목록 구성, 랜덤? 고정값?
        allStocks.Add(new StockData { stockName = "Energy", currentPrice = 0 });
        allStocks.Add(new StockData { stockName = "Technology", currentPrice = 0 });
        allStocks.Add(new StockData { stockName = "Finance", currentPrice = 0 });
        allStocks.Add(new StockData { stockName = "Healthcare", currentPrice = 0 });
        allStocks.Add(new StockData { stockName = "ConsumerDiscretionary", currentPrice = 0 });
        allStocks.Add(new StockData { stockName = "ConsumerStaples", currentPrice = 0 });
        allStocks.Add(new StockData { stockName = "Telecom", currentPrice = 0 });
        allStocks.Add(new StockData { stockName = "Industrials", currentPrice = 0 });
        allStocks.Add(new StockData { stockName = "Materials", currentPrice = 0 });
        allStocks.Add(new StockData { stockName = "RealEstate", currentPrice = 0 });
    }

    public StockData GetStockData(string name)
    {
        StockData stock = allStocks.Find(s => s.stockName == name);
        if (stock != null)
        {
            return stock;
        }
        return null; // 해당 이름의 주식이 없을 경우
    }

    // 라운드 시작 및 종료 시 주가 변동 시뮬레이션 로직
    public void PriceChange(string affectedSectors, string impactDirection)
    {
        if (affectedSectors != null)
        {
            StockData stock = allStocks.Find(s => s.stockName == affectedSectors);
            if (impactDirection == "+") stock.currentPrice = stock.currentPrice * 1.01f;
            else stock.currentPrice = stock.currentPrice * 0.99f;
        }
    }
}