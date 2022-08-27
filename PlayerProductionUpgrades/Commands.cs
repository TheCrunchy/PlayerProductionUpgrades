using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PlayerProductionUpgrades.Helpers;
using PlayerProductionUpgrades.Models;
using PlayerProductionUpgrades.Models.Upgrades;
using PlayerProductionUpgrades.Patches;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRageMath;

namespace PlayerProductionUpgrades
{
    [Category("upgrades")]
    public class Commands : CommandModule
    {

        [Command("reload", "reload upgrades")]
        [Permission(MyPromoteLevel.Admin)]
        public void ReloadUpgrades()
        {
            Core.ConfigProvider.LoadUpgrades();
            Context.Respond("Done!");
        }

        [Command("buy", "purchase next upgrades")]
        [Permission(MyPromoteLevel.None)]
        public void BuyUpgrades(string UpgradeType)
        {
            if (!Core.Config.EnableBuyingUpgrades)
            {
                Context.Respond("Buying upgrades is not enabled.");
                return;
            }
            var faction = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond("You must make a faction before you can buy upgrades.");
                return;
            }
            if (!Enum.TryParse(UpgradeType, out UpgradeType type)) return;
            var PlayerData = Core.PlayerStorageProvider.GetPlayerData(Context.Player.SteamUserId);
            var level = PlayerData.GetUpgradeLevel(type);
            if (!Core.ConfigProvider.CanUpgrade(level, type))
            {
                Context.Respond("No more upgrades available.");
                return;
            }

            var upgrade = Core.ConfigProvider.GetUpgrade(level + 1, type);

            var gridWithSubGrids = GridFinder.FindLookAtGridGroupMechanical(Context.Player.Character);

            var grids = new List<MyCubeGrid>();
            foreach (var item in gridWithSubGrids)
            {
                foreach (var grid in item.Nodes.Select(groupNodes => groupNodes.NodeData).Where(grid => grid.Projector == null))
                {
                    if (FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid)) != null)
                    {
                        if (!FacUtils.InSameFaction(FacUtils.GetOwner(grid), Context.Player.IdentityId)) continue;
                        if (!grids.Contains(grid))
                            grids.Add(grid);
                    }
                    else
                    {
                        if (!FacUtils.GetOwner(grid).Equals(Context.Player.Identity.IdentityId)) continue;
                        if (!grids.Contains(grid))
                            grids.Add(grid);
                    }
                }
            }
            var invents = new List<VRage.Game.ModAPI.IMyInventory>();
            foreach (var grid in grids)
            {
                invents.AddList(InventoryHelper.GetInventories(grid));
            }

            if (upgrade.MoneyRequired > 0)
            {
                if (EconUtils.GetBalance(Context.Player.IdentityId) >= upgrade.MoneyRequired)
                {
                    if (!InventoryHelper.ConsumeComponents(invents, upgrade.GetItemsRequired(), Context.Player.SteamUserId)) return;
                    EconUtils.TakeMoney(Context.Player.IdentityId, upgrade.MoneyRequired);
                    Core.SendMessage("[Upgrades]", "Upgrading Assembler. Items taken.", Color.LightBlue, (long)Context.Player.SteamUserId);
                    PlayerData.AddUpgradeLevel(type);
                    Core.PlayerStorageProvider.SavePlayerData(PlayerData);
                }
                else
                {
                    Core.SendMessage("[Upgrades]", $"You cant afford the upgrade price of: {upgrade.MoneyRequired:n0}", Color.Red, (long)Context.Player.SteamUserId);
                }
            }
            else
            {
                if (!InventoryHelper.ConsumeComponents(invents, upgrade.GetItemsRequired(), Context.Player.SteamUserId)) return;
                Core.SendMessage("[Upgrades]", "Upgrading Assembler. Items taken.", Color.LightBlue, (long)Context.Player.SteamUserId);
                PlayerData.AddUpgradeLevel(type);
                Core.PlayerStorageProvider.SavePlayerData(PlayerData);
            }
        }

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
