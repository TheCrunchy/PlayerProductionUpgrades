using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerProductionUpgrades.Patches;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace PlayerProductionUpgrades
{
    public class Commands : CommandModule
    {
        [Command("assdebug", "debug command")]
        [Permission(MyPromoteLevel.None)]
        public void Debug(int amount)
        {
            
        }
    }
}
