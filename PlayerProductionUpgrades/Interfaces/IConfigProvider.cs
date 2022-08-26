using System.Collections.Generic;
using PlayerProductionUpgrades.Upgrades;

namespace PlayerProductionUpgrades.Storage
{
    public interface IConfigProvider
    {
        Dictionary<UpgradeType, Dictionary<int, Upgrade>> Upgrades { get; set; }
        void LoadUpgrades();
        Upgrade GetUpgrade(int Level, UpgradeType type);
    }
}