using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerProductionUpgrades.Models;
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
            GenerateExamples();
            foreach (var filePath in Directory.GetFiles($"{FolderPath}//Upgrades//", "*", SearchOption.AllDirectories))
            {
                LoadFile(filePath);
            }
        }

        public void GenerateExamples()
        {
            foreach (var type in Enum.GetNames(typeof(UpgradeType)))
            {
                var path = $"{FolderPath}//Upgrades//{type}";
                Directory.CreateDirectory(path);
                if (File.Exists($"{path}//Example.json")) continue;
                var upgrade = new Upgrade
                {
                    Type = (UpgradeType)Enum.Parse(typeof(UpgradeType), type)
                };
                var list = new BuffList();
                list.buffs.Add(new BuffedBlock());
                upgrade.BuffedBlocks.Add(list);
                var req = new ItemRequirement();
                upgrade.items.Add(req);
                Utils.WriteToJsonFile($"{path}//Example.json", upgrade);
            }
        }

        public void LoadFile(string FilePath)
        {
            try
            {
                var upgrade = Utils.ReadFromJsonFile<Upgrade>(FilePath);
                upgrade.PutBuffedInDictionary();

                if (Upgrades.TryGetValue(upgrade.Type, out var temp))
                {
                    if (temp.ContainsKey(upgrade.UpgradeId)) return;
                    temp.Add(upgrade.UpgradeId, upgrade);
                }
                else
                {
                    Upgrades.Add(upgrade.Type, new Dictionary<int, Upgrade> { { upgrade.UpgradeId, upgrade } });
                }
            }
            catch (Exception e)
            {
                Core.Log.Error($"Error on file {FilePath} {e}");
            }
        }

        public Upgrade GetUpgrade(int Level, UpgradeType type)
        {
            if (!Upgrades.TryGetValue(type, out var temp)) return null;
            return temp.TryGetValue(Level, out var upgrade) ? upgrade : null;
        }
    }
}
