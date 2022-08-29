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
using Sandbox.Game.Entities.Blocks;
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

        public void HandleAssembleHourPurchase(PlayerData Data, int Hours, long PlayerId)
        {
            var level = Data.GetUpgradeLevel(UpgradeType.AssemblerSpeed);
            if (level <= 0)
            {
                Context.Respond("You have not purchased any assembler upgrades.");
                return;
            }
            
            var upgrade = Core.ConfigProvider.GetUpgrade(level, UpgradeType.AssemblerSpeed);
            if (upgrade == null)
            {
                Context.Respond($"There is an error obtaining that assembler speed upgrade level {level}. ");
                return;
            }

            var cost = upgrade.PricePerHour * Hours;
            var max = upgrade.MaxBuyableHours;
            var current = 0;
            if (DateTime.Now > Data.PricePerHourEndTimeAssembler)
            {
                current = 0;
            }
            else
            {
                current = (int)(Data.PricePerHourEndTimeAssembler - DateTime.Now).TotalHours;
            }
            if (current > max)
            {
                Context.Respond("Maximum buyable achieved.");
                return;
            }
            if (current + Hours > max)
            {
                Context.Respond($"You can only buy {max - current} hours.");
                return;
            }

            if (EconUtils.GetBalance(PlayerId) < cost)
            {
                Context.Respond($"You cannot afford the cost of {cost:n0)}");
                return;
            }
            Context.Respond("Hours added!");
            EconUtils.TakeMoney(PlayerId, cost);
            Data.PricePerHourEndTimeAssembler = Data.PricePerHourEndTimeAssembler.AddHours(Hours);
            Core.PlayerStorageProvider.SavePlayerData(Data);
        }
        public void HandleRefineryHourPurchase(PlayerData Data, int Hours, long PlayerId)
        {
            var upgradeType = UpgradeType.RefinerySpeed;
            var level = Data.GetUpgradeLevel(UpgradeType.RefineryYield);
            var level2 = Data.GetUpgradeLevel(UpgradeType.RefinerySpeed);
            if (level >= level2)
            {
                upgradeType = UpgradeType.RefineryYield;
            }
            else
            {
                level = level2;
                upgradeType = UpgradeType.RefinerySpeed;
            }

            if (level <= 0)
            {
                Context.Respond("You have not purchased any refinery upgrades.");
                return;
            }
            var upgrade = Core.ConfigProvider.GetUpgrade(level, upgradeType);
            if (upgrade == null)
            {
                Context.Respond($"There is an error obtaining that {upgradeType} upgrade level {level}. ");
                return;
            }

            var cost = upgrade.PricePerHour * Hours;
            var max = upgrade.MaxBuyableHours;
            var current = 0;
            if (DateTime.Now > Data.PricePerHourEndTimeRefinery)
            {
                current = 0;
            }
            else
            {
                current = (int)(Data.PricePerHourEndTimeRefinery - DateTime.Now).TotalHours;
            }
            if (current > max)
            {
                Context.Respond("Maximum buyable achieved.");
                return;
            }
            if (current + Hours > max)
            {
                Context.Respond($"You can only buy {max - current} hours.");
                return;
            }

            if (EconUtils.GetBalance(PlayerId) < cost)
            {
                Context.Respond($"You cannot afford the cost of {cost:n0)}");
                return;
            }
            Context.Respond("Hours added!");
            EconUtils.TakeMoney(PlayerId, cost);
            Data.PricePerHourEndTimeAssembler = Data.PricePerHourEndTimeAssembler.AddHours(Hours);
            Core.PlayerStorageProvider.SavePlayerData(Data);
        }

        [Command("hours", "purchase hours")]
        [Permission(MyPromoteLevel.None)]
        public void BuyHours(string Type, int hours)
        {
            var data = Core.PlayerStorageProvider.GetPlayerData(Context.Player.SteamUserId);
            switch (Type.ToLower())
            {
                case "assembler":
                    HandleAssembleHourPurchase(data, hours, Context.Player.IdentityId);
                    break;
                case "refinery":
                    HandleRefineryHourPurchase(data, hours, Context.Player.IdentityId);
                    break;
                default:
                    Context.Respond("Available types are Assembler and Refinery");
                    return;
            }
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
            var playerData = Core.PlayerStorageProvider.GetPlayerData(Context.Player.SteamUserId);
            var level = playerData.GetUpgradeLevel(type);
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
                    Core.SendMessage("[Upgrades]", "Upgrading. Items taken.", Color.LightBlue, (long)Context.Player.SteamUserId);
                    playerData.AddUpgradeLevel(type);
                    Core.PlayerStorageProvider.SavePlayerData(playerData);
                }
                else
                {
                    Core.SendMessage("[Upgrades]", $"You cant afford the upgrade price of: {upgrade.MoneyRequired:n0}", Color.Red, (long)Context.Player.SteamUserId);
                }
            }
            else
            {
                if (!InventoryHelper.ConsumeComponents(invents, upgrade.GetItemsRequired(), Context.Player.SteamUserId)) return;
                Core.SendMessage("[Upgrades]", "Upgrading. Items taken.", Color.LightBlue, (long)Context.Player.SteamUserId);
                playerData.AddUpgradeLevel(type);
                Core.PlayerStorageProvider.SavePlayerData(playerData);
            }
        }

        [Command("view", "view the upgrades")]
        [Permission(MyPromoteLevel.None)]
        public void ViewUpgrades()
        {
            var sb = new StringBuilder();
            var playerData = Core.PlayerStorageProvider.GetPlayerData(Context.Player.SteamUserId);
            if (Core.Config.MakePlayersPayPerHour)
            {
                if (DateTime.Now < playerData.PricePerHourEndTimeAssembler)
                {
                    sb.AppendLine($"Refinery Hours: {(playerData.PricePerHourEndTimeAssembler - DateTime.Now).TotalHours}");
                }
                if (DateTime.Now < playerData.PricePerHourEndTimeRefinery)
                {
                    sb.AppendLine($"Assembler Hours: {(playerData.PricePerHourEndTimeRefinery - DateTime.Now).TotalHours}");
                }
            }
            foreach (var upgradeTypes in Core.ConfigProvider.Upgrades)
            {
                foreach (var (k, upgrade) in upgradeTypes.Value)
                {
                    sb.AppendLine($"Current Upgrade Level {playerData.GetUpgradeLevel(upgradeTypes.Key)} for {k}");
                    sb.AppendLine("Upgrade number " + k);
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
