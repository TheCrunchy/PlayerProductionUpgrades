using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PlayerProductionUpgrades.Helpers;
using PlayerProductionUpgrades.Interfaces;
using PlayerProductionUpgrades.Storage;
using PlayerProductionUpgrades.Storage.Configs;
using PlayerProductionUpgrades.Storage.Data;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Managers;
using Torch.Managers.PatchManager;
using Torch.Session;

namespace PlayerProductionUpgrades
{
    public class Core : TorchPluginBase
    {
        public static string PlayerStoragePath;
        public static Logger Log = LogManager.GetLogger("PlayerProduction");
        public static IConfigProvider ConfigProvider;
        public static IPlayerStorage PlayerStorageProvider;
        public static Config Config;
        public static bool AlliancePluginInstalled = false;
        public static ITorchPlugin Alliances;
        public static MethodInfo GetAllianceAssemblerModifier;
        public static MethodInfo GetAllianceRefineryModifier;
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
            {
                sessionManager.SessionStateChanged += SessionChanged;
            }
            SetupConfig();
        }

        public static void InitPluginDependencies(PluginManager Plugins)
        {
            if (Plugins.Plugins.TryGetValue(Guid.Parse("74796707-646f-4ebd-8700-d077a5f47af3"),
                    out var AlliancePlugin))
            {
                try
                {
                    var AllianceIntegration =
                        AlliancePlugin.GetType().Assembly.GetType("AlliancesPlugin.Integrations.AllianceIntegrationCore");

                    GetAllianceRefineryModifier = AllianceIntegration.GetMethod("GetRefineryYieldMultiplier", BindingFlags.Public | BindingFlags.Static);
                    GetAllianceAssemblerModifier = AllianceIntegration.GetMethod("GetAssemblerSpeedMultiplier", BindingFlags.Public | BindingFlags.Static);
                    Alliances = AlliancePlugin;
                    AlliancePluginInstalled = true;
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading the alliance integration for upgrades");
                }
            }
            else
            {
                Log.Info("Alliances not installed");
            }
        }

        private void SetupConfig()
        {
            var utils = new FileUtils();
            var path = StoragePath + "\\PlayerUpgradesConfig.xml";
            if (File.Exists(path))
            {
                Config = utils.ReadFromXmlFile<Config>(path);
                utils.WriteToXmlFile<Config>(path, Config, false);
            }
            else
            {
                Config = new Config();
                utils.WriteToXmlFile<Config>(path, Config, false);
            }
            if (Config.StoragePath.Equals("Default"))
            {
                PlayerStoragePath = Path.Combine($"{StoragePath}//PlayerUpgrades");
                Directory.CreateDirectory(StoragePath + "//PlayerUpgrades");
            }
            else
            {
                PlayerStoragePath = Config.StoragePath;
            }
        }

        private void SessionChanged(ITorchSession session, TorchSessionState newState)
        {
            if (newState is TorchSessionState.Loaded)
            {
                ConfigProvider = new XMLConfigProvider(PlayerStoragePath);
                PlayerStorageProvider = new JsonPlayerStorage(PlayerStoragePath);
                ConfigProvider.LoadUpgrades();

                InitPluginDependencies(Torch.Managers.GetManager<PluginManager>());
            }

        }
    }
}

