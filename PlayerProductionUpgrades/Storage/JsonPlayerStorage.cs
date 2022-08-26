using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerProductionUpgrades.Storage
{
    public class JsonPlayerStorage
    {
        public ulong SteamId { get; set; }
        public int RefineryYieldUpgradeLevel { get; set; } = 0;
        public int AssemblerSpeedUpgradeLevel { get; set; } = 0;

        public JsonPlayerStorage(ulong SteamId)
        {
            this.SteamId = SteamId;
        }

    }



}
