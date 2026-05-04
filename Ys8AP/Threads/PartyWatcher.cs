using Ys8AP.GlobalAddresses;
using Ys8AP.Threads;
using Ys8AP.Mem;
using Ys8AP;
using System.Collections.Generic;
using System.Threading;
using Silk.NET.GLFW;
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
        private static Dictionary<uint, CharacterStats> currentPartyMembers = new Dictionary<uint, CharacterStats>();
        private static int enemyKills = 0;
        private static DeathLinkService? _deathlinkService = null;

        // Character data structure to eliminate repetitive join flag checks
        private readonly struct CharacterInfo
        {
            public uint CharacterID { get; init; }
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

        internal static void DoLoop(object? parameters)
        {
            while (App.Client != null)
            {
                
                if (PlayerState.IsPlayerReady)
                {
                    if (Contexts.FlagEnumContext.GetMonsterKillCount() != enemyKills)
                    {
                        enemyKills = Contexts.FlagEnumContext.GetMonsterKillCount();
                        HandlePartyExperience();
                    }

                    ListenForDeath();
                }
                Thread.Sleep(1000);
            }
        }
        private static void GetCurrentPartyMembers()
        {
            currentPartyMembers.Clear();
            for (uint slot = 0; slot < PARTY_SLOTS; slot++)
            {
                uint characterId = Contexts.InventoryContext.GetPartyMemberBySlot(slot);
                if (characterId >= 0)
                    currentPartyMembers.Add(characterId, Contexts.CharacterDataContext.GetCharacterDataByID(characterId));
            }
        }

        public static void KillParty()
        {
            GetCurrentPartyMembers();
            foreach (var member in currentPartyMembers)
            {
                member.Value.CurrentHP = 0;
                Contexts.CharacterDataContext.WriteCharacterData(member.Key, member.Value);
                deathFromDeathlink = true;
            }
        }

        public static void ListenForDeath()
        {
            if (PlayerState.GameOver() && !deathFromDeathlink)
            {
                App.sendDeathLink();
                deathFromDeathlink = false;
            }
        }

        private static void HandlePartyExperience()
        {
            float PartyAverageExperience = GetPartyAverageExperience();
            
            foreach (var character in PartyCharacters)
            {
                if (!character.IsJoined())
                {
                    UpdateExperienceForCharacter(character.CharacterID, PartyAverageExperience);
                }
            }
        }

        private static float GetPartyAverageExperience()
        {
            float totalExperience = 0;
            var processedFlags = new HashSet<Func<bool>>(); // Track processed flags to count Dana once

            foreach (var character in PartyCharacters)
            {
                if (character.IsJoined() && processedFlags.Add(character.IsJoined))
                {
                    totalExperience += Contexts.CharacterDataContext.GetCharacterDataByID(character.CharacterID).CharacterEXP;
                }
            }

            var availablePartyMembers = processedFlags.Count;
            return availablePartyMembers > 0 ? totalExperience / availablePartyMembers : 0;
        }

        private static void UpdateExperienceForCharacter(uint characterID, float experience)
        {
            CharacterStats character = Contexts.CharacterDataContext.GetCharacterDataByID(characterID);
            character.CharacterEXP = experience;
            Contexts.CharacterDataContext.WriteCharacterData(characterID, character);
        }
    }
}