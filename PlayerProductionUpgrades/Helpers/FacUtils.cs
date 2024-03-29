﻿using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace PlayerProductionUpgrades.Helpers
{
    public class FacUtils
    {
        public static IMyFaction GetPlayersFaction(long playerId)
        {
            return MySession.Static.Factions.TryGetPlayerFaction(playerId);
        }

        public static bool InSameFaction(long player1, long player2)
        {
            var faction1 = GetPlayersFaction(player1);
            var faction2 = GetPlayersFaction(player2);
            return faction1 == faction2;
        }

        public static string GetFactionTag(long playerId)
        {
            var faction = MySession.Static.Factions.TryGetPlayerFaction(playerId);

            return faction == null ? "" : faction.Tag;
        }
        public static long GetOwner(MyCubeGrid grid)
        {

            var gridOwnerList = grid.BigOwners;
            var ownerCnt = gridOwnerList.Count;
            var gridOwner = 0L;

            if (ownerCnt > 0 && gridOwnerList[0] != 0)
                return gridOwnerList[0];
            else if (ownerCnt > 1)
                return gridOwnerList[1];

            return gridOwner;
        }

        public static bool IsOwnerOrFactionOwned(MyCubeGrid grid, long playerId, bool doFactionCheck)
        {
            if (grid.BigOwners.Contains(playerId))
            {
                return true;
            }
            else
            {
                if (!doFactionCheck)
                {
                    return false;
                }
                long ownerId = GetOwner(grid);
                //check if the owner is a faction member, i honestly dont know the difference between grid.BigOwners and grid.SmallOwners
                return FacUtils.InSameFaction(playerId, ownerId);
            }
        }

    }
}
