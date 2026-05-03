using System.Reflection;
using System;
using Archipelago.Core.Util;
using System.Reactive.Concurrency;
using System.Xml;

namespace Ys8AP.GlobalAddresses
{
    // ============================================================================
    // CONTEXTS - Global context accessors for all game memory structures
    // ============================================================================
    public static class Contexts
    {
        public static MainGame? GameContext { get; set; }
        public static FlagEnum? FlagEnumContext { get; set; }
        public static Inventory? InventoryContext { get; set; }
        public static CharacterData? CharacterDataContext { get; set; }
    }

    // ============================================================================
    // GAME CONTEXT - Main game state and address management
    // ============================================================================
    public class MainGame
    {
        [MemoryOffset(0x006B7138)]
        public ulong FlagEnumAddress { get; set; }

        [MemoryOffset(0x006CAC30)]
        public ulong InventoryAddress { get; set; }

        [MemoryOffset(0x006CAC28)]
        public ulong CharacterDataAddress { get; set; }
    }

    // ============================================================================
    // FLAG ENUM CONTEXT - Game flags, state management, and AP integration
    // ============================================================================
    public class FlagEnum
    {
        // ============================================================================
        // STATE MANAGEMENT FLAGS - Direct memory reads to avoid caching issues
        // ============================================================================
        private const ulong RetryFlagOffset = 0x002C72AC;
        private const ulong SaveMenuFlagOffset = 0x002C705C;
        private const ulong TimeAttackFlagOffset = 0x002C7130;
        private const ulong InTownFlagOffset = 0x002C7074;
        private const ulong APSeedOffset = 0x002CA5BC;
        private const ulong InfernoFlagOffset = 0x002C71B0;
        private const ulong CustomGameOverFlagOffset = 0x002CA5C4;
        private const ulong MonsterKillCountOffset = 0x002C7278;
        private const ulong GoalFlagOffset = 0x002CA5C8;

        public bool GetRetryFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + RetryFlagOffset) != 0;
        public bool GetSaveMenuFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + SaveMenuFlagOffset) != 0;
        public bool GetTimeAttackFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + TimeAttackFlagOffset) != 0;
        public bool GetInTownFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + InTownFlagOffset) != 0;
        public uint GetAPSeed() => Memory.ReadUInt(Contexts.GameContext.FlagEnumAddress + APSeedOffset);
        public bool GetInfernoFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + InfernoFlagOffset) != 0;
        public bool GetCustomGameOverFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + CustomGameOverFlagOffset) != 0;
        public int GetMonsterKillCount() => Memory.ReadInt(Contexts.GameContext.FlagEnumAddress + MonsterKillCountOffset);
        public bool GetGoalFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + GoalFlagOffset) != 0;

        // ============================================================================
        // PARTY MEMBER FLAGS - Direct memory reads
        // ============================================================================
        private const ulong AdolJoinFlagOffset = 0x002C7084;
        private const ulong LaxiaJoinFlagOffset = 0x002C7088;
        private const ulong SahadJoinFlagOffset = 0x002C708C;
        private const ulong HummelJoinFlagOffset = 0x002C7090;
        private const ulong RicottaJoinFlagOffset = 0x002C7094;
        private const ulong DanaJoinFlagOffset = 0x002C7098;

        public bool GetAdolJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + AdolJoinFlagOffset) != 0;
        public bool GetLaxiaJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + LaxiaJoinFlagOffset) != 0;
        public bool GetSahadJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + SahadJoinFlagOffset) != 0;
        public bool GetHummelJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + HummelJoinFlagOffset) != 0;
        public bool GetRicottaJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + RicottaJoinFlagOffset) != 0;
        public bool GetDanaJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + DanaJoinFlagOffset) != 0;

        // ============================================================================
        // T'S MEMOS - Direct memory reads
        // ============================================================================
        private const ulong TMemo1Offset = 0x002CA578;
        private const ulong TMemo2Offset = 0x002CA57C;
        private const ulong TMemo3Offset = 0x002CA580;
        private const ulong TMemo4Offset = 0x002CA584;

        public bool GetTMemo1() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + TMemo1Offset) != 0;
        public bool GetTMemo2() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + TMemo2Offset) != 0;
        public bool GetTMemo3() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + TMemo3Offset) != 0;
        public bool GetTMemo4() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + TMemo4Offset) != 0;
        
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

    // ============================================================================
    // CHEST PARAMS - Chest state structure
    // ============================================================================
    public class ChestParams
    {
        [MemoryOffset(0x00)]
        public byte NonChestEventFlag { get; set; } // Used for corpses and driftage, they're in the chest table but aren't actually chests

        [MemoryOffset(0x02)]
        public byte ChestOpened { get; set; }
    }

    // ============================================================================
    // INVENTORY CONTEXT - Item management, party member tracking, and skills
    // ============================================================================
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

    // ============================================================================
    // SKILL - Character skill data structure
    // ============================================================================
    public class Skill
    {
        [MemoryOffset(0x00)]
        public uint SkillID { get; set; }

        [MemoryOffset(0x04)]
        public uint SkillLevel { get; set; }

        [MemoryOffset(0x08)]
        public uint SkillExperience { get; set; }
    }

    // ============================================================================
    // CHARACTER DATA CONTEXT - Character stats and memory management
    // ============================================================================
    public class CharacterData
    {

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

    // ============================================================================
    // ADDRESS INIT - Context initialization and memory binding
    // ============================================================================
    public class AddressInit
    {
        public static void InitializeAddresses()
        {
            Contexts.GameContext = Memory.ReadObject<MainGame>(Memory.GetBaseAddress("ys8"));
            // FlagEnumContext is created as empty; flags are read directly from memory with Get methods
            Contexts.FlagEnumContext = new FlagEnum();
            Contexts.InventoryContext = Memory.ReadObject<Inventory>(Contexts.GameContext.InventoryAddress);
            Contexts.CharacterDataContext = Memory.ReadObject<CharacterData>(Contexts.GameContext.CharacterDataAddress);
        }
    }
}