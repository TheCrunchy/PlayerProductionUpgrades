using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerProductionUpgrades
{
    public class Config
    {
        public string StoragePath = "Default";
        public bool EnableBuyingUpgrades = false;
        public bool GiveBuffForOfflineHours = false;
        public float BuffPerHour = 0.5f;
        public int MaximumHoursToBuff = 8;
        public int MinimumHoursToBuff = 2;
        public int HoursBuffLasts = 1;
        public bool MakePlayersPayPerHour = false;
        public bool DoVoteBuffs = false;
        public float VoteBuff = 0.50f;
        public string VoteApiKey = "dont share this";

        public float ClusterNerfDefaultLoses75Percent = 0.25f;
        public int NerfClusteredGridsAboveCount = 3;
        public bool NerfClusteredGrids = false;
        public int ClusterDistanceMetres = 15000;
        public Boolean SendGPSForClusters = true;
        public float DynamicGridsProductionMultiplier = 1;
    }
}
