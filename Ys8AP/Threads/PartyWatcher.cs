using Ys8AP.GlobalAddresses;
using Ys8AP.Mem;
using Ys8AP;
using System.Collections.Generic;
using System.Threading;
using Silk.NET.GLFW;

namespace Ys8AP.Threads
{
    /// <summary>
    /// Watches for changes in the party members and sends updates to AP when they change.
    /// </summary>
    internal class PartyWatcher
    {
        private static bool deathFromDeathlink = false;
        private static Dictionary<uint, CharacterStats> currentPartyMembers = new Dictionary<uint, CharacterStats>();
        private static int enemyKills = 0;
        internal static void DoLoop(object? parameters)
        {
            while (true)
            {
                
                if (PlayerState.PlayerReady())
                {
                    if (Contexts.FlagEnumContext.MonsterKillCount != enemyKills)
                    {
                        HandlePartyExperience();
                    }
                }
                Thread.Sleep(1000);
            }
        }
        private static void GetCurrentPartyMembers()
        {
            currentPartyMembers.Clear();
            for (uint slot = 0; slot < 4; slot++)
            {
                uint characterId = Contexts.InventoryContext.GetPartyMemberBySlot(slot);
                if (characterId >= 0)
                {
                    currentPartyMembers.Add(characterId, Contexts.CharacterDataContext.GetCharacterDataByID((uint)characterId));
                }
            }
        }

        public static async void KillParty()
        {
            GetCurrentPartyMembers();
            foreach (var member in currentPartyMembers)
            {
                member.Value.CurrentHP = 0;
                Contexts.CharacterDataContext.WriteCharacterData(member.Key, member.Value);
                deathFromDeathlink = true;
            }
        }

        public static async void ListenForDeath()
        {
            if (PlayerState.GameOver() && !deathFromDeathlink)
            {
                App.sendDeathLink();
                deathFromDeathlink = false;
            }
        }

        private static async void HandlePartyExperience()
        {
            float PartyAverageExperience = GetPartyAverageExperience();
            CharacterStats CurrentCharacter = null;
            
            if (Contexts.FlagEnumContext.AdolJoinFlag)
            {
                UpdateExperienceForCharacter(0, PartyAverageExperience);
            }
            if (Contexts.FlagEnumContext.LaxiaJoinFlag)
            {
                UpdateExperienceForCharacter(1, PartyAverageExperience);
            }
            if (Contexts.FlagEnumContext.SahadJoinFlag)
            {
                UpdateExperienceForCharacter(2, PartyAverageExperience);
            }
            if (Contexts.FlagEnumContext.HummelJoinFlag)
            {
                UpdateExperienceForCharacter(3, PartyAverageExperience);
            }
            if (Contexts.FlagEnumContext.RicottaJoinFlag)
            {
                UpdateExperienceForCharacter(4, PartyAverageExperience);
            }
            if (Contexts.FlagEnumContext.DanaJoinFlag)
            {
                UpdateExperienceForCharacter(5, PartyAverageExperience);
                UpdateExperienceForCharacter(7, PartyAverageExperience);
                UpdateExperienceForCharacter(8, PartyAverageExperience);
            }

        }

        private static float GetPartyAverageExperience()
        {
            float AvailablePartyMembers = 0;
            float totalExperience = 0;

            if (Contexts.FlagEnumContext.AdolJoinFlag)
            {
                AvailablePartyMembers++;
                totalExperience += Contexts.CharacterDataContext.GetCharacterDataByID(0).CharacterEXP;
            }
            if (Contexts.FlagEnumContext.LaxiaJoinFlag)
            {
                AvailablePartyMembers++;
                totalExperience += Contexts.CharacterDataContext.GetCharacterDataByID(1).CharacterEXP;
            }
            if (Contexts.FlagEnumContext.SahadJoinFlag)
            {
                AvailablePartyMembers++;
                totalExperience += Contexts.CharacterDataContext.GetCharacterDataByID(2).CharacterEXP;
            }
            if (Contexts.FlagEnumContext.HummelJoinFlag)
            {
                AvailablePartyMembers++;
                totalExperience += Contexts.CharacterDataContext.GetCharacterDataByID(3).CharacterEXP;
            }
            if (Contexts.FlagEnumContext.RicottaJoinFlag)
            {
                AvailablePartyMembers++;
                totalExperience += Contexts.CharacterDataContext.GetCharacterDataByID(4).CharacterEXP;
            }
            if (Contexts.FlagEnumContext.DanaJoinFlag)
            {
                AvailablePartyMembers++;
                totalExperience += Contexts.CharacterDataContext.GetCharacterDataByID(5).CharacterEXP;
            }


            return AvailablePartyMembers > 0 ? totalExperience / AvailablePartyMembers : 0;
        }

        private static void UpdateExperienceForCharacter(uint characterID, float experience)
        {
            CharacterStats character = Contexts.CharacterDataContext.GetCharacterDataByID(characterID);
            character.CharacterEXP = experience;
            Contexts.CharacterDataContext.WriteCharacterData(characterID, character);
        }
    }
}