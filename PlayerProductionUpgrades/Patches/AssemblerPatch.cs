using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
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
            //figure out how to do this as a transpiler patch 
            // ctx.GetPattern(update).Suffixes.Add(patch);
            var harmony = new Harmony("Crunch.Assembler.Patch");
            harmony.PatchAll();
        }

        private static float GetBuff(long PlayerId, MyAssembler Assembler)
        {
            return 5000;
            double buff = 1;
            if (!Core.Config.EnableAlliancePluginBuffs) return (float)buff;
            var methodInput = new object[] { PlayerId, Assembler };
            var multiplier = (double)Core.GetAllianceAssemblerModifier.Invoke(Core.Alliances, methodInput);
            return (float)(buff *= multiplier);
        }

        public static float PatchMethod(MyAssembler __instance, MyBlueprintDefinitionBase currentBlueprint)
        {
            var speed = (double)(((MyAssemblerDefinition)__instance.BlockDefinition).AssemblySpeed + (double)__instance.UpgradeValues["Productivity"]) * GetBuff(__instance.OwnerId, __instance);
            return (float)Math.Round((double)currentBlueprint.BaseProductionTimeInSeconds * 1000.0 / ((double)MySession.Static.AssemblerSpeedMultiplier * speed));
        }

        [HarmonyPatch(typeof(MyAssembler))]
        [HarmonyPatch("UpdateProduction")]
        public static class HarmonyTranspilePatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Core.Log.Info("HELP?");
                  var replaceMethod= typeof(AssemblerPatch).GetMethod(nameof(PatchMethod));
            //  var replaceMethodConstructor =
            //      typeof(AssemblerPatch).GetConstructor(new[] { typeof(MyBlueprintDefinition), typeof(MyAssembler) });
                var codes = new List<CodeInstruction>(instructions);
                return codes.MethodReplacer(update, replaceMethod);
            }
        }

    }
}
