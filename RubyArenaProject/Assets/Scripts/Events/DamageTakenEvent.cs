using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Events
{
    public class DamageTakenEvent
    {
  

        public ulong damagerNetworkObjectId;
        public ulong recieverNetworkObjectId;

        public ulong spellCarrierNetworkObjectId;
        public float damageAmountPostMitigation;
        public float damageAmountPreMitigation;

        public float healthBefore;
        public float healthAfter;
    }
}
