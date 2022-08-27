using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRageMath;

namespace PlayerProductionUpgrades.Helpers
{

    public class GridFinder
    {

        public static ConcurrentBag<List<MyCubeGrid>> FindGridList(long playerId, bool includeConnectedGrids)
        {

            var grids = new ConcurrentBag<List<MyCubeGrid>>();

            if (includeConnectedGrids)
            {
                Parallel.ForEach(MyCubeGridGroups.Static.Physical.Groups, group => {

                    var gridList = group.Nodes.Select(groupNodes => groupNodes.NodeData).Where(grid => grid.Physics != null).ToList();

                    if (IsPlayerIdCorrect(playerId, gridList))
                        grids.Add(gridList);
                });
            }
            else
            {
                Parallel.ForEach(MyCubeGridGroups.Static.Mechanical.Groups, group => {

                    var gridList = group.Nodes.Select(groupNodes => groupNodes.NodeData).Where(grid => grid.Physics != null).ToList();

                    if (IsPlayerIdCorrect(playerId, gridList))
                        grids.Add(gridList);
                });
            }

            return grids;
        }

        private static bool IsPlayerIdCorrect(long playerId, List<MyCubeGrid> gridList)
        {

            MyCubeGrid biggestGrid = null;

            foreach (var grid in gridList.Where(grid => biggestGrid == null || biggestGrid.BlocksCount < grid.BlocksCount))
                biggestGrid = grid;

            /* No biggest grid should not be possible, unless the gridgroup only had projections -.- just skip it. */
            if (biggestGrid == null)
                return false;

            var hasOwners = biggestGrid.BigOwners.Count != 0;

            if (hasOwners) return playerId == biggestGrid.BigOwners[0];
            return playerId == 0L;
        }

        public static List<MyCubeGrid> FindGridList(string gridNameOrEntityId, MyCharacter character, bool includeConnectedGrids)
        {

            var grids = new List<MyCubeGrid>();

            if (gridNameOrEntityId == null && character == null)
                return new List<MyCubeGrid>();

            if (includeConnectedGrids)
            {

                ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups;

                groups = gridNameOrEntityId == null ? FindLookAtGridGroup(character) : FindGridGroup(gridNameOrEntityId);

                if (groups.Count > 1)
                    return null;

                grids.AddRange(groups.SelectMany(group => group.Nodes, (group, node) => node.NodeData).Where(grid => grid.Physics != null));
            }
            else
            {

                ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> groups;

                groups = gridNameOrEntityId == null ? FindLookAtGridGroupMechanical(character) : FindGridGroupMechanical(gridNameOrEntityId);

                if (groups.Count > 1)
                    return null;

                grids.AddRange(groups.SelectMany(group => group.Nodes, (group, node) => node.NodeData).Where(grid => grid.Physics != null));
            }

            return grids;
        }

        public static ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> FindGridGroup(string gridName)
        {

            var groups = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group>();
            Parallel.ForEach(MyCubeGridGroups.Static.Physical.Groups, group =>
            {
                foreach (var grid in from groupNodes in @group.Nodes select groupNodes.NodeData into grid where grid.Physics != null where grid.DisplayName.Equals(gridName) || grid.EntityId + "" == gridName select grid)
                {
                    groups.Add(group);
                }
            });

            return groups;
        }

        public static ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> FindLookAtGridGroup(IMyCharacter controlledEntity)
        {

            const float range = 5000;
            Matrix worldMatrix;
            Vector3D startPosition;
            Vector3D endPosition;

            worldMatrix = controlledEntity.GetHeadMatrix(true, true, false); // dead center of player cross hairs, or the direction the player is looking with ALT.
            startPosition = worldMatrix.Translation + worldMatrix.Forward * 0.5f;
            endPosition = worldMatrix.Translation + worldMatrix.Forward * (range + 0.5f);

            var list = new Dictionary<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group, double>();
            var ray = new RayD(startPosition, worldMatrix.Forward);

            foreach (var group in MyCubeGridGroups.Static.Physical.Groups)
            {
                foreach (var distance in from groupNodes in @group.Nodes select groupNodes.NodeData into cubeGrid where cubeGrid != null where ((IMyCubeGrid)cubeGrid).Physics != null where ray.Intersects(((IMyCubeGrid)cubeGrid).WorldAABB).HasValue let hit = ((IMyCubeGrid)cubeGrid).RayCastBlocks(startPosition, endPosition) where hit.HasValue select (startPosition - ((IMyCubeGrid)cubeGrid).GridIntegerToWorld(hit.Value)).Length())
                {
                    if (list.TryGetValue(group, out var oldDistance))
                    {
                        if (!(distance < oldDistance)) continue;
                        list.Remove(group);
                        list.Add(group, distance);

                    }
                    else
                    {

                        list.Add(group, distance);
                    }
                }
            }

            var bag = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group>();

            if (list.Count == 0)
                return bag;

            // find the closest Entity.
            var item = list.OrderBy(f => f.Value).First();
            bag.Add(item.Key);

            return bag;
        }

        public static ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> FindGridGroupMechanical(string gridName)
        {

            var groups = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group>();
            Parallel.ForEach(MyCubeGridGroups.Static.Mechanical.Groups, group =>
            {
                foreach (var grid in from groupNodes in @group.Nodes select groupNodes.NodeData into grid where grid.Physics != null where grid.DisplayName.Equals(gridName) || grid.EntityId + "" == gridName select grid)
                {
                    groups.Add(group);
                }
            });

            return groups;
        }

        public static ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> FindLookAtGridGroupMechanical(IMyCharacter controlledEntity)
        {

            const float range = 5000;
            Matrix worldMatrix;
            Vector3D startPosition;
            Vector3D endPosition;

            worldMatrix = controlledEntity.GetHeadMatrix(true, true, false); // dead center of player cross hairs, or the direction the player is looking with ALT.
            startPosition = worldMatrix.Translation + worldMatrix.Forward * 0.5f;
            endPosition = worldMatrix.Translation + worldMatrix.Forward * (range + 0.5f);

            var list = new Dictionary<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group, double>();
            var ray = new RayD(startPosition, worldMatrix.Forward);

            foreach (var group in MyCubeGridGroups.Static.Mechanical.Groups)
            {
                foreach (var distance in from groupNodes in @group.Nodes select groupNodes.NodeData into cubeGrid where cubeGrid != null where ((IMyCubeGrid)cubeGrid).Physics != null where ray.Intersects(((IMyCubeGrid)cubeGrid).WorldAABB).HasValue let hit = ((IMyCubeGrid)cubeGrid).RayCastBlocks(startPosition, endPosition) where hit.HasValue select (startPosition - ((IMyCubeGrid)cubeGrid).GridIntegerToWorld(hit.Value)).Length())
                {
                    if (list.TryGetValue(group, out var oldDistance))
                    {
                        if (!(distance < oldDistance)) continue;
                        list.Remove(group);
                        list.Add(group, distance);

                    }
                    else
                    {

                        list.Add(group, distance);
                    }
                }
            }

            var bag = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group>();

            if (list.Count == 0)
                return bag;

            // find the closest Entity.
            var item = list.OrderBy(f => f.Value).First();
            bag.Add(item.Key);

            return bag;
        }
    }
}
