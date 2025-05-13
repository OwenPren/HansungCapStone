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

    private List<string> stockNames = new List<string>
    {
        "Energy",
        "Technology",
        "Finance",
        "Healthcare",
        "ConsumerDiscretionary",
        "ConsumerStaples",
        "Telecom",
        "Industrials",
        "Materials",
        "RealEstate"
    };

    void Awake()
    {
        Debug.Log("주식 시장 목록 초기화...");
        InitializeStocks();
    }

    void InitializeStocks()
    {
        allStocks.Clear(); // 기존 데이터 초기화

        foreach (string stockName in stockNames)
        {
            // 100.0f에서 1000.0f 사이의 랜덤 가격 생성
            // UnityEngine.Random.Range(float minInclusive, float maxInclusive)는 min과 max 모두 포함합니다.
            float randomPrice = UnityEngine.Random.Range(100f, 1000f);

            allStocks.Add(new StockData
            {
                stockName = stockName,
                currentPrice = randomPrice // 생성된 랜덤 가격 할당
            });

            Debug.Log($"Initialized Stock: {stockName} with price {randomPrice:N2}"); // 확인을 위해 로그 출력
        }
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