using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Torch.Managers.PatchManager;

namespace PlayerProductionUpgrades.Patches
{
    [PatchShim]
    public class AssemblerPatch
    {
        internal static readonly MethodInfo update =
            typeof(MyAssembler).GetMethod("CalculateBlueprintProductionTime", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo patch =
            typeof(AssemblerPatch).GetMethod(nameof(PatchMethod), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(update).Suffixes.Add(patch);
        }

        private static float GetBuff(long PlayerId, MyAssembler Assembler)
        {
            double buff = 1;
            if (!Core.Config.EnableAlliancePluginBuffs) return (float) buff;
            var methodInput = new object[]{ PlayerId, Assembler };
            var multiplier = (double) Core.GetAllianceAssemblerModifier.Invoke(Core.Alliances, methodInput);
            return (float)(buff *= multiplier);
        }

        public static void PatchMethod(MyBlueprintDefinitionBase currentBlueprint, ref float __result, MyAssembler __instance)
        {
            var buff = GetBuff(__instance.OwnerId, __instance);
            __result *= buff;
        }
    }
}
