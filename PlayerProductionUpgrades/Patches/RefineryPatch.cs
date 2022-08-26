using System;
using System.Reflection;
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

        public static double GetBuff(long PlayerId)
        {
            return 1;
        }
        public static double GetSpeedBuff(long PlayerId)
        {
            return 1.5;
        }

        public static bool TEST = false;

        public static Boolean ChangeRequirementsToResults(
     MyBlueprintDefinitionBase queueItem,
     MyFixedPoint blueprintAmount, MyRefinery __instance)
        {


            MyRefinery refin = __instance;

            if (refin.BlockDefinition as MyRefineryDefinition == null)
            {
                return false;
            }

            var speedBuff = GetSpeedBuff(refin.OwnerId);
            if (TEST)
            {
                blueprintAmount *= (MyFixedPoint)speedBuff;
            }

            if (!Sync.IsServer || MySession.Static == null || (queueItem == null || queueItem.Prerequisites == null) || (refin.OutputInventory == null || refin.InputInventory == null || (queueItem.Results == null)))
                return false;
            if (!MySession.Static.CreativeMode)
                blueprintAmount = MyFixedPoint.Min(refin.OutputInventory.ComputeAmountThatFits(queueItem), blueprintAmount);
            if (blueprintAmount == (MyFixedPoint)0)
                return false;

            double buff = GetBuff(refin.OwnerId);

            foreach (var prerequisite in queueItem.Prerequisites)
            {
                if ((!(MyObjectBuilderSerializer.CreateNewObject((SerializableDefinitionId)prerequisite.Id) is
                        MyObjectBuilder_PhysicalObject newObject))) continue;

                refin.InputInventory.RemoveItemsOfType((MyFixedPoint)((float)blueprintAmount * (float)prerequisite.Amount), newObject, false, false);
                var itemAmount = refin.InputInventory.GetItemAmount(prerequisite.Id, MyItemFlags.None, false);
                if (itemAmount < (MyFixedPoint)0.01f)
                    refin.InputInventory.RemoveItemsOfType(itemAmount, prerequisite.Id, MyItemFlags.None, false);
            }
            foreach (var result in queueItem.Results)
            {
                if ((!(MyObjectBuilderSerializer.CreateNewObject((SerializableDefinitionId)result.Id) is
                        MyObjectBuilder_PhysicalObject newObject))) continue;

                var def = refin.BlockDefinition as MyRefineryDefinition;
                var num = (float)result.Amount * def.MaterialEfficiency * refin.UpgradeValues["Effectiveness"];
                refin.OutputInventory.AddItems((MyFixedPoint)((float)blueprintAmount * num * buff), (MyObjectBuilder_Base)newObject);
            }

            //  ref.RemoveFirstQueueItemAnnounce(blueprintAmount, 0.0f);
            if (RemoveQueue == null)
            {
                Type change = refin.GetType().Assembly.GetType("Sandbox.Game.Entities.Cube.MyProductionBlock");
                RemoveQueue = change.GetMethod("RemoveFirstQueueItemAnnounce", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            var MethodInput = new object[] { blueprintAmount, 0.0f };
            RemoveQueue?.Invoke(refin, MethodInput);

            return false;
        }
    }
}

