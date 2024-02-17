﻿using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Menu.Remix;
using MoreSlugcats;
using UnityEngine;
using Expedition;
using ItemType = AbstractPhysicalObject.AbstractObjectType;
using CreatureType = CreatureTemplate.Type;
using MSCItemType = MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace BingoMode.Challenges
{
    using static ChallengeHooks;
    public class BingoEatChallenge : Challenge, IBingoChallenge
    {
        public SettingBox<string> foodType;
        public SettingBox<int> amountRequired;
        public int currentEated;
        public bool isCreature;

        public override void UpdateDescription()
        {
            if (ChallengeTools.creatureNames == null)
            {
                ChallengeTools.CreatureName(ref ChallengeTools.creatureNames);
            }
            description = ChallengeTools.IGT.Translate("Eat [<current>/<amount>] <food_type>")
                .Replace("<current>", ValueConverter.ConvertToString(currentEated))
                .Replace("<amount>", ValueConverter.ConvertToString(amountRequired.Value))
                .Replace("<food_type>", isCreature ? ChallengeTools.IGT.Translate(ChallengeTools.creatureNames[new CreatureType(foodType.Value).Index]) : ChallengeTools.ItemName(new(foodType.Value)));
            base.UpdateDescription();
        }
    
        public override string ChallengeName()
        {
            return ChallengeTools.IGT.Translate("Eating Food");
        }
    
        public override bool Duplicable(Challenge challenge)
        {
            return challenge is not BingoEatChallenge || (challenge as BingoEatChallenge).foodType != foodType;
        }
    
        public override Challenge Generate()
        {
            bool c = UnityEngine.Random.value < 0.5f;

            // Choose random food, if Riv is selected then make glowweed available
            string randomFood;
            if (c)
            {
                randomFood = ChallengeUtils.CreatureFoodTypes[UnityEngine.Random.Range(0, ChallengeUtils.CreatureFoodTypes.Length)];
            }
            else
            {
                randomFood = ChallengeUtils.ItemFoodTypes[UnityEngine.Random.Range(0, ChallengeUtils.ItemFoodTypes.Length -
                            (ModManager.MSC ? (ExpeditionData.slugcatPlayer == MoreSlugcatsEnums.SlugcatStatsName.Rivulet ? 0 : 1) : 4))];
            }

            return new BingoEatChallenge()
            {
                foodType = new(randomFood, "Food type", 0),
                isCreature = c,
                amountRequired = new(UnityEngine.Random.Range(3, 8) * (isCreature && foodType.Value == "Fly" ? 3 : 1), "Amount", 1)//Mathf.RoundToInt(Mathf.Lerp(3, Mathf.Lerp(6, 10, UnityEngine.Random.value), ExpeditionData.challengeDifficulty)) * (isCreature && creatureFoodType == CreatureType.Fly ? 3 : 1)
            };
        }
    
        public override bool CombatRequired()
        {
            return false;
        }
    
        public override int Points()
        {
            return Mathf.RoundToInt(6 * FoodDifficultyMultiplier()) * amountRequired.Value * (hidden ? 2 : 1);
        }
    
        public float FoodDifficultyMultiplier()
        {
            switch (foodType.Value)
            {
                case "DangleFruit": return 0.5f;
                case "SlimeMold": return 1.33f;
                case "GlowWeed": return 1.66f;
                case "DandelionPeach": return 1.33f;
                case "SmallNeedleWorm": return 1.5f;
                case "Fly": return 0.33f;
            }
    
            return 1f;
        }
    
        public void FoodEated(IPlayerEdible thisEdibleIsShit)
        {
            if (thisEdibleIsShit != null && thisEdibleIsShit is PhysicalObject p &&
                (isCreature ? (p.abstractPhysicalObject is AbstractCreature g && g.creatureTemplate.type.value == foodType.Value) : (p.abstractPhysicalObject.type.value == foodType.Value)))
            {
                currentEated++;
                UpdateDescription();
                if (currentEated >= amountRequired.Value) CompleteChallenge();
            }
        }
    
        public override string ToString()
        {
            return string.Concat(new string[]
            {
                "BingoEatChallenge",
                "~",
                amountRequired.ToString(),
                "><",
                currentEated.ToString(),
                "><",
                isCreature ? "1" : "0",
                "><",
                foodType.ToString(),
                "><",
                completed ? "1" : "0",
                "><",
                hidden ? "1" : "0",
                "><",
                revealed ? "1" : "0"
            });
        }
    
        public override void FromString(string args)
        {
            try
            {
                string[] array = Regex.Split(args, "><");
                amountRequired = SettingBoxFromString(array[0]) as SettingBox<int>;
                currentEated = int.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                isCreature = (array[2] == "1");
                foodType = SettingBoxFromString(array[3]) as SettingBox<string>;
                completed = (array[4] == "1");
                hidden = (array[5] == "1");
                revealed = (array[6] == "1");
                UpdateDescription();
            }
            catch (Exception ex)
            {
                ExpLog.Log("ERROR: BingoEatChallenge FromString() encountered an error: " + ex.Message);
            }
        }

        public void AddHooks()
        {
            On.Player.ObjectEaten += Player_ObjectEaten;
        }

        public void RemoveHooks()
        {
            On.Player.ObjectEaten -= Player_ObjectEaten;
        }

        public List<object> Settings() => [foodType, amountRequired];
    }
}
