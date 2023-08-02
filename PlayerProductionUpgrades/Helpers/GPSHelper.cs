using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Screens.Helpers;
using VRageMath;

namespace PlayerProductionUpgrades.Helpers
{
    public static class GPSHelper
    {
        public static MyGps CreateGps(Vector3D Position, Color gpsColor, String Name, String Reason)
        {

            MyGps gps = new MyGps
            {
                Coords = Position,
                Name = Name,
                DisplayName = Name,
                GPSColor = gpsColor,
                IsContainerGPS = true,
                ShowOnHud = true,
                DiscardAt = new TimeSpan(0, 0, 5, 0),
                Description = Reason,
            };
            gps.UpdateHash();


            return gps;
        }
    }
}
