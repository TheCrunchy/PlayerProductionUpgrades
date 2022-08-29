using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Torch.Managers.PatchManager;
using VRage.Game;

namespace PlayerProductionUpgrades.Patches
{
    [PatchShim]
    public class GasGeneratorPatch
    {
        internal static readonly MethodInfo update =
            typeof(MyGasGenerator).GetMethod("IceToGasRatio", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo patch =
            typeof(GasGeneratorPatch).GetMethod(nameof(ChangeResult), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

   
        public static void Patch(PatchContext ctx)
        {
            //   ctx.GetPattern(update).Suffixes.Add(patch);
        }

        public static void ChangeResult(ref MyDefinitionId gasId, ref double __result)
        {
            __result *= 500000;
        }

    }
}
