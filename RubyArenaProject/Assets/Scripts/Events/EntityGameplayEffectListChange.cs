using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Events
{
    enum EntityGamePlayEffectChangeType
    {
        Added,
        Removed,
        Refreshed
    }
    class EntityGameplayEffectListChange
    {
        public ulong recieverNetworkObjectId;
        public GameplayEffectSO effectSO;
        //Potentiall maybe later on pass also the endtime, so it doesnt have to be calculated by the UI
        public EntityGamePlayEffectChangeType Type;
    }
}
