using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerProductionUpgrades
{
    public class Config
    {
        public string StoragePath = "Default";
        public bool EnableBuyingUpgrades = false;
        public bool EnableAlliancePluginBuffs = false;
        public bool GiveBuffForOfflineHours = false;
        public float BuffPerHour = 0.5f;
        public int MaximumHoursToBuff = 8;
        public int MinimumHoursToBuff = 2;
        public int HoursBuffLasts = 1;
        public bool MakePlayersPayPerHour = false;
    }
}
