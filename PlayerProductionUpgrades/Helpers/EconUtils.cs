using System;
using Sandbox.Game.GameSystems.BankingAndCurrency;

namespace PlayerProductionUpgrades.Helpers
{
    public class EconUtils
    {
        public static long GetBalance(long walletID)
        {
            MyAccountInfo info;
            return MyBankingSystem.Static.TryGetAccountInfo(walletID, out info) ? info.Balance : 0L;
        }
        public static void AddMoney(long walletID, long amount)
        {
            MyBankingSystem.ChangeBalance(walletID, amount);
        }
        public static void TakeMoney(long walletID, long amount)
        {
            if (GetBalance(walletID) < amount) return;
            amount = amount * -1;
            MyBankingSystem.ChangeBalance(walletID, amount);
        }
    }
}