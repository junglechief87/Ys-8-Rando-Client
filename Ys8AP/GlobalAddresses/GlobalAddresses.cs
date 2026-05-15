using System.Reflection;
using System;
using Archipelago.Core.Util;
using Serilog;
using Ys8AP.Items;
using Ys8AP.Threads;

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
        private const ulong EventStartFlagOffset = 0x002C7268; // Flag is set on game start to allow events to trigger
        private const ulong InTownFlagOffset = 0x002C7074;
        private const ulong APSeedOffset = 0x002CA5BC;
        private const ulong InfernoFlagOffset = 0x002C71B0;
        private const ulong CustomGameOverFlagOffset = 0x002CA5C4;
        private const ulong MonsterKillCountOffset = 0x002C7278;
        private const ulong GoalFlagOffset = 0x002CA5C8;

        public bool GetRetryFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + RetryFlagOffset) != 0;
        public bool GetSaveMenuFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + SaveMenuFlagOffset) != 0;
        public bool GetEventStartFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + EventStartFlagOffset) != 0;
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
        private const uint PartyAverageLevelOffset = 0x002CA5CC;


        public bool GetAdolJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + AdolJoinFlagOffset) != 0;
        public bool GetLaxiaJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + LaxiaJoinFlagOffset) != 0;
        public bool GetSahadJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + SahadJoinFlagOffset) != 0;
        public bool GetHummelJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + HummelJoinFlagOffset) != 0;
        public bool GetRicottaJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + RicottaJoinFlagOffset) != 0;
        public bool GetDanaJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + DanaJoinFlagOffset) != 0;
        public void WritePartyAverageLevel(uint averageLevel) =>
            Memory.Write(Contexts.GameContext.FlagEnumAddress + PartyAverageLevelOffset, (byte)averageLevel);

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
        public const ulong ItemObtainedFlgTblOffset = 0x00022C92;
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
        public const ulong SkillTableStartOffset = 0x0001E6E4;
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

        public int GetPartyMemberBySlot(uint slot)
        {
            return Memory.ReadInt(Contexts.GameContext.InventoryAddress + PartyMemberOffset + (slot * 4));
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
        // ============================================================================
        // CHARACTER DATA ADDRESSES - Direct memory lookups to avoid caching issues
        // ============================================================================
        private const ulong AdolDataOffset = 0x002EBC28;
        private const ulong LaxiaDataOffset = 0x002EBCA0;
        private const ulong SahadDataOffset = 0x002EBD18;
        private const ulong HummelDataOffset = 0x002EBD90;
        private const ulong RicottaDataOffset = 0x002EBE08;
        private const ulong DanaDataOffset = 0x002EBE80;
        private const ulong Dana2DataOffset = 0x002EBF70; // Gratika
        private const ulong Dana3DataOffset = 0x002EBFE8; // Luminous

        public ulong AdolData => Memory.ReadULong(Contexts.GameContext.CharacterDataAddress + AdolDataOffset);
        public ulong LaxiaData => Memory.ReadULong(Contexts.GameContext.CharacterDataAddress + LaxiaDataOffset);
        public ulong SahadData => Memory.ReadULong(Contexts.GameContext.CharacterDataAddress + SahadDataOffset);
        public ulong HummelData => Memory.ReadULong(Contexts.GameContext.CharacterDataAddress + HummelDataOffset);
        public ulong RicottaData => Memory.ReadULong(Contexts.GameContext.CharacterDataAddress + RicottaDataOffset);
        public ulong DanaData => Memory.ReadULong(Contexts.GameContext.CharacterDataAddress + DanaDataOffset);
        public ulong Dana2Data => Memory.ReadULong(Contexts.GameContext.CharacterDataAddress + Dana2DataOffset);
        public ulong Dana3Data => Memory.ReadULong(Contexts.GameContext.CharacterDataAddress + Dana3DataOffset);

        public CharacterStats GetCharacterDataByID(int characterId)
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

        public ulong GetCharacterDataAddressByID(int characterId)
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
        
        public void WriteCharacterData(int characterId, CharacterStats data)
        {
            Memory.WriteObject(GetCharacterDataAddressByID(characterId), data);
        }
    }

    public class CharacterStats
    {
        [MemoryOffset(0x1080)]
        public uint Level { get; set; }

        [MemoryOffset(0x1088)]
        public float CurrentHP { get; set; }

        [MemoryOffset(0x10A8)]
        public float CharacterEXP { get; set; }

        [MemoryOffset(0x1290)]
        public int CharState { get; set; } // not sure what this does, but it's consistently -1 when the character is locked due to cutscenes, loading, events, etc. 3 seems to be nuetral.
    }

    // ============================================================================
    // ADDRESS INIT - Context initialization and memory binding
    // ============================================================================
    public class AddressInit
    {
        public static void InitializeAddresses()
        {
            try
            {
                Contexts.GameContext = Memory.ReadObject<MainGame>(Memory.GetBaseAddress("ys8"));
                // FlagEnumContext is created as empty; flags are read directly from memory with Get methods
                Contexts.FlagEnumContext = new FlagEnum();
                Contexts.CharacterDataContext = new CharacterData();;
                Contexts.InventoryContext = new Inventory();
                PrepSeed();  // Prepare seed tracking items and flags on connect      
            }
            catch (Exception)
            {
                Log.Logger.Error("Unable to find process 'ys8.exe'");
                throw;
            }
        }

        private static void PrepSeed()
        {
            // On connect reveal the player tracking items in the inventory.
            Contexts.InventoryContext.CheckIfObtainedAndSet(InventoryMgmt.PROGRESSIVE_SHOP_RANK_ID); // Progressive Shop Rank
            Contexts.InventoryContext.CheckIfObtainedAndSet(InventoryMgmt.CASTAWAY_TRACKING_ID); // Castaway
            Contexts.InventoryContext.CheckIfObtainedAndSet(InventoryMgmt.LANDMARK_TRACKING_ID); // Discovery
            Contexts.InventoryContext.CheckIfObtainedAndSet(InventoryMgmt.PROGRESSIVE_RAID_LIST_ID); // Progressive Raid List
            Contexts.InventoryContext.CheckIfObtainedAndSet(698); // Maiden Journal
            Contexts.InventoryContext.CheckIfObtainedAndSet(699); // Frozen Flower
            Contexts.InventoryContext.CheckIfObtainedAndSet(700); // Blue Seal of Whirlying Water
            Contexts.InventoryContext.CheckIfObtainedAndSet(701); // Green Seal of Roaring Stone
            Contexts.InventoryContext.CheckIfObtainedAndSet(702); // Golden Seal of Piercing Light
            Contexts.InventoryContext.CheckIfObtainedAndSet(727); // Shrine Maiden Amulet
            Contexts.InventoryContext.CheckIfObtainedAndSet(739); // Glow Stone

            if (Options.FormerSanctuaryCrypt == 1) 
                Contexts.InventoryContext.CheckIfObtainedAndSet(206); // Jade Pendant

            if (Options.FinalBossAccess == 2) 
                Contexts.InventoryContext.CheckIfObtainedAndSet(InventoryMgmt.PSYCHES_ITEM_ID); // Psyches

            ItemQueue.checkItems = true;
        }
    }
}