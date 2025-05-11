using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 플레이어 인벤토리 항목 구조체 또는 클래스
[System.Serializable]
public class PlayerStock
{
    public string stockName; // 주식 종목 이름
    public int quantity; // 보유 수량
}

// 주식 항목 구조체
public class PlayerManager : MonoBehaviour
{
    public float playerCash = 0f; 
    public List<PlayerStock> portfolio = new List<PlayerStock>();

    // GameManager에서 호출되어 초기 자금 등을 설정
    public void Initialize(float initialCash)
    {
        playerCash = initialCash;

        //| Energy | 에너지 |
        //| Technology | 기술 |
        //| Finance | 금융 |
        //| Healthcare | 의료 |
        //| ConsumerDiscretionary | 임의소비재 |
        //| ConsumerStaples | 필수소비재 |
        //| Telecom | 통신 |
        //| Industrials | 산업재 |
        //| Materials | 소재 |
        //| RealEstate | 부동산 |

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

    // 특정 주식 보유량 가져오기
    public int GetPlayerStockQuantity(string name)
    {
        var holding = portfolio.Find(h => h.stockName == name);
        return holding != null ? holding.quantity : 0;
    }

    // 주식 매수 로직 (MarketPanel2UI에서 호출됨)
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

            // 포트폴리오 업데이트
            var holding = portfolio.Find(h => h.stockName == name);
            if (holding != null)
            {
                holding.quantity += quantity;
            }

            // TODO: 매수 성공 UI 피드백
            return true; // 매수 성공
        }
        else
        {
            // TODO: 매수 실패 UI 피드백
            return false; // 매수 실패 (자금 부족)
        }
    }

    // 주식 매도 로직 (MarketPanel2UI에서 호출됨)
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

            // 포트폴리오 업데이트
            holding.quantity -= quantity;
            if (holding.quantity == 0)
            {
                portfolio.Remove(holding); 
            }

            // TODO: 매도 성공 UI 피드백
            return true; // 매도 성공
        }
        else
        { 
            // TODO: 매도 실패 UI 피드백
            return false; // 매도 실패 (수량 부족 또는 주식 미보유)
        }
    }
}