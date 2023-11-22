using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using PlayerProductionUpgrades.Models.Upgrades;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;

namespace PlayerProductionUpgrades.Patches
{
    [PatchShim]
    public class AssemblerPatch
    {
        internal static readonly MethodInfo update =
            typeof(MyAssembler).GetMethod("CalculateBlueprintProductionTime", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");


        public static void Patch(PatchContext ctx)
        {
            var harmony = new Harmony("Crunch.Assembler.Patch");
            harmony.PatchAll();
        }

        private static float GetPlayerBuffs(long PlayerId, MyAssembler Assembler)
        {
            var buff = 0f;

            var steamId = MySession.Static.Players.TryGetSteamId(PlayerId);
            if (steamId <= 0L) return buff;
            var playerData = Core.PlayerStorageProvider.GetPlayerData(steamId);
            var upgradeLevel = playerData.GetUpgradeLevel(UpgradeType.AssemblerSpeed);
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
            if (upgradeLevel > 0)
            {
             
                var upgrade = Core.ConfigProvider.GetUpgrade(upgradeLevel, UpgradeType.AssemblerSpeed);
         
                if (upgrade == null) return buff;

                var subType = Assembler.BlockDefinition.Id.SubtypeName;
                var percentageBuff = upgrade.GetBuffValue(subType);
                if (percentageBuff != 0)
                {
                    var temp = (float)percentageBuff;
                    if (Core.Config.MakePlayersPayPerHour)
                    {
                        if (DateTime.Now > playerData.PricePerHourEndTimeAssembler)
                        {
                            temp = (float)(temp * 0.5);
                        }
                    }
                    buff += temp;
                }

            }
            return buff;
        }

        private static float GetBuff(long PlayerId, MyAssembler Assembler)
        {
            double buff = 1;

            buff += GetPlayerBuffs(PlayerId, Assembler);
            var steamId = MySession.Static.Players.TryGetSteamId(PlayerId);
            if (steamId <= 0L) return (float)buff;
            var playerData = Core.PlayerStorageProvider.GetPlayerData(steamId);
            if (!Core.AlliancePluginInstalled) return (float)buff;
            var methodInput = new object[] { PlayerId, Assembler };
            var multiplier = (double)Core.GetAllianceAssemblerModifier.Invoke(Core.Alliances, methodInput);
            return (float)(buff *= multiplier) * playerData.GetOfflineBuff();
        }

        public static float PatchMethod(MyAssembler __instance, MyBlueprintDefinitionBase currentBlueprint)
        {
            var buff = GetBuff(__instance.OwnerId, __instance);
            if (!__instance.CubeGrid.IsStatic)
            {
                buff *= Core.Config.DynamicGridsProductionMultiplier;
            }
         
            if (Core.IsPlayerClustered(__instance.OwnerId, __instance.CubeGrid) && Core.Config.NerfClusteredGrids)
            {
                buff *= Core.Config.ClusterNerfDefaultLoses75Percent;
            }
            var speed = (double)(((MyAssemblerDefinition)__instance.BlockDefinition).AssemblySpeed + (double)__instance.UpgradeValues["Productivity"]) * buff;
            return (float)Math.Round((double)currentBlueprint.BaseProductionTimeInSeconds * 1000.0 / ((double)MySession.Static.AssemblerSpeedMultiplier * speed));
        }

        [HarmonyPatch(typeof(MyAssembler))]
        [HarmonyPatch("UpdateProduction")]
        public static class HarmonyTranspilePatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var replaceMethod = typeof(AssemblerPatch).GetMethod(nameof(PatchMethod));
                var codes = new List<CodeInstruction>(instructions);
                return codes.MethodReplacer(update, replaceMethod);
            }
        }

    }
}
