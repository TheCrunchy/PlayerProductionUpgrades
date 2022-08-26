using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerProductionUpgrades.Models
{
    public class BuffList
    {
        //this might get speed bonus in future 
        public double PercentageBuff = 0.025;
        public List<BuffedBlock> buffs = new List<BuffedBlock>();
    }
}
