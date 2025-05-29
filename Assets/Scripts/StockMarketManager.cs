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
        allStocks.Clear(); // 기존 데이터 초기화

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

            Debug.Log($"[StockMarketManager] Initialized Stock: {stockName} with price {randomPrice:N2}");
        }

        Debug.Log($"[StockMarketManager] All stocks initialized. Total: {allStocks.Count}");
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
            return;
        }

        foreach (StockData currentStockData in allStocks)
        {
            if (currentStockData == null)
            {
                Debug.LogWarning($"Stock data not found for {currentStockData.stockName}. Skipping.");
                continue;
            }

            // 변동률 계산 전에 이전 가격 업데이트
            if (currentStockData.previousPrice > 0)
            {
                currentStockData.stockChangeRate = (100.0f * currentStockData.currentPrice) / currentStockData.previousPrice - 100.0f;
            }
            else
            {
                currentStockData.stockChangeRate = 0.0f;
            }

            Debug.Log($"[StockMarketManager] Server updated {currentStockData.stockName}: Change Rate: {currentStockData.stockChangeRate:F2}%, Current: {currentStockData.currentPrice:F1}, Previous: {currentStockData.previousPrice:F1}");
        }
    }

    public void ClientPriceUpdate()
    {
        if (allStocks == null)
        {
            Debug.LogError("allStocks is null!");
            return;
        }

        foreach (StockData currentStockData in allStocks)
        {
            if (currentStockData == null)
            {
                Debug.LogWarning($"Stock data is null. Skipping.");
                continue;
            }

            // 이전 가격이 0이 아닌 경우에만 변동률 계산
            if (currentStockData.previousPrice > 0)
            {
                currentStockData.stockChangeRate = (100.0f * currentStockData.currentPrice) / currentStockData.previousPrice - 100.0f;
            }
            else
            {
                currentStockData.stockChangeRate = 0.0f;
            }

            Debug.Log($"[StockMarketManager] Client updated {currentStockData.stockName}: {currentStockData.stockChangeRate:F2}%");
        }
    }

    public void SetStockData(string stockName, float currentPrice, float previousPrice, float changeRate)
    {
        StockData stock = allStocks.Find(s => s.stockName == stockName);
        if (stock != null)
        {
            stock.currentPrice = currentPrice;
            stock.previousPrice = previousPrice;
            stock.stockChangeRate = changeRate;
            Debug.Log($"[StockMarketManager] Client stock data set: {stockName} = {currentPrice:F1}원, {changeRate:F2}%");
        }
        else
        {
            Debug.LogError($"[StockMarketManager] Stock not found: {stockName}");
        }
    }

    public List<(string name, float currentPrice, float previousPrice, float changeRate)> GetAllStockData()
    {
        var stockDataList = new List<(string, float, float, float)>();

        foreach (var stock in allStocks)
        {
            stockDataList.Add((stock.stockName, stock.currentPrice, stock.previousPrice, stock.stockChangeRate));
        }

        return stockDataList;
    }
}