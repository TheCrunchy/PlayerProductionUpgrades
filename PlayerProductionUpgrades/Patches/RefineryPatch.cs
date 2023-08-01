using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PlayerProductionUpgrades.Models;
using PlayerProductionUpgrades.Models.Upgrades;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;

namespace PlayerProductionUpgrades.Patches
{
    [PatchShim]
    public static class RefineryPatch
    {
        internal static readonly MethodInfo update =
        typeof(MyRefinery).GetMethod("ChangeRequirementsToResults", BindingFlags.Instance | BindingFlags.NonPublic) ??
        throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo patch =
            typeof(RefineryPatch).GetMethod(nameof(ChangeRequirementsToResults), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        public static MethodInfo RemoveQueue;
        public static void Patch(PatchContext ctx)
        {

            ctx.GetPattern(update).Prefixes.Add(patch);
        }

        public static double GetBuff(long PlayerId, MyRefinery Refinery)
        {
            double buff = 1;
            var SteamId = MySession.Static.Players.TryGetSteamId(PlayerId);
            if (SteamId == 0L)
            {
                return 1;
            }

            var playerData = Core.PlayerStorageProvider.GetPlayerData(SteamId);
            var upgradeLevel = playerData.GetUpgradeLevel(UpgradeType.RefineryYield);
            if (upgradeLevel > 0)
            {
                var upgrade = Core.ConfigProvider.GetUpgrade(upgradeLevel, UpgradeType.RefineryYield);
                if (upgrade == null) return buff;
                if (Core.Config.MakePlayersPayPerHour)
                {
                    if (DateTime.Now < playerData.PricePerHourEndTimeRefinery)
                    {
                        var subType = Refinery.BlockDefinition.Id.SubtypeName;
                        var temp = (float)upgrade.BuffedBlocks.FirstOrDefault(x => x.buffs.Any(z => z.Enabled && z.SubtypeId == subType))?.PercentageBuff;
                        buff += temp;
                    }
                }
                else
                {
                    var subType = Refinery.BlockDefinition.Id.SubtypeName;
                    var temp = (float)upgrade.BuffedBlocks.FirstOrDefault(x => x.buffs.Any(z => z.Enabled && z.SubtypeId == subType))?.PercentageBuff;
                    buff += temp;
                }
            }

            if (Core.Config.DoVoteBuffs)
            {
                if (DateTime.Now <= playerData.VoteBuffedUntil)
                {
                    buff += Core.Config.VoteBuff;
                }
                else
                {
                    playerData.AddToChecking();
                }
            }
            if (!Core.Config.EnableAlliancePluginBuffs || !Core.AlliancePluginInstalled) return (float)buff;
            var methodInput = new object[] { PlayerId, Refinery };
            if (Core.GetAllianceRefineryModifier == null)
            {
                return buff;
            }
            var multiplier = (double)Core.GetAllianceRefineryModifier.Invoke(null, methodInput);
            return (float)(buff *= multiplier);

        }

        public static double GetSpeedBuff(long PlayerId, MyRefinery Refinery)
        {
            float buff = 1;
            var steamId = MySession.Static.Players.TryGetSteamId(PlayerId);
            if (steamId <= 0L) return buff;
            var playerData = Core.PlayerStorageProvider.GetPlayerData(steamId);
            var upgradeLevel = playerData.GetUpgradeLevel(UpgradeType.RefinerySpeed);

            if (Core.Config.DoVoteBuffs)
            {
                if (DateTime.Now <= playerData.VoteBuffedUntil)
                {
                    buff += Core.Config.VoteBuff;
                }
                else
                {
                    playerData.AddToChecking();
                }
            }
            //  Core.Log.Info($"{buff}");
            if (upgradeLevel > 0)
            {
                if (Core.Config.MakePlayersPayPerHour)
                {
                    if (DateTime.Now > playerData.PricePerHourEndTimeRefinery)
                    {
                        return buff;
                    }
                }
                var upgrade = Core.ConfigProvider.GetUpgrade(upgradeLevel, UpgradeType.RefinerySpeed);
                if (upgrade == null) return buff;
                var subType = Refinery.BlockDefinition.Id.SubtypeName;
                var temp = (float)upgrade.BuffedBlocks.FirstOrDefault(x => x.buffs.Any(z => z.Enabled && z.SubtypeId == subType))?.PercentageBuff;
                buff += temp;
            }
            else
            {
                return buff;
            }
            return buff;
        }

        public static Boolean ChangeRequirementsToResults(MyBlueprintDefinitionBase queueItem, MyFixedPoint blueprintAmount, MyRefinery __instance)
        {
            if (__instance.BlockDefinition as MyRefineryDefinition == null)
            {
                return false;
            }

            var speedBuff = GetSpeedBuff(__instance.OwnerId, __instance);
            double buff = GetBuff(__instance.OwnerId, __instance);
            var steamId = MySession.Static.Players.TryGetSteamId(__instance.OwnerId);
            float offlineBuff = 1;
            if (steamId > 0L)
            {
                var playerData = Core.PlayerStorageProvider.GetPlayerData(steamId);
                offlineBuff = playerData.GetOfflineBuff();
            }

            speedBuff *= offlineBuff;
            if (Core.IsPlayerClustered(__instance.OwnerId, __instance.CubeGrid) && Core.Config.NerfClusteredGrids)
            {
                speedBuff *= Core.Config.ClusterNerfDefaultLoses75Percent;
                buff *= Core.Config.ClusterNerfDefaultLoses75Percent;
            }

            blueprintAmount *= (MyFixedPoint)speedBuff;



            if (!Sync.IsServer || MySession.Static == null || (queueItem == null || queueItem.Prerequisites == null) || (__instance.OutputInventory == null || __instance.InputInventory == null || (queueItem.Results == null)))
                return false;
            if (!MySession.Static.CreativeMode)
                blueprintAmount = MyFixedPoint.Min(__instance.OutputInventory.ComputeAmountThatFits(queueItem), blueprintAmount);
            if (blueprintAmount == (MyFixedPoint)0)
                return false;


            foreach (var prerequisite in queueItem.Prerequisites)
            {
                if ((!(MyObjectBuilderSerializer.CreateNewObject((SerializableDefinitionId)prerequisite.Id) is
                        MyObjectBuilder_PhysicalObject newObject))) continue;

                __instance.InputInventory.RemoveItemsOfType((MyFixedPoint)((float)blueprintAmount * (float)prerequisite.Amount), newObject, false, false);
                var itemAmount = __instance.InputInventory.GetItemAmount(prerequisite.Id, MyItemFlags.None, false);
                if (itemAmount < (MyFixedPoint)0.01f)
                    __instance.InputInventory.RemoveItemsOfType(itemAmount, prerequisite.Id, MyItemFlags.None, false);
            }
            foreach (var result in queueItem.Results)
            {
                if ((!(MyObjectBuilderSerializer.CreateNewObject((SerializableDefinitionId)result.Id) is
                        MyObjectBuilder_PhysicalObject newObject))) continue;

                var def = __instance.BlockDefinition as MyRefineryDefinition;
                var num = (float)result.Amount * def.MaterialEfficiency * __instance.UpgradeValues["Effectiveness"];
                __instance.OutputInventory.AddItems((MyFixedPoint)((float)blueprintAmount * num * buff), (MyObjectBuilder_Base)newObject);
            }

            if (RemoveQueue == null)
            {
                Type change = __instance.GetType().Assembly.GetType("Sandbox.Game.Entities.Cube.MyProductionBlock");
                RemoveQueue = change.GetMethod("RemoveFirstQueueItemAnnounce", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            var MethodInput = new object[] { blueprintAmount, 0.0f };
            RemoveQueue?.Invoke(__instance, MethodInput);

            return false;
        }
    }
}

