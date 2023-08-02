using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Concurrent;
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
using RestSharp;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Managers;
using Torch.Managers.PatchManager;
using Torch.Session;
using VRageMath;

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

        public static bool InitPlugins = false;
        public DateTime NextVoteCheck = DateTime.Now.AddMinutes(1);
        public int ticks = 0;
        public static ConcurrentDictionary<ulong, Lazy> RecheckTimer = new ConcurrentDictionary<ulong, Lazy>();

        public class Lazy
        {
            public DateTime CheckTime;
        }

        public static Dictionary<long, bool> IsClustered = new Dictionary<long, bool>();
        public static Dictionary<long, DateTime> NextClusterCheck = new Dictionary<long, DateTime>();

        public static bool ClusterCheck(MyCubeGrid grid)
        {
            var sphere = new BoundingSphereD(grid.PositionComp.GetPosition(), Config.ClusterDistanceMetres * 2);
            var gridCount = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            var players = gridCount.OfType<MyCharacter>();

            var isClustering = gridCount.OfType<MyCubeGrid>().Count() > Config.NerfClusteredGridsAboveCount;
            if (Core.Config.SendGPSForClusters && isClustering)
            {
                var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                foreach (var grids in gridCount.OfType<MyCubeGrid>().Where(x => x.BlocksCount > 50))
                {
                    if (grids.Closed)
                    {
                        continue;
                    }
                    var gps = GPSHelper.CreateGps(grids.PositionComp.GetPosition(), Color.Red, $"Clustered Grid", "Grid detected clustering, production is nerfed, move away minimum of {Config.ClusterDistanceMetres}M");
                    foreach (MyCharacter player in players)
                    {
                        var id = player.GetIdentity().IdentityId;
                        gpscol.SendAddGpsRequest(id, ref gps);
                    }
                }
            }
            return isClustering;
        }

        public static bool IsPlayerClustered(long playerIdentityId, MyCubeGrid grid)
        {
            if (NextClusterCheck.TryGetValue(playerIdentityId, out var time))
            {
                if (DateTime.Now >= time)
                {
                    IsClustered[playerIdentityId] = ClusterCheck(grid);
                    NextClusterCheck[playerIdentityId] = DateTime.Now.AddMinutes(5);
                }
            }
            else
            {
                NextClusterCheck.Add(playerIdentityId, DateTime.Now.AddMinutes(5));
                IsClustered.Add(playerIdentityId, ClusterCheck(grid));
            }

            return IsClustered[playerIdentityId];
        }

        public async override void Update()
        {
            if (!InitPlugins && ticks == 0)
            {
                InitPluginDependencies(Torch.Managers.GetManager<PluginManager>());
                InitPlugins = true;
            }

            ticks++;
            if (ticks % 64 != 0) return;

            if (!Core.Config.DoVoteBuffs) return;

            List<ulong> temp = new List<ulong>();
            var ServerKey = Core.Config.VoteApiKey;
            foreach (var player in RecheckTimer.Where(x => x.Value.CheckTime <= DateTime.Now))
            {
                //    Log.Info($"{player.Key} checking");

                try
                {
                    //var client = new RestClient($"https://space-engineers.com/api/?object=servers&element=voters&key={ServerKey}&month={Period}&format={Format}&rank={Value}&limit={Limit}");
                    var client = new RestClient($"https://space-engineers.com/api/?object=votes&element=claim&key={ServerKey}&steamid={player.Key}");
                    var request = new RestRequest();
                    var result = await client.PostAsync(request);
                    if (result.IsSuccessful)
                    {
                        //     Log.Info($"{result.Content}");
                        switch (result.Content.ToString())
                        {
                            case "1":
                            case "2":
                                var playerData = Core.PlayerStorageProvider.GetPlayerData(player.Key);
                                playerData.VoteBuffedUntil = DateTime.Now.AddHours(24);
                                PlayerStorageProvider.SavePlayerData(playerData);
                                //       Log.Info($"{player.Key} has voted and is getting buffed for 24 hours");
                                temp.Add(player.Key);
                                break;
                            default:
                                RecheckTimer[player.Key].CheckTime = DateTime.Now.AddMinutes(10);
                                //      Log.Info($"{player.Key} has not voted recheck 1 min");
                                break;
                        }
                    }
                    else
                    {
                        //    Log.Info($"{player.Key} failed query");
                    }
                }
                catch (Exception)
                {
                }
            }

            foreach (var item in temp)
            {
                RecheckTimer.Remove(item);
            }

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
        public static void SendMessage(string author, string message, Color color, long steamID)
        {
            Logger _chatLog = LogManager.GetLogger("Chat");
            ScriptedChatMsg scriptedChatMsg1 = new ScriptedChatMsg();
            scriptedChatMsg1.Author = author;
            scriptedChatMsg1.Text = message;
            scriptedChatMsg1.Font = "White";
            scriptedChatMsg1.Color = color;
            scriptedChatMsg1.Target = Sync.Players.TryGetIdentityId((ulong)steamID);
            ScriptedChatMsg scriptedChatMsg2 = scriptedChatMsg1;
            MyMultiplayerBase.SendScriptedChatMessage(ref scriptedChatMsg2);
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
                session.Managers.GetManager<IMultiplayerManagerBase>().PlayerJoined += LoginLogoutHelper.Login;
                session.Managers.GetManager<IMultiplayerManagerBase>().PlayerLeft += LoginLogoutHelper.Logout;
            }

        }
    }
}

