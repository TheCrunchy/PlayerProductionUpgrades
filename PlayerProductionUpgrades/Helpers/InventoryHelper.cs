using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRageMath;

namespace PlayerProductionUpgrades.Helpers
{
    public static class InventoryHelper
    {
        public static List<VRage.Game.ModAPI.IMyInventory> GetInventories(MyCubeGrid grid)
        {
            var inventories = new List<VRage.Game.ModAPI.IMyInventory>();

            foreach (var block in grid.GetFatBlocks())
            {
                //block.SlimBlock.GetMissingComponents()
                //block.SlimBlock.ComponentStack
                for (var i = 0; i < block.InventoryCount; i++)
                {
                    var inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);
                    inventories.Add(inv);
                }

            }
            return inventories;
        }


        public static bool ConsumeComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, IDictionary<MyDefinitionId, int> components, ulong steamid)
        {
            var toRemove = new List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>>();
            foreach (var c in components)
            {
                var needed = CountComponents(inventories, c.Key, c.Value, toRemove);
                if (needed <= 0) continue;
                Core.SendMessage("[Shipyard]", "Missing " + needed + " " + c.Key.SubtypeName + " All components must be inside one grid.", Color.Red, (long)steamid);

                return false;
            }

            foreach (var item in toRemove)
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    item.Item1.RemoveItemAmount(item.Item2, item.Item3);
                });
            return true;
        }

        public static MyFixedPoint CountComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, MyDefinitionId id, int amount, ICollection<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>> items)
        {
            MyFixedPoint targetAmount = amount;
            foreach (var inv in inventories)
            {
                var invItem = inv.FindItem(id);
                if (invItem == null) continue;
                if (invItem.Amount >= targetAmount)
                {
                    items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, targetAmount));
                    targetAmount = 0;
                    break;
                }
                else
                {
                    items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, invItem.Amount));
                    targetAmount -= invItem.Amount;
                }
            }
            return targetAmount;
        }


        public static void GetComponents(MyCubeBlockDefinition def, IDictionary<MyDefinitionId, int> components)
        {
            if (def?.Components == null) return;
            foreach (var c in def.Components)
            {
                var id = c.Definition.Id;
                int num;
                if (components.TryGetValue(id, out num))
                    components[id] = num + c.Count;
                else
                    components.Add(id, c.Count);
            }
        }


        public static Dictionary<MyDefinitionId, int> GetComponents(VRage.Game.ModAPI.IMyCubeGrid projection)
        {
            var comps = new Dictionary<MyDefinitionId, int>();
            var temp = new List<VRage.Game.ModAPI.IMySlimBlock>(0);
            projection.GetBlocks(temp, (slim) =>
            {
                GetComponents((MyCubeBlockDefinition)slim.BlockDefinition, comps);
                return false;
            });
            return comps;
        }

    }
}
