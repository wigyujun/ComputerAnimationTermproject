using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [SerializeField] private int coin = 0;

    public int Coin => coin;

    public void AddCoin(int amount)
    {
        coin += amount;
        if (coin < 0)
            coin = 0;
    }

    public bool SpendCoin(int amount)
    {
        if (coin < amount)
            return false;

        coin -= amount;
        return true;
    }
}
