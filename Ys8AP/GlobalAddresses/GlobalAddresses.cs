using System.Reflection;
using System;
using Archipelago.Core.Util;
using System.Reactive.Concurrency;
using System.Xml;

namespace Ys8AP.GlobalAddresses
{
    public static class Contexts
    {
        public static MainGame? GameContext { get; set; }
        public static FlagEnum? FlagEnumContext { get; set; }
        public static Inventory? InventoryContext { get; set; }
        public static CharacterData? CharacterDataContext { get; set; }
    }

    public class MainGame
    {
        [MemoryOffset(0x006B7138)]
        public ulong FlagEnumAddress { get; set; }

        [MemoryOffset(0x006CAC30)]
        public ulong InventoryAddress { get; set; }

        [MemoryOffset(0x006CAC28)]
        public ulong CharacterDataAddress { get; set; }
    }

    public class FlagEnum
    {
        [MemoryOffset(0x00000000)]
        public uint Context { get; set; } // In case I need to call custom attrbiute on the object instead of the property for some reason

        // State Management Flags //////////////////////////////////////////////////////////
        [MemoryOffset(0x002C72AC)]
        public bool RetryFlag { get; set; }

        [MemoryOffset(0x002C705C)]
        public bool SaveMenuFlag { get; set; }

        [MemoryOffset(0x002C7130)]
        public bool TimeAttackFlag { get; set; }

        [MemoryOffset(0x002C7074)]
        public bool InTownFlag { get; set; }

        [MemoryOffset(0x002CA5BC)]
        public uint APSeed { get; set; } 

        [MemoryOffset(0x002C71B0)]
        public bool InfernoFlag { get; set; }

        [MemoryOffset(0x002CA5C4)]
        public bool CustomGameOverFlag { get; set; }

        [MemoryOffset(0x002C7278)]
        public int MonsterKillCount { get; set; }

        [MemoryOffset(0x002CA5C8)]
        public bool GoalFlag { get; set; }

        // Available Party Member Flags //////////////////////////////////////////////////////////
        [MemoryOffset(0x002C7084)]
        public bool AdolJoinFlag { get; set; }

        [MemoryOffset(0x002C7088)]        
        public bool LaxiaJoinFlag { get; set; }

        [MemoryOffset(0x002C708C)]        
        public bool SahadJoinFlag { get; set; }

        [MemoryOffset(0x002C7090)]        
        public bool HummelJoinFlag { get; set; }

        [MemoryOffset(0x002C7094)]        
        public bool RicottaJoinFlag { get; set; }

        [MemoryOffset(0x002C7098)]        
        public bool DanaJoinFlag { get; set; }

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

        // AP Save /////////////////////////////////////////////////////////////

        public ulong APSaveStartOffset = 0x002C9D70;

        public ulong APSaveStartAddress 
        { 
            get
            {
                return Contexts.GameContext.FlagEnumAddress + APSaveStartOffset;
            }
        }

        public int GetAPSaveValue(uint id)
        {
            return (int)Memory.ReadByte(APSaveStartAddress + id);
        }

        public void SetAPSaveValue(uint id)
        {
            int value = GetAPSaveValue(id) + 1;
            Memory.WriteByte(APSaveStartAddress + id, (byte)value);
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


        // Item Quantities //////////////////////////////////////////////////////////
        public ulong ItemQuantityTblOffset = 0x00020F34;
        public ulong ItemQuantityTblAddress 
        { 
            get
            {
                return Contexts.GameContext.InventoryAddress + ItemQuantityTblOffset;
            }
        } 

        // Item Obtained Flags //////////////////////////////////////////////////////////
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

        // Skill Data //////////////////////////////////////////////////////////
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

        public void SetSkillByCharacterAndID(uint id, uint characterId, Skill skill)
        {
            Memory.WriteObject<Skill>(SkillTableStartAddress + (id * 12) + (characterId * 0x1DC), skill);
        }

        public int GetCharacterDamageType(uint characterId)
        {
            // 24 is Slash, 25 is Strike, 26 is Pierce
            return Memory.ReadInt(Contexts.InventoryContext.SkillTableStartAddress + 0x108 + (characterId * 0x1DC));
        }

        // Current Party Members //////////////////////////////////////////////////////////
        public ulong PartyMemberOffset = 0x001809F8;

        public uint GetPartyMemberBySlot(uint slot)
        {
            return (uint)Memory.ReadInt(Contexts.GameContext.InventoryAddress + PartyMemberOffset + (slot * 4));
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

    public class CharacterData
    {
        [MemoryOffset(0x00000000)]
        public uint Context { get; set; } // In case I need to call custom attrbiute on the object instead of the property for some reason

        [MemoryOffset(0x002EBC28)]
        public ulong AdolData { get; set; }

        [MemoryOffset(0x002EBCA0)]
        public ulong LaxiaData { get; set; }

        [MemoryOffset(0x002EBD18)]
        public ulong SahadData { get; set; }

        [MemoryOffset(0x002EBD90)]
        public ulong HummelData { get; set; }

        [MemoryOffset(0x002EBE08)]
        public ulong RicottaData { get; set; }

        [MemoryOffset(0x002EBE80)]
        public ulong DanaData { get; set; }

        [MemoryOffset(0x002EBF70)]
        public ulong Dana2Data { get; set; } // Gratika

        [MemoryOffset(0x002EBFE8)]
        public ulong Dana3Data { get; set; } // Luminous

        public CharacterStats GetCharacterDataByID(uint characterId)
        {
            return characterId switch
            {
                0 => Memory.ReadObject<CharacterStats>(AdolData),
                1 => Memory.ReadObject<CharacterStats>(LaxiaData),
                2 => Memory.ReadObject<CharacterStats>(SahadData),
                3 => Memory.ReadObject<CharacterStats>(HummelData),
                4 => Memory.ReadObject<CharacterStats>(RicottaData),
                5 => Memory.ReadObject<CharacterStats>(DanaData),
                7 => Memory.ReadObject<CharacterStats>(Dana2Data),
                8 => Memory.ReadObject<CharacterStats>(Dana3Data),
                _ => throw new ArgumentException("Invalid character ID")
            };
        }

        public ulong GetCharacterDataAddressByID(uint characterId)
        {
            return characterId switch
            {
                0 => AdolData,
                1 => LaxiaData,
                2 => SahadData,
                3 => HummelData,
                4 => RicottaData,
                5 => DanaData,
                7 => Dana2Data,
                8 => Dana3Data,
                _ => throw new ArgumentException("Invalid character ID")
            };
        }
        public void WriteCharacterData(uint characterId, CharacterStats data)
        {
            Memory.WriteObject(GetCharacterDataAddressByID(characterId), data);
        }
    }

    public class CharacterStats
    {
        [MemoryOffset(0x1088)]
        public float CurrentHP { get; set; }

        [MemoryOffset(0x10A8)]
        public float CharacterEXP { get; set; }
    }

    public class AddressInit
    {
        public static void InitializeAddresses()
        {
            Contexts.GameContext = Memory.ReadObject<MainGame>(Memory.GetBaseAddress("ys8"));
            Contexts.FlagEnumContext = Memory.ReadObject<FlagEnum>(Contexts.GameContext.FlagEnumAddress);
            Contexts.InventoryContext = Memory.ReadObject<Inventory>(Contexts.GameContext.InventoryAddress);
            Contexts.CharacterDataContext = Memory.ReadObject<CharacterData>(Contexts.GameContext.CharacterDataAddress);
        }
    }
}