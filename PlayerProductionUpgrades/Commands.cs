using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerProductionUpgrades.Models;
using PlayerProductionUpgrades.Patches;
using PlayerProductionUpgrades.Upgrades;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;

namespace PlayerProductionUpgrades
{
    [Category("upgrades")]
    public class Commands : CommandModule
    {
        [Command("view", "view the upgrades")]
        [Permission(MyPromoteLevel.None)]
        public void ViewUpgrades()
        {
            var sb = new StringBuilder();
            foreach (var upgradeTypes in Core.ConfigProvider.Upgrades)
            {
                foreach (var upgradeLevels in upgradeTypes.Value)
                {
                    sb.AppendLine($"Current Upgrade Level {Core.PlayerStorageProvider.GetPlayerData(Context.Player.SteamUserId).GetUpgradeLevel(upgradeTypes.Key)} for {upgradeLevels.Key}");
                    var upgrade = upgradeLevels.Value;
                    sb.AppendLine("Upgrade number " + upgradeLevels.Key);
                    if (upgrade.MoneyRequired > 0)
                    {
                        sb.AppendLine($"Costs {upgrade.MoneyRequired:n0} SC.");
                    }
                    foreach (var item in upgrade.items.Where(item => item.Enabled))
                    {
                        sb.AppendLine($"Costs {item.RequiredAmount} {item.TypeId} {item.SubTypeId}");
                    }
                    foreach (var buffed in upgrade.BuffedBlocks)
                    {
                        foreach (var block in buffed.buffs.Where(block => block.Enabled))
                        {
                            sb.AppendLine($"Buffs speed for {block.SubtypeId} by {buffed.PercentageBuff * 100}%");
                        }
                    }
                    sb.AppendLine("");
                }
            }

            var message = new DialogMessage("Available upgrades", "", sb.ToString());
            ModCommunication.SendMessageTo(message, Context.Player.SteamUserId);
        }
    }
}
