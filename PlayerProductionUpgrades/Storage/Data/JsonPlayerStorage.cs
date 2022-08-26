using System.Collections.Generic;
using System.IO;
using PlayerProductionUpgrades.Models;
using PlayerProductionUpgrades.Upgrades;

namespace PlayerProductionUpgrades.Storage.Data
{
    public class JsonPlayerStorage : IPlayerStorage
    {
        public Dictionary<ulong, PlayerData> PlayerData { get; } = new Dictionary<ulong, PlayerData>();
        private string FolderPath;
        private readonly FileUtils _utils = new FileUtils();
        public JsonPlayerStorage(string FolderPath)
        {
            this.FolderPath = $"{FolderPath}//PlayerData//";
            Directory.CreateDirectory(this.FolderPath);
        }
        public PlayerData GetPlayerData(ulong SteamId)
        {
            return PlayerData.TryGetValue(SteamId, out var data) ? data : LoadPlayerData(SteamId);
        }

        public PlayerData LoadPlayerData(ulong SteamId)
        {
            var path = $"{FolderPath}//{SteamId}.json";
            if (File.Exists(path))
            {
                var existing = _utils.ReadFromJsonFile<PlayerData>(path);
                PlayerData.Add(SteamId, existing);
                return existing;
            }
            var newData = new PlayerData();
            newData.SetupNew();
            newData.SteamId = SteamId;
            PlayerData.Add(SteamId, newData);
            _utils.WriteToJsonFile(path, newData);
            return newData;
        }

        public void SavePlayerData(PlayerData data)
        {
            var path = $"{FolderPath}//{data.SteamId}.json";
            _utils.WriteToJsonFile(path, data);
        }
    }
}
