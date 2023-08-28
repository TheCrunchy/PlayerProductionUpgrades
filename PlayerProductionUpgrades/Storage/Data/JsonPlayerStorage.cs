using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using PlayerProductionUpgrades.Helpers;
using PlayerProductionUpgrades.Interfaces;
using PlayerProductionUpgrades.Models;

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
            try
            {
                var path = $"{FolderPath}//{SteamId}.json";
                if (File.Exists(path))
                {
                    var existing = _utils.ReadFromJsonFile<PlayerData>(path);
                    PlayerData.Remove(SteamId);
                    PlayerData.Add(SteamId, existing);
                    return existing;
                }
                var newData = new PlayerData();
                newData.SetupNew();
                newData.SteamId = SteamId;
                SavePlayerData(newData);
                return newData;
            }
            catch (Exception)
            {
            }

            return null;
        }

        public void SavePlayerData(PlayerData data)
        {
            var path = $"{FolderPath}//{data.SteamId}.json";
            _utils.WriteToJsonFile(path, data);
            PlayerData.Remove(data.SteamId);
            PlayerData.Add(data.SteamId, data);
        }
    }
}
