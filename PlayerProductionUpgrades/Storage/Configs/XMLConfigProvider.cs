using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PlayerProductionUpgrades.Helpers;
using PlayerProductionUpgrades.Interfaces;
using PlayerProductionUpgrades.Models;
using PlayerProductionUpgrades.Models.Upgrades;

namespace PlayerProductionUpgrades.Storage.Configs
{
    public class XMLConfigProvider : IConfigProvider
    {
        //i surprised myself with how clean this looks, compared to the other way to do it with multiple dictionaries 
        public Dictionary<UpgradeType, Dictionary<int, Upgrade>> Upgrades { get; set; } = new Dictionary<UpgradeType, Dictionary<int, Upgrade>>();
        private string FolderPath;
        public XMLConfigProvider(string FolderPath)
        {
            this.FolderPath = $"{FolderPath}//UpgradeConfigs//";

        }

        public FileUtils Utils = new FileUtils();

        public void LoadUpgrades()
        {
            Directory.CreateDirectory($"{FolderPath}");
            GenerateExamples();
            Upgrades.Clear();
            foreach (var filePath in Directory.GetFiles($"{FolderPath}", "*", SearchOption.AllDirectories))
            {
                LoadFile(filePath);
            }
        }
        public bool CanUpgrade(int CurrentLevel, UpgradeType type)
        {
            return Upgrades.TryGetValue(type, out var levels) && levels.ContainsKey(CurrentLevel + 1);
        }
        public void GenerateExamples()
        {
            foreach (var type in Enum.GetNames(typeof(UpgradeType)))
            {
                var path = $"{FolderPath}{type}";
                Directory.CreateDirectory(path);
                if (File.Exists($"{path}//Example.xml")) continue;
                Enum.TryParse(type, out UpgradeType newType);
                var upgrade = new Upgrade
                {
                    Type = newType
                };
                var list = new BuffList();
                if (type.Contains("Refinery"))
                {
                    list.buffs.Add(new BuffedBlock()
                {
                    Enabled = true,
                    SubtypeId = "LargeRefineryIndustrial"
                });
                list.buffs.Add(new BuffedBlock()
                {
                    Enabled = true,
                    SubtypeId = "LargeRefinery"
                });
                }
                if (type.Contains("Assembler"))
                {
                    list.buffs.Add(new BuffedBlock()
                    {
                        Enabled = true,
                        SubtypeId = "LargeAssemblerIndustrial"
                    });
                    list.buffs.Add(new BuffedBlock()
                    {
                        Enabled = true,
                        SubtypeId = "LargeAssembler"
                    });
                }

                upgrade.BuffedBlocks.Add(list);
                var req = new ItemRequirement();
                var req2 = new ItemRequirement
                {
                    Enabled = false
                };
                upgrade.items.Add(req);
                upgrade.items.Add(req2);
                Utils.WriteToXmlFile($"{path}//Example.xml", upgrade);
            }
        }

        public void LoadFile(string FilePath)
        {
            try
            {
                var upgrade = Utils.ReadFromXmlFile<Upgrade>(FilePath);
                if (!upgrade.Enabled)
                {
                    return;
                }
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
            try
            {
                if (!Upgrades.TryGetValue(type, out var temp))
                {
                    var tempUp = Upgrades.FirstOrDefault(x => x.Key == type).Value ?? null;
                    if (tempUp == null)
                    {
                        return new Upgrade();
                    }

                    return tempUp.OrderBy(x => x.Value).First().Value;
                }
                return temp.TryGetValue(Level, out var upgrade) ? upgrade : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
