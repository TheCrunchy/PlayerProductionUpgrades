using System.Collections.Generic;
using PlayerProductionUpgrades.Upgrades;

namespace PlayerProductionUpgrades.Interfaces
{
    public interface IConfigProvider
    {
        Dictionary<UpgradeType, Dictionary<int, Upgrade>> Upgrades { get; set; }
        void LoadUpgrades();
        void GenerateExamples();
        Upgrade GetUpgrade(int Level, UpgradeType type);
    }
}