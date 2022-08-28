using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using Sandbox.Game.World;
using Torch.API;

namespace PlayerProductionUpgrades.Helpers
{
    public class LoginLogoutHelper
    {
        public static void Login(IPlayer p)
        {
            if (p == null)
            {
                return;
            }
            var data = Core.PlayerStorageProvider.LoadPlayerData(p.SteamId);
            data.SetLastLogin();
            data.SetBuffedHours();
            Core.PlayerStorageProvider.SavePlayerData(data);
        }

        public static void Logout(IPlayer p)
        {
            if (p == null)
            {
                return;
            }
            var data = Core.PlayerStorageProvider.LoadPlayerData(p.SteamId);
            data.SetLastLogout();
            Core.PlayerStorageProvider.SavePlayerData(data);
        }
    }
}
