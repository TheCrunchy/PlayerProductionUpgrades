using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PlayerProductionUpgrades.Models.Upgrades;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;

namespace PlayerProductionUpgrades.Patches
{
    [PatchShim]
    public class MyStorePatch
    {
        internal static readonly MethodInfo update =
            typeof(MyStoreBlock).GetMethod("BuyFromPlayer", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo storePatch =
            typeof(MyStorePatch).GetMethod(nameof(StorePatchMethod), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        public static Dictionary<ulong, DateTime> Confirmations = new Dictionary<ulong, DateTime>();

        public static Boolean StorePatchMethod(long id, int amount, long targetEntityId, MyPlayer player, MyAccountInfo playerAccountInfo, MyStoreBlock __instance)
        {
            if (!(__instance is MyStoreBlock store)) 
                return true;

            if (store.SubBlockName != "UpgradeStore") 
                return true;



            MyStoreItem storeItem = (MyStoreItem)null;
            foreach (MyStoreItem playerItem in store.PlayerItems)
            {
                if (playerItem.Id == id)
                {
                    storeItem = playerItem;
                    break;
                }
            }
            if (storeItem == null)
            {

                return true;
            }
            var upgradeType = GetUpgradeType(storeItem.Item.Value.SubtypeName);
            if (upgradeType is UpgradeType.Null)
            {
                return false;
            }

            if (Confirmations.TryGetValue(player.Id.SteamId, out var time))
            {
                if (DateTime.Now > time)
                {
                    Confirmations[player.Id.SteamId] = DateTime.Now.AddSeconds(30);
                    //do buy again to confirm message 
                    return false;
                }
            }
            else
            {
                Confirmations.Add(player.Id.SteamId,DateTime.Now.AddSeconds(30));
                return false;
            }

            return false;
        }

        public static UpgradeType GetUpgradeType(string stuff)
        {
            switch (stuff)
            {
                case "RefinerySpeed":
                    return UpgradeType.RefinerySpeed;
                case "RefineryYield":
                    return UpgradeType.RefineryYield;
                case "AssemblerSpeed":
                    return UpgradeType.AssemblerSpeed;
            }

            return UpgradeType.Null;
        }
    }
}
