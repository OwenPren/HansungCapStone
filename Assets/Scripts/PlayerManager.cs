using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Fusion;


[System.Serializable]
public class PlayerStock
{
    public string stockName;
    public int quantity;
    public float usedMoney;
    public float stockReturn;
}


public class PlayerManager : NetworkBehaviour
{
    [Networked] public float playerCash { get; private set; }
    [Networked] public float playerValue { get; private set; }
    [Networked] public float previousValue { get; private set; }
    [Networked] public float portfolioReturn { get; private set; }
    public PlayerRef PlayerRef { get; private set; }
    public string NameField;
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
        playerValue = initialCash;
        previousValue = initialCash;
        portfolioReturn = 0.0f;
        NameField = PlayerData.instance.nickname;
        foreach (string stockName in stockNames)
        {
            portfolio.Add(new PlayerStock
            {
                stockName = stockName,
                quantity = 0,
                usedMoney = 0,
                stockReturn = 0
            });
        }
    }

    [Networked] public string NetworkedNickname { get; private set; }
    [Networked] public string NetworkedUserID { get; private set; }
    [Networked] public int NetworkedCharacterIndex { get; private set; }

    public void UpdatePlayerInfo(string userID, string nickname, int characterIndex)
    {
        Debug.Log($"[PlayerManager] UpdatePlayerInfo called - UserID: '{userID}', Nickname: '{nickname}', CharIndex: {characterIndex}");
        Debug.Log($"[PlayerManager] HasStateAuthority: {Object.HasStateAuthority}, HasInputAuthority: {Object.HasInputAuthority}");
        
        if (Object.HasStateAuthority)
        {
            Debug.Log($"[PlayerManager] Has state authority, updating networked variables...");
            NetworkedNickname = nickname;
            NetworkedUserID = userID;
            NetworkedCharacterIndex = characterIndex;
            Debug.Log($"[PlayerManager] Networked variables updated successfully");
        }
        else
        {
            Debug.LogWarning($"[PlayerManager] Does NOT have state authority, cannot update networked variables");
        }
        
        NameField = nickname;
        Debug.Log($"[PlayerManager] NameField set to: '{NameField}'");
        
        Debug.Log($"[PlayerManager] Player info update completed for: {nickname}");
    }

    public void SetPreviousValue()
    {
        previousValue = playerValue;
    }

    public void SetPlayerRef(PlayerRef playerRef)
    {
        this.PlayerRef = playerRef;
    }

    public void UpdatePortfolioReturn()
    {
        portfolioReturn = (100 * (playerValue / previousValue)) - 100.00f;
    }

    public int GetPlayerStockQuantity(string name)
    {
        var holding = portfolio.Find(h => h.stockName == name);
        return holding != null ? holding.quantity : 0;
    }

    public void ValuationUpdate(List<PlayerStock> portfolio)
    {
        float StockValuation = 0f;

        if (portfolio == null)
        {
            Debug.LogError("Portfolio is null!");
            //return 0f;
        }

        foreach (PlayerStock playerStock in portfolio)
        {
            StockData currentStock = stockMarketManager.GetStockData(playerStock.stockName);

            if (currentStock == null)
            {
                Debug.LogWarning($"Stock data not found for {playerStock.stockName}. Skipping.");
                continue;
            }

            float stockValue = (float)playerStock.quantity * currentStock.currentPrice;

            playerStock.stockReturn = (100.0f*stockValue)/playerStock.usedMoney-100.0f;

            StockValuation += stockValue;
        }

        playerValue = StockValuation + this.playerCash;

        //return StockValuation;
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
                holding.usedMoney += cost;
                ValuationUpdate(portfolio);
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
            holding.usedMoney -= (((float)quantity/holding.quantity) * holding.usedMoney);
            holding.quantity -= quantity;
            ValuationUpdate(portfolio);
            return true;
        }
        else
        {
            return false;
        }
        
    }
    
}