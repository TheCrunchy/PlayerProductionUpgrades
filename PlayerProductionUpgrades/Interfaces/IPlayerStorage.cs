using System.Collections.Generic;
using PlayerProductionUpgrades.Models;

namespace PlayerProductionUpgrades.Storage.Data
{
    public interface IPlayerStorage
    {
        Dictionary<ulong, PlayerData> PlayerData { get; }
        PlayerData GetPlayerData(ulong SteamId);
        PlayerData LoadPlayerData(ulong SteamId);
        void SavePlayerData(PlayerData);
    }
}