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
        private const ulong WarpDisabledOffset = 0x002C7274; 
        private const ulong AutoSaveEnabledOffset = 0x002C71C4;
        private const ulong SaveDisabledOffset = 0x002C7080;
        private const ulong GoalCompletedFlagOffset = 0x002C71B4;
        private const ulong LastEntryOffset = 0x002C70F8;
        public bool GetRetryFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + RetryFlagOffset) != 0;
        public bool GetSaveMenuFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + SaveMenuFlagOffset) != 0;
        public bool GetEventStartFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + EventStartFlagOffset) != 0;
        public bool GetInTownFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + InTownFlagOffset) != 0;
        public uint GetAPSeed() => Memory.ReadUInt(Contexts.GameContext.FlagEnumAddress + APSeedOffset);
        public bool GetInfernoFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + InfernoFlagOffset) != 0;
        public bool GetCustomGameOverFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + CustomGameOverFlagOffset) != 0;
        public void SetCustomGameOverFlag(bool value) => Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + CustomGameOverFlagOffset, (byte)(value ? 1 : 0));
        public int GetMonsterKillCount() => Memory.ReadInt(Contexts.GameContext.FlagEnumAddress + MonsterKillCountOffset);
        public bool GetGoalFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + GoalFlagOffset) != 0;
        public void SetWarpDisabledFlag(bool value) => Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + WarpDisabledOffset, (byte)(value ? 1 : 0));
        public void SetAutoSaveEnabledFlag(bool value) => Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + AutoSaveEnabledOffset, (byte)(value ? 1 : 0));
        public void SetSaveDisabledFlag(bool value) => Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + SaveDisabledOffset, (byte)(value ? 1 : 0));
        public void SetGoalCompletedFlag(bool value) => Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + GoalCompletedFlagOffset, (byte)(value ? 1 : 0));
        public int GetLastEntry() => Memory.ReadInt(Contexts.GameContext.FlagEnumAddress + LastEntryOffset);

        // ============================================================================
        // PARTY MEMBER FLAGS - Direct memory reads
        // ============================================================================
        private const ulong AdolJoinFlagOffset = 0x002C735C;
        private const ulong LaxiaJoinFlagOffset = 0x002C7360;
        private const ulong SahadJoinFlagOffset = 0x002C7364;
        private const ulong HummelJoinFlagOffset = 0x002C7368;
        private const ulong RicottaJoinFlagOffset = 0x002C736C;
        private const ulong DanaJoinFlagOffset = 0x002C7370;
        private const ulong GratikaJoinFlagOffset = 0x002CA578;
        private const ulong LuminousJoinFlagOffset = 0x002CA57C;
        private const uint PartyAverageLevelOffset = 0x002CA5CC;


        public bool GetAdolJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + AdolJoinFlagOffset) != 0;
        public bool GetLaxiaJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + LaxiaJoinFlagOffset) != 0;
        public bool GetSahadJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + SahadJoinFlagOffset) != 0;
        public bool GetHummelJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + HummelJoinFlagOffset) != 0;
        public bool GetRicottaJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + RicottaJoinFlagOffset) != 0;
        public bool GetDanaJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + DanaJoinFlagOffset) != 0;
        public bool GetGratikaJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + GratikaJoinFlagOffset) != 0;
        public bool GetLuminousJoinFlag() => Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + LuminousJoinFlagOffset) != 0;
        public void WritePartyAverageLevel(uint averageLevel) =>
                Memory.Write(Contexts.GameContext.FlagEnumAddress + PartyAverageLevelOffset, (byte)averageLevel);
        
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

        public byte GetAPSaveValue(uint id)
        {
            return Memory.ReadByte(APSaveStartAddress + id);
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
        // Map Data /////////////////////////////////////////////////////////////////
        public const ulong MapDataOffset = 0x1CA5;
        public string GetMapID() => Memory.ReadString(Contexts.GameContext.InventoryAddress + MapDataOffset,8);

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

        public void SetCharacterDamageType(uint characterId, string damageType)
        {
            // 24 is Slash, 25 is Strike, 26 is Pierce
            byte damageTypeByte = damageType switch
            {
                "Slash" => 0x18,
                "Strike" => 0x19,
                "Pierce" => 0x1A,
                _ => throw new ArgumentException("Invalid damage type", nameof(damageType))
            };
            Memory.WriteByte(Contexts.InventoryContext.SkillTableStartAddress + 0x108 + (characterId * 0x1DC), damageTypeByte);
        }

        public bool VerifyDamageType()
        {
            foreach (var kv in Options.DamageMapping)
            {
                var type = kv.Key;
                foreach (var character in kv.Value)
                {
                    uint characterId = Contexts.CharacterDataContext.GetCharacterIDByName(character);
                    byte damageTypeByte = Memory.ReadByte(Contexts.InventoryContext.SkillTableStartAddress + 0x108 + (characterId * 0x1DC));
                    string currentType = damageTypeByte switch
                    {
                        0x18 => "Slash",
                        0x19 => "Strike",
                        0x1A => "Pierce",
                        _ => "Unknown"
                    };
                    if (currentType != type)
                        return false;
                }
            }
            return true;
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

        public uint GetCharacterIDByName(string characterName)
        {
            return characterName switch
            {
                "Adol" => 0,
                "Laxia" => 1,
                "Sahad" => 2,
                "Hummel" => 3,
                "Ricotta" => 4,
                "Dana" => 5,
                "Gratika" => 7,
                "Luminous" => 8,
                _ => throw new ArgumentException("Invalid character name")
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
                // setting this to true so we make sure we grab any missed items sent before connecting and to make sure we prep our tracking items and seed settings
                ItemQueue.checkItems = true;
            }
            catch (Exception)
            {
                Log.Logger.Error("Unable to find process 'ys8.exe'");
                throw;
            }
        }

        public static void PrepSeed()
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
            Contexts.InventoryContext.CheckIfObtainedAndSet(770); // Ship's Log 1
            Contexts.InventoryContext.CheckIfObtainedAndSet(796); // Tresure Chest Key

            if (Options.FormerSanctuaryCrypt == 1) 
                Contexts.InventoryContext.CheckIfObtainedAndSet(206); // Jade Pendant

            if (Options.FinalBossAccess == 2) 
                Contexts.InventoryContext.CheckIfObtainedAndSet(InventoryMgmt.PSYCHES_ITEM_ID); // Psyches

        }
    }
}