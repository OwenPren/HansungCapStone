using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Fusion;


[System.Serializable]
public class PlayerStock
{
    public string stockName; 
    public int quantity; 
}


public class PlayerManager : NetworkBehaviour
{


    [Networked] public float playerCash { get; private set; }
    public PlayerRef PlayerRef { get; private set; }
    public List<PlayerStock> portfolio = new List<PlayerStock>();
    public Sprite character;
    public StockMarketManager stockMarketManager;

    void Start()
    {
        GameObject stockMarketManagerObject = GameObject.Find("StockMarketManager");

        if (stockMarketManagerObject != null)
        {
            stockMarketManager = stockMarketManagerObject.GetComponent<StockMarketManager>();

            if (stockMarketManager != null)
            {
                Debug.Log("StockMarketManager Find Success.");
            }
            else
            {
                Debug.LogError("StockMarketManager Find Fail.");
            }

        }
    }

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
    public void Initialize(float initialCash)
    {
        playerCash = initialCash;

        foreach (string stockName in stockNames)
        { 
            portfolio.Add(new PlayerStock
            {
                stockName = stockName,
                quantity = 0
            });
        }
    }

    public void SetPlayerRef(PlayerRef playerRef){
        this.PlayerRef = playerRef;
    }

    public int GetPlayerStockQuantity(string name)
    {
        var holding = portfolio.Find(h => h.stockName == name);
        return holding != null ? holding.quantity : 0;
    }

    public bool BuyStock(string name, int quantity)
    {
        StockData CurrentStock = stockMarketManager.GetStockData(name);
        if (CurrentStock == null)
        {
            Debug.Log("StockLoad Fail");
            return false;
        }

        float Price = CurrentStock.currentPrice;

        
        if (quantity <= 0)
        {
            return false;
        }

        float cost = quantity * Price;

        if (playerCash >= cost)
        {
            playerCash -= cost;

            var holding = portfolio.Find(h => h.stockName == name);
            if (holding != null)
            {
                holding.quantity += quantity;
            }
            return true;
        }
        else
        {
            return false;
        }
    }

 
    public bool SellStock(string name, int quantity)
    {
        StockData CurrentStock = stockMarketManager.GetStockData(name);
        if (CurrentStock == null)
        {
            Debug.Log("StockLoad Fail");
            return false;
        }

        float Price = CurrentStock.currentPrice;

        if (quantity <= 0)
        {
            return false;
        }

        var holding = portfolio.Find(h => h.stockName == name);

        if (holding != null && holding.quantity >= quantity)
        {
            float revenue = quantity * Price;
            playerCash += revenue;

          
            holding.quantity -= quantity;
            if (holding.quantity == 0)
            {
                portfolio.Remove(holding); 
            }

            return true; 
        }
        else
        {
            return false; 
        }
    }
}