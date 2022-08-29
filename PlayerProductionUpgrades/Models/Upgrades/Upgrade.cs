using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;

namespace PlayerProductionUpgrades.Models.Upgrades
{
    public class Upgrade
    {
        public UpgradeType Type;
        public int UpgradeId = 1;
        public Boolean Enabled = false;
        public long MoneyRequired = 5000000;
        public List<ItemRequirement> items = new List<ItemRequirement>();
        public List<BuffList> BuffedBlocks = new List<BuffList>();
        private readonly Dictionary<string, double> buffed = new Dictionary<string, double>();
        public long PricePerHour = 100000;
        public int MaxBuyableHours = 50;
        public Dictionary<MyDefinitionId, int> GetItemsRequired()
        {
            var temp = new Dictionary<MyDefinitionId, int>();
            foreach (var item in this.items.Where(item => item.Enabled))
            {
                if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId,
                        out var id)) continue;
                if (!temp.ContainsKey(id))
                {
                    temp.Add(id, item.RequiredAmount);
                }
            }

            return temp;
        }

        public double GetBuffValue(string subtype)
        {
            if (buffed.TryGetValue("all", out var b))
            {
                return b;
            }
            if (buffed.TryGetValue(subtype, out var num))
            {
                return num;
            }
            return 0;
        }
        public void PutBuffedInDictionary()
        {
            foreach (var buff in BuffedBlocks)
            {
                foreach (var assembler in buff.buffs.Where(assem => assem.Enabled).Where(assem => !buffed.ContainsKey(assem.SubtypeId)))
                {
                    buffed.Add(assembler.SubtypeId, buff.PercentageBuff);
                }
            }
        }
    }
}
