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
using PlayerProductionUpgrades.Storage;
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
        public static Config config;
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


        private void SetupConfig()
        {
            FileUtils utils = new FileUtils();

            if (File.Exists(StoragePath + "\\PlayerUpgradesConfig.xml"))
            {
                config = utils.ReadFromXmlFile<Config>(StoragePath + "\\PlayerUpgradesConfig.xml");
                utils.WriteToXmlFile<Config>(StoragePath + "\\PlayerUpgradesConfig.xml", config, false);
            }
            else
            {
                config = new Config();
                utils.WriteToXmlFile<Config>(StoragePath + "\\PlayerUpgradesConfig.xml", config, false);
            }
            if (config.StoragePath.Equals("default"))
            {
                PlayerStoragePath = Path.Combine();
                Directory.CreateDirectory(StoragePath + "//PlayerUpgrades");
            }
            else
            {
                PlayerStoragePath = config.StoragePath;
            }

            ConfigProvider = new JsonConfigProvider(PlayerStoragePath);
        }

        private void SessionChanged(ITorchSession session, TorchSessionState newState)
        {
            if (newState is TorchSessionState.Loaded)
            {
                ConfigProvider.LoadUpgrades();
            }
        }



    }
}

