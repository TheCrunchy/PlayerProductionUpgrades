using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerProductionUpgrades.Upgrades;

namespace PlayerProductionUpgrades.Models
{
    public class PlayerData
    {
        public ulong SteamId { get; set; }
        public Dictionary<UpgradeType, int> UpgradeLevels { get; set; } = new Dictionary<UpgradeType, int>();

        public void SetupNew()
        {
            foreach (var type in Enum.GetNames(typeof(UpgradeType)))
            {
                Enum.TryParse(type, out UpgradeType newType);
                UpgradeLevels.Add(newType, 0);
            }
        }

        public int GetUpgradeLevel(UpgradeType Type)
        {
            return UpgradeLevels.TryGetValue(Type, out var level) ? level : 0;
        }

        public void SetUpgradeLevel(UpgradeType Type, int NewLevel)
        {
            if (UpgradeLevels.ContainsKey(Type))
            { 
                UpgradeLevels[Type] = NewLevel;
            }
            else
            {
                UpgradeLevels.Add(Type, NewLevel);
            }
        }
        public void AddUpgradeLevel(UpgradeType Type)
        {
            if (UpgradeLevels.TryGetValue(Type, out var level))
            {
                UpgradeLevels[Type] = level + 1;
            }
            else
            {
                UpgradeLevels.Add(Type, 1);
            }
        }
    }
}
