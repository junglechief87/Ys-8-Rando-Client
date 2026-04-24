using System.Reflection;
using System;
using Archipelago.Core.Util;
using System.Reactive.Concurrency;

namespace Ys8AP.GlobalAddresses
{
    public static class Contexts
    {
        public static MainGame? GameContext { get; set; }
        public static FlagEnum? FlagEnumContext { get; set; }
        public static Inventory? InventoryContext { get; set; }
    }

    public class MainGame
    {
        [MemoryOffset(0x006B7138)]
        public ulong FlagEnumAddress { get; set; }

        [MemoryOffset(0x006CAC30)]
        public ulong InventoryAddress { get; set; }
    }

    public class FlagEnum
    {
        [MemoryOffset(0x00000000)]
        public uint Context { get; set; } // In case I need to call custom attrbiute on the object instead of the property for some reason

        [MemoryOffset(0x002C705C)]
        public bool SaveMenuFlag { get; set; }

        [MemoryOffset(0x002C7130)]
        public bool TimeAttackFlag { get; set; }

        [MemoryOffset(0x002C71B0)]
        public bool InfernoFlag { get; set; }

        // Ts Memos /////////////////////////////////////////////////////////////
        [MemoryOffset(0x002CA578)]
        public bool TMemo1 { get; set; }

        [MemoryOffset(0x002CA57C)]
        public bool TMemo2 { get; set; }

        [MemoryOffset(0x002CA580)]
        public bool TMemo3 { get; set; }

        [MemoryOffset(0x002CA584)]
        public bool TMemo4 { get; set; }
        
        // Village Join Flags ///////////////////////////////////////////////////////
        public ulong NPCJoinState = 0x002C7308;
        
        public uint CurrentState
        {
            get
            {
                return Memory.ReadUInt(Contexts.GameContext.FlagEnumAddress + NPCJoinState);
            }
        }
        public void SetNPCJoinState(int CrewJoinID)
        {
            uint currentState = CurrentState;
            uint bitToSet = (uint)(1 << CrewJoinID);
            Memory.Write(Contexts.GameContext.FlagEnumAddress + NPCJoinState, currentState | bitToSet);
        }

        // Chest Flags /////////////////////////////////////////////////////////////
        public ulong ChestStartOffset = 0x002C9934;
        public ulong ChestStartAddress 
        { 
            get
            {
                return Contexts.GameContext.FlagEnumAddress + ChestStartOffset;
            }
        } 

        public ChestParams GetChestByID(uint id)
        {
            return Memory.ReadObject<ChestParams>(ChestStartAddress + (id * 4));
        }
    }

    public class ChestParams
    {
        [MemoryOffset(0x02)]
        public byte ChestOpened { get; set; }
    }
    public class Inventory
    {
        [MemoryOffset(0x00000000)]
        public uint Context { get; set; } // In case I need to call custom attrbiute on the object instead of the property for some reason

        public ulong ItemQuantityTblOffset = 0x00020F34;
        public ulong ItemQuantityTblAddress 
        { 
            get
            {
                return Contexts.GameContext.InventoryAddress + ItemQuantityTblOffset;
            }
        } 

        public ulong ItemObtainedFlgTblOffset = 0x00022C92;
        public ulong ItemObtainedFlgTblAddress 
        { 
            get
            {
                return Contexts.GameContext.InventoryAddress + ItemObtainedFlgTblOffset;
            }
        } 

        public ulong GetItemQuantityAddress(uint id)
        {
            return Contexts.InventoryContext.ItemQuantityTblAddress + (id * 2);
        }

        public void CheckIfObtainedAndSet(uint id)
        {
            byte obtainedByte = Memory.ReadByte(Contexts.InventoryContext.ItemObtainedFlgTblAddress + (id >> 2));
            int bitToSet = (int)((id & 0x03) * 0x02);
            bool ItemObtained = (obtainedByte & (1 << bitToSet)) != 0;

            if (!ItemObtained)
            {
                obtainedByte |= (byte)(1 << bitToSet);
                Memory.WriteByte(Contexts.InventoryContext.ItemObtainedFlgTblAddress + (id >> 2), obtainedByte);
            }
        }

        public ulong SkillTableStartOffset = 0x0001E6E4;
        public ulong SkillTableStartAddress 
        { 
            get
            {
                return Contexts.GameContext.InventoryAddress + SkillTableStartOffset;
            }
        }

        public Skill GetSkillByCharacterAndID(uint id, uint characterId)
        {
            return Memory.ReadObject<Skill>(SkillTableStartAddress + (id * 12) + (characterId * 0x1DC));
        }

        public int GetCharacterDamageType(uint characterId)
        {
            // 24 is Slash, 25 is Strike, 26 is Pierce
            return Memory.ReadInt(Contexts.InventoryContext.SkillTableStartAddress + 0x108 + (characterId * 0x1DC));
        }
    }

    public class Skill
    {
        [MemoryOffset(0x00)]
        public uint SkillID { get; set; }

        [MemoryOffset(0x04)]
        public uint SkillLevel { get; set; }

        [MemoryOffset(0x08)]
        public uint SkillExperience { get; set; }
    }

    public class AddressInit
    {
        public static void InitializeAddresses()
        {
            Contexts.GameContext = Memory.ReadObject<MainGame>(Memory.GetBaseAddress("ys8"));
            Contexts.FlagEnumContext = Memory.ReadObject<FlagEnum>(Contexts.GameContext.FlagEnumAddress);
            Contexts.InventoryContext = Memory.ReadObject<Inventory>(Contexts.GameContext.InventoryAddress);
        }
    }
}