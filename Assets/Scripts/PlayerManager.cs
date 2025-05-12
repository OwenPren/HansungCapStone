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

    public void Initialize(float initialCash)
    {
        playerCash = initialCash;

        portfolio.Add(new PlayerStock { stockName = "Energy", quantity = 0 });
        portfolio.Add(new PlayerStock { stockName = "Technology", quantity = 0 });
        portfolio.Add(new PlayerStock { stockName = "Finance", quantity = 0 });
        portfolio.Add(new PlayerStock { stockName = "Healthcare", quantity = 0 });
        portfolio.Add(new PlayerStock { stockName = "ConsumerDiscretionary", quantity = 0 });
        portfolio.Add(new PlayerStock { stockName = "ConsumerStaples", quantity = 0 });
        portfolio.Add(new PlayerStock { stockName = "Telecom", quantity = 0 });
        portfolio.Add(new PlayerStock { stockName = "Industrials", quantity = 0 });
        portfolio.Add(new PlayerStock { stockName = "Materials", quantity = 0 });
        portfolio.Add(new PlayerStock { stockName = "RealEstate", quantity = 0 });
    }

    public void SetPlayerRef(PlayerRef playerRef){
        this.PlayerRef = playerRef;
    }

    public int GetPlayerStockQuantity(string name)
    {
        var holding = portfolio.Find(h => h.stockName == name);
        return holding != null ? holding.quantity : 0;
    }

    public bool BuyStock(string name, int quantity, float currentPrice)
    {
        
        if (quantity <= 0)
        {
            return false;
        }

        float cost = quantity * currentPrice;

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

 
    public bool SellStock(string name, int quantity, float currentPrice)
    {
        if (quantity <= 0)
        {
            return false;
        }

        var holding = portfolio.Find(h => h.stockName == name);

        if (holding != null && holding.quantity >= quantity)
        {
            float revenue = quantity * currentPrice;
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