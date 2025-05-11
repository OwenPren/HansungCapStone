using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// �ֽ� ������ ����ü �Ǵ� Ŭ���� (���� �ڵ� ����)
[System.Serializable]
public class StockData
{
    public string stockName; // �̸�
    public float currentPrice; // ����
}

public class StockMarketManager : MonoBehaviour
{
    // ����Ƽ �����Ϳ��� �ֽ� ��� ����
    public List<StockData> allStocks = new List<StockData>();
    
    void Awake()
    {
        Debug.Log("�ֽ� ���� ��� �ʱ�ȭ...");
        // ���� �̿Ϸ� : �ֽ� ���� 10���� �׸� ���� ��� ����, ����? ������?
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
        return null; // �ش� �̸��� �ֽ��� ���� ���
    }

    // ���� ���� �� ���� �� �ְ� ���� �ùķ��̼� ����
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