using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// �ֽ� ������ ����ü �Ǵ� Ŭ���� (���� �ڵ� ����)
[System.Serializable]
public class StockData
{
    public string stockName; // 
    public float currentPrice; // 
    public float previousPrice;  //
    public float stockChangeRate; // 
}

public class StockMarketManager : MonoBehaviour
{
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
        Debug.Log("�ֽ� ���� ��� �ʱ�ȭ...");
        InitializeStocks();
    }

    void InitializeStocks()
    {
        allStocks.Clear(); // ���� ������ �ʱ�ȭ

        foreach (string stockName in stockNames)
        {
            float randomPrice = UnityEngine.Random.Range(10000f, 50000f);

            allStocks.Add(new StockData
            {
                stockName = stockName,
                currentPrice = randomPrice, 
                previousPrice = randomPrice, 
                stockChangeRate = 0.0f
            });

            Debug.Log($"Initialized Stock: {stockName} with price {randomPrice:N2}"); 
        }
    }

    public StockData GetStockData(string name)
    {
        StockData stock = allStocks.Find(s => s.stockName == name);
        if (stock != null)
        {
            return stock;
        }
        return null; 
    }

    
    public void PriceChange(string affectedSectors, string impactDirection)
    {
        if (affectedSectors != null)
        {
            StockData stock = allStocks.Find(s => s.stockName == affectedSectors);
            if (impactDirection == "+") stock.currentPrice = stock.currentPrice * 1.1f;
            else stock.currentPrice = stock.currentPrice * 0.9f;
        }
    }

    public void PriceUpdate()
    {
        if (allStocks == null)
        {
            Debug.LogError("allStocks is null!");
        }

        foreach (StockData currentStockData in allStocks)
        {
            if (currentStockData == null)
            {
                Debug.LogWarning($"Stock data not found for {currentStockData.stockName}. Skipping.");
                continue;
            }

            currentStockData.stockChangeRate = (100.0f * currentStockData.currentPrice) / currentStockData.previousPrice - 100.0f;
        }
    }
}