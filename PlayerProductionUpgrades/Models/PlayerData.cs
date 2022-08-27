using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerProductionUpgrades.Models.Upgrades;

namespace PlayerProductionUpgrades.Models
{
    public class PlayerData
    {
        public ulong SteamId { get; set; }
        public Dictionary<UpgradeType, int> UpgradeLevels { get; set; } = new Dictionary<UpgradeType, int>();
        public DateTime LastLogout { get; set; } = DateTime.Now;
        public DateTime LastLogin { get; set; } = DateTime.Now;
        public DateTime BuffedUntil { get; set; } = DateTime.Now;

        public void SetLastLogin()
        {
            LastLogin = DateTime.Now;
        }
        public void SetLastLogout()
        {
            LastLogout = DateTime.Now;
        }
        public float GetOfflineBuff()
        {
            if (!Core.Config.GiveBuffForOfflineHours)
            {
                return 0;
            }
            float buff = 0;
            var minimum = (int)(LastLogin - LastLogout).TotalHours;
            if (minimum <= Core.Config.MinimumHoursToBuff) return GetBuff();
            if (DateTime.Now < LastLogout) return GetBuff();
            var hours = 0;
            hours = (int)(DateTime.Now - LastLogout).TotalHours;
            if (hours > Core.Config.MaximumHoursToBuff)
            {
                hours = Core.Config.MaximumHoursToBuff;
            }
            BuffedUntil = DateTime.Now.AddHours(hours);
            return GetBuff();
        }
        private float GetBuff()
        {
            if (DateTime.Now >= BuffedUntil) return 0;
            var hours = (int)(BuffedUntil - DateTime.Now).TotalHours;
            if (hours > Core.Config.MaximumHoursToBuff)
            {
                hours = Core.Config.MaximumHoursToBuff;
            }
            var buff = 1 + (hours * Core.Config.BuffPerHour);
            return buff;
        }

        public void SetBuffedUntil(int hours)
        {
            BuffedUntil = DateTime.Now.AddHours(hours);
        }
        public DateTime GetBuffedUntil()
        {
            return BuffedUntil;
        }
        public void SetupNew()
        {
            foreach (var type in Enum.GetNames(typeof(UpgradeType)))
            {
                Enum.TryParse(type, out UpgradeType newType);
                UpgradeLevels.Add(newType, 0);
            }
        }

        public int GetUpgradeLevel(UpgradeType Type)
        {
            return UpgradeLevels.TryGetValue(Type, out var level) ? level : 0;
        }

        public void SetUpgradeLevel(UpgradeType Type, int NewLevel)
        {
            if (UpgradeLevels.ContainsKey(Type))
            {
                UpgradeLevels[Type] = NewLevel;
            }
            else
            {
                UpgradeLevels.Add(Type, NewLevel);
            }
        }
        public void AddUpgradeLevel(UpgradeType Type)
        {
            if (UpgradeLevels.TryGetValue(Type, out var level))
            {
                UpgradeLevels[Type] = level + 1;
            }
            else
            {
                UpgradeLevels.Add(Type, 1);
            }
        }
    }
}
