using Ys8AP.GlobalAddresses;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.Core;
using System;

namespace Ys8AP.Threads
{
    /// <summary>
    /// Watches for changes in the party members and sends updates to AP when they change.
    /// </summary>
    internal class PartyWatcher
    {
        private const int PARTY_SLOTS = 3;
        private static bool deathFromDeathlink = false;
        private static bool deathLinkIncoming = false;
        private static string deathLinkMsg = "";
        private static bool deathLinkMsgLogged = false;
        private static Dictionary<int, CharacterStats> currentPartyMembers = new Dictionary<int, CharacterStats>();
        private static int enemyKills = 0;
        private static DeathLinkService? _deathlinkService = null;
        private static bool deathSent = false;

        // Character data structure to eliminate repetitive join flag checks
        private readonly struct CharacterInfo
        {
            public int CharacterID { get; init; }
            public Func<bool> IsJoined { get; init; }
        }

        private static readonly CharacterInfo[] PartyCharacters =
        {
            new() { CharacterID = 0, IsJoined = () => Contexts.FlagEnumContext.GetAdolJoinFlag() },
            new() { CharacterID = 1, IsJoined = () => Contexts.FlagEnumContext.GetLaxiaJoinFlag() },
            new() { CharacterID = 2, IsJoined = () => Contexts.FlagEnumContext.GetSahadJoinFlag() },
            new() { CharacterID = 3, IsJoined = () => Contexts.FlagEnumContext.GetHummelJoinFlag() },
            new() { CharacterID = 4, IsJoined = () => Contexts.FlagEnumContext.GetRicottaJoinFlag() },
            new() { CharacterID = 5, IsJoined = () => Contexts.FlagEnumContext.GetDanaJoinFlag() },
            new() { CharacterID = 7, IsJoined = () => Contexts.FlagEnumContext.GetDanaJoinFlag() },
            new() { CharacterID = 8, IsJoined = () => Contexts.FlagEnumContext.GetDanaJoinFlag() },
        };

        /// <summary>
        /// Initializes the DeathLink service and sets up event subscriptions.
        /// </summary>
        internal static DeathLinkService? InitializeDeathLink(ArchipelagoClient client, bool deathLinkEnabled, Action<DeathLink> onDeathLinkReceived)
        {
            if (!deathLinkEnabled)
                return null;

            _deathlinkService = client.EnableDeathLink();
            _deathlinkService.OnDeathLinkReceived += (deathLink) => onDeathLinkReceived?.Invoke(deathLink);
            
            return _deathlinkService;
        }

        /// <summary>
        /// Sets the death link message and flag to indicate an incoming death link.
        /// </summary>
        internal static void SetDeathLinkIncoming(string message)
        {
            deathLinkMsg = message;
            deathLinkMsgLogged = false;
            deathLinkIncoming = true;
        }

        internal static async Task DoLoop()
        {
            while (App.Client != null)
            {
                {
                    if (PlayerState.IsPlayerReady)
                    {
                        // Only reset deathSent when we're truly ready and NOT recovering from a deathlink
                        if (deathSent && !deathFromDeathlink)
                        {
                            deathSent = false;
                        }

                        if (Contexts.FlagEnumContext.GetMonsterKillCount() != enemyKills)
                        {
                            enemyKills = Contexts.FlagEnumContext.GetMonsterKillCount();
                            HandlePartyExperience();
                        }

                        // Kill player x_x - keep retrying until gameover is detected
                        if (deathLinkIncoming && PlayerState.NotInTown())
                        {
                            await Task.Delay(500); // 0.5 second delay for loading. State management mostly works but this is a safety net.
                            KillParty();
                            if (!deathLinkMsgLogged)
                            {
                                Contexts.FlagEnumContext.SetWarpDisabledFlag(false); // Re-enable warping after death
                                Log.Logger.Information(deathLinkMsg);
                                deathLinkMsgLogged = true;
                            }
                        }
                    }
                    else if (!deathSent)
                    {
                        ListenForDeath();
                    }
                    
                    if (deathLinkIncoming && PlayerState.GameOver())
                    {
                        deathLinkIncoming = false;
                    }
                }
                
                await Task.Delay(1000);
            }
        }

        private static void GetCurrentPartyMembers()
        {
            currentPartyMembers.Clear();
            for (uint slot = 0; slot < PARTY_SLOTS; slot++)
            {
                int characterId = Contexts.InventoryContext.GetPartyMemberBySlot(slot);
                if (characterId >= 0)
                    currentPartyMembers.Add(characterId, Contexts.CharacterDataContext.GetCharacterDataByID(characterId));
            }
        }

        public static void KillParty()
        {
            GetCurrentPartyMembers();
            // Gather all valid party members first
            var toKill = new List<(int id, CharacterStats stats)>();
            foreach (var member in currentPartyMembers)
            {
                if (member.Value.CharState != -1)
                {
                    toKill.Add((member.Key, member.Value));
                }
            }

            // this slightly more convluted method is meant to get as close to simultaneous as possible
            if (toKill.Count > 0)
            {
                Contexts.FlagEnumContext.SetWarpDisabledFlag(true); // Disable warping to prevent death softlock
                deathFromDeathlink = true;
                // Set all HP to 0 in memory at the same time
                foreach (var entry in toKill)
                {
                    entry.stats.CurrentHP = 0;
                }
                foreach (var entry in toKill)
                {
                    Contexts.CharacterDataContext.WriteCharacterData(entry.id, entry.stats);
                }
            }
        }

        public static void ListenForDeath()
        {
            if (PlayerState.GameOver() && !deathFromDeathlink)
            {
                App.sendDeathLink();
                deathSent = true;
            }
            else if (PlayerState.GameOver() && deathFromDeathlink)
            {
                // Mark as sent so we don't send on the next loop iteration after resetting deathFromDeathlink
                deathSent = true;
                deathFromDeathlink = false;
                deathLinkMsgLogged = false;
            }
        }

        private static void HandlePartyExperience()
        {
            uint partyAverageLevel = GetPartyAverageLevel();
            Contexts.FlagEnumContext.WritePartyAverageLevel(partyAverageLevel);
        }

        private static uint GetPartyAverageLevel()
        {
            uint totalLevel = 0;
            int count = 0;
            var seen = new HashSet<int>();

            foreach (var character in PartyCharacters)
            {
                if (!character.IsJoined())
                    continue;

                // Dana has three forms (IDs 5, 7, 8) sharing the same join flag — normalize to 5 to count her once
                int key = character.CharacterID is 7 or 8 ? 5 : character.CharacterID;

                if (seen.Add(key))
                {
                    totalLevel += Contexts.CharacterDataContext.GetCharacterDataByID(character.CharacterID).Level;
                    count++;
                }
            }

            return count > 0 ? totalLevel / (uint)count : 0;
        }
    }
}