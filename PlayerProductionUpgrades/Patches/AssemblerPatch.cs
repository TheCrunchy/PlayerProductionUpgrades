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

        private static float GetBuff(long PlayerId)
        {
            return 1;
        }

        public static void PatchMethod(MyBlueprintDefinitionBase currentBlueprint, ref float __result, MyAssembler __instance)
        {
            var buff = GetBuff(__instance.OwnerId);
            __result *= buff;
        }
    }
}
