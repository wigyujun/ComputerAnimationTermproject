using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public int Coin => RunContext.Coin;

    public void AddCoin(int amount)
    {
        if (amount <= 0)
            return;

        RunContext.AddCoin(amount);
    }

    public bool SpendCoin(int amount)
    {
        return RunContext.TrySpendCoin(amount);
    }

    public void SetCoin(int value)
    {
        RunContext.SetCoin(value);
    }
}
