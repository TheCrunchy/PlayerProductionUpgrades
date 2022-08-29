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
        public DateTime LastLogout { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime BuffedUntil { get; set; }
        public DateTime PricePerHourEndTimeAssembler { get; set; }
        public DateTime PricePerHourEndTimeRefinery { get; set; }
        public int BuffedHoursMultiplier { get; set; }

        public void SetLastLogin()
        {
            LastLogin = DateTime.Now;
        }
        public void SetLastLogout()
        {
            LastLogout = DateTime.Now;
        }

        public void SetBuffedHours()
        {
            if (DateTime.Now < BuffedUntil) return;
            var minimum = (int)(LastLogin - LastLogout).TotalHours;
            if (minimum <= Core.Config.MinimumHoursToBuff) return;
            if (DateTime.Now < LastLogout) return;
            BuffedUntil = DateTime.Now.AddHours(Core.Config.HoursBuffLasts);
            BuffedHoursMultiplier = (int)(LastLogin - LastLogout).TotalHours;
            if (BuffedHoursMultiplier > Core.Config.MaximumHoursToBuff)
            {
                BuffedHoursMultiplier = Core.Config.MaximumHoursToBuff;
            }
        }

        public float GetOfflineBuff()
        {
            if (!Core.Config.GiveBuffForOfflineHours)
            {
                return 1;
            }
            return GetBuff();
        }
        private float GetBuff()
        {
            if (DateTime.Now >= BuffedUntil) return 1;
            return 1 + (BuffedHoursMultiplier * Core.Config.BuffPerHour);
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
