using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerProductionUpgrades.Upgrades;

namespace PlayerProductionUpgrades.Storage
{
    public class JsonConfigProvider : IConfigProvider
    {
        //i surprised myself with how clean this looks, compared to the other way to do it with multiple dictionaries 
        public Dictionary<UpgradeType, Dictionary<int, Upgrade>> Upgrades { get; set; } = new Dictionary<UpgradeType, Dictionary<int, Upgrade>>();
        private string FolderPath;
        public JsonConfigProvider(string FolderPath)
        {
            this.FolderPath = FolderPath;
        }

        public FileUtils Utils = new FileUtils();

        public void LoadUpgrades()
        {
            foreach (var s in Directory.GetFiles($"{FolderPath}//Upgrades//", "*", SearchOption.AllDirectories))
            {
                try
                {
                    var upgrade = Utils.ReadFromJsonFile<Upgrade>(s);
                    upgrade.PutBuffedInDictionary();

                    if (Upgrades.TryGetValue(upgrade.Type, out var temp))
                    {
                        if (temp.ContainsKey(upgrade.UpgradeId)) continue;
                        temp.Add(upgrade.UpgradeId, upgrade);
                    }
                    else
                    {
                        Upgrades.Add(upgrade.Type, new Dictionary<int, Upgrade> { { upgrade.UpgradeId, upgrade } });
                    }
                }
                catch (Exception e)
                {
                    Core.Log.Error($"Error on file {s} {e}");
                }
            }
        }

        public Upgrade GetUpgrade(int Level, UpgradeType type)
        {
            if (!Upgrades.TryGetValue(type, out var temp)) return null;
            return temp.TryGetValue(Level, out var upgrade) ? upgrade : null;
        }
    }
}
