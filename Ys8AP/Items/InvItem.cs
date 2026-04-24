
using System.Collections.Generic;

namespace Ys8AP.Items
{
    internal class InvItem
    {
        public string Name;
        public uint ItemID;
        public int QuantityMax;
        public int QuantityMaxInferno;
        public int ItemQuantity;
        public List<string>? Flags; // list of flag addresses to set when item is obtained, in hex string format (e.g. "0x002C8A30")    
        public int? CrewJoinID; // determines bit offset in SF_NPCJOINSTATE, used for all non-party castaways to track availability for work or raids.
        public bool CrewMember = false; 
        public bool Landmark = false; 

    }
}
