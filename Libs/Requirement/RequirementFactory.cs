﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Libs
{
    public class RequirementFactory
    {
        private readonly PlayerReader playerReader;
        private readonly BagReader bagReader;
        private readonly ILogger logger;
        private Dictionary<string, Func<bool>> BuffDictionary = new Dictionary<string, Func<bool>>();

        public RequirementFactory(PlayerReader playerReader, BagReader bagReader, ILogger logger)
        {
            this.playerReader = playerReader;
            this.bagReader = bagReader;
            this.logger = logger;
        }

        public void InitialiseRequirements(KeyConfiguration item)
        {
            item.RequirementObjects.Clear();
            foreach (string requirement in item.Requirements)
            {
                item.RequirementObjects.Add(GetRequirement(item.Name, requirement));
            }

            CreateMinRequirement(item.RequirementObjects, "Mana", item.MinMana);
            CreateMinRequirement(item.RequirementObjects, "Rage", item.MinRage);
            CreateMinRequirement(item.RequirementObjects, "Energy", item.MinEnergy);
            CreateMinComboPointsRequirement(item.RequirementObjects, item);
            item.CreateCooldownRequirement();
        }

        private void CreateMinRequirement(List<Requirement> RequirementObjects, string type, int value)
        {
            if (value > 0)
            {
                RequirementObjects.Add(new Requirement
                {
                    HasRequirement = () => playerReader.ManaCurrent >= value,
                    LogMessage = () => $"{type} {playerReader.ManaCurrent} >= {value}"
                });
            }
        }

        private void CreateMinComboPointsRequirement(List<Requirement> RequirementObjects, KeyConfiguration item)
        {
            if (item.MinComboPoints > 0)
            {
                RequirementObjects.Add(new Requirement
                {
                    HasRequirement = () => playerReader.ComboPoints >= item.MinComboPoints,
                    LogMessage = () => $"Combo point {playerReader.ComboPoints} >= {item.MinComboPoints}"
                });
            }
        }

        public Requirement GetRequirement(string name, string requirement)
        {
            this.logger.LogInformation($"Processing requirement {name} : {requirement}");

            var requirementText = requirement;

            if (requirement.Contains(">") || requirement.Contains("<"))
            {
                return GetValueBasedRequirement(name, requirement);
            }

            if (requirement.Contains("npcID:"))
            {
                var parts = requirement.Split(":");
                var npcId = int.Parse(parts[1]);

                if (requirement.StartsWith("!") || requirement.StartsWith("not "))
                {
                    return new Requirement
                    {
                        HasRequirement = () => this.playerReader.TargetId != npcId,
                        LogMessage = () => $"not Target id {this.playerReader.TargetId} = {npcId}"
                    };
                }
                return new Requirement
                {
                    HasRequirement = () => this.playerReader.TargetId == npcId,
                    LogMessage = () => $"Target id {this.playerReader.TargetId} = {npcId}"
                };
            }

            if (requirement.Contains("BagItem:"))
            {
                var parts = requirement.Split(":");
                var itemId = int.Parse(parts[1]);
                var count = parts.Length < 3 ? 1 : int.Parse(parts[2]);

                if (requirement.StartsWith("!") || requirement.StartsWith("not "))
                {
                    return new Requirement
                    {
                        HasRequirement = () => this.bagReader.ItemCount(itemId) < count,
                        LogMessage = () => count == 1 ? $"{itemId} not in bag" : $"{itemId} count < {count}"
                    };
                }
                return new Requirement
                {
                    HasRequirement = () => this.bagReader.ItemCount(itemId) >= count,
                    LogMessage = () => count == 1 ? $"{itemId} in bag" : $"{itemId} count >= {count}"
                };
            }

            if (BuffDictionary.Count == 0)
            {
                BuffDictionary = new Dictionary<string, Func<bool>>
                {
                    {  "Seal", ()=> playerReader.Buffs.Seal },
                    {  "Aura", ()=>playerReader.Buffs.Aura },
                    {  "Devotion Aura", ()=>playerReader.Buffs.Aura },
                    {  "Blessing", ()=> playerReader.Buffs.Blessing },
                    {  "Blessing of Might", ()=> playerReader.Buffs.Blessing },
                    {  "Well Fed", ()=> playerReader.Buffs.WellFed },
                    {  "Eating", ()=> playerReader.Buffs.Eating },
                    {  "Drinking", ()=> playerReader.Buffs.Drinking },
                    {  "Mana Regeneration", ()=> playerReader.Buffs.ManaRegeneration },
                    {  "Fortitude", ()=> playerReader.Buffs.Fortitude },
                    {  "InnerFire", ()=> playerReader.Buffs.InnerFire },
                    {  "Divine Spirit", ()=> playerReader.Buffs.DivineSpirit },
                    {  "Renew", ()=> playerReader.Buffs.Renew },
                    {  "Shield", ()=> playerReader.Buffs.Shield },
                    {  "Mark of the Wild", ()=> playerReader.Buffs.MarkOfTheWild },
                    {  "Thorns", ()=> playerReader.Buffs.Thorns },
                    {  "Frost Armor", ()=> playerReader.Buffs.FrostArmor },
                    {  "Arcane Intellect", ()=> playerReader.Buffs.ArcaneIntellect },
                    {  "Ice Barrier", ()=>playerReader.Buffs.IceBarrier },
                    {  "Slice And Dice", ()=> playerReader.Buffs.SliceAndDice },
                    {  "Battle Shout", ()=> playerReader.Buffs.BattleShout },
                    {  "Demon Skin", ()=> playerReader.Buffs.DemonSkin },
                    {  "Has Pet", ()=> playerReader.PlayerBitValues.HasPet },

                    {  "Demoralizing Roar", ()=> playerReader.Debuffs.Roar },
                    {  "Faerie Fire", ()=> playerReader.Debuffs.FaerieFire },
                    {  "Rend", ()=> playerReader.Debuffs.Rend },

                    {  "Shadow Word: Pain", ()=> playerReader.Debuffs.ShadowWordPain },
                    {  "Curse of Weakness", ()=> playerReader.Debuffs.CurseofWeakness },

                    { "OutOfCombatRange", ()=> !playerReader.WithInCombatRange },
                    { "InCombatRange", ()=> playerReader.WithInCombatRange },

                    {  "Shooting", ()=> playerReader.IsShooting },
                };
            }

            if (BuffDictionary.Keys.Contains(requirement))
            {
                return new Requirement
                {
                    HasRequirement = BuffDictionary[requirement],
                    LogMessage = () => $"{requirementText}"
                };
            }

            if (requirement.StartsWith("not "))
            {
                requirement = requirement.Substring(4);
            }

            if (requirement.StartsWith("!"))
            {
                requirement = requirement.Substring(1);
            }

            if (BuffDictionary.Keys.Contains(requirement))
            {
                return new Requirement
                {
                    HasRequirement = () => !BuffDictionary[requirement](),
                    LogMessage = () => $"{requirementText}"
                };
            }

            logger.LogInformation($"UNKNOWN REQUIREMENT! {name} - {requirement}: try one of: {string.Join(", ", BuffDictionary.Keys)}");
            return new Requirement
            {
                HasRequirement = () => false,
                LogMessage = () => $"UNKNOWN REQUIREMENT! {requirementText}"
            };
        }

        private Requirement GetValueBasedRequirement(string name, string requirement)
        {
            var symbol = "<";
            if (requirement.Contains(">"))
            {
                symbol = ">";
            }

            var parts = requirement.Split(symbol);
            var value = int.Parse(parts[1]);

            var valueDictionary = new Dictionary<string, Func<long>>
            {
                {  "Health%", ()=> playerReader.HealthPercent },
                {  "TargetHealth%", ()=> playerReader.TargetHealthPercentage },
                {  "Mana%", ()=> playerReader.ManaPercentage },
            };

            if (!valueDictionary.Keys.Contains(parts[0]))
            {
                logger.LogInformation($"UNKNOWN REQUIREMENT! {name} - {requirement}: try one of: {string.Join(", ", valueDictionary.Keys)}");
                return new Requirement
                {
                    HasRequirement = () => false,
                    LogMessage = () => $"UNKNOWN REQUIREMENT! {requirement}"
                };
            }

            var valueCheck = valueDictionary[parts[0]];

            Func<bool> shapeshiftCheck = () => true;

            if (this.playerReader.PlayerClass == PlayerClassEnum.Druid && parts[0] == "Mana%")
            {
                shapeshiftCheck = () => playerReader.Druid_ShapeshiftForm == ShapeshiftForm.None || playerReader.Druid_ShapeshiftForm == ShapeshiftForm.Druid_Travel;
            }

            if (symbol == ">")
            {
                return new Requirement
                {
                    HasRequirement = () => shapeshiftCheck() && valueCheck() >= value,
                    LogMessage = () => $"{parts[0]} {valueCheck()} > {value}"
                };
            }
            else
            {
                return new Requirement
                {
                    HasRequirement = () => shapeshiftCheck() && valueCheck() <= value,
                    LogMessage = () => $"{parts[0]} {valueCheck()} < {value}"
                };
            }
        }
    }
}