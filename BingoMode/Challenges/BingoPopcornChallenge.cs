﻿using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Menu.Remix;
using MoreSlugcats;
using UnityEngine;
using Expedition;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BingoMode.Challenges
{
    using static ChallengeHooks;
    public class BingoPopcornChallenge : Challenge, IBingoChallenge
    {
        public int current;
        public SettingBox<int> amound;

        public override void UpdateDescription()
        {
            this.description = ChallengeTools.IGT.Translate("Open [<current>/<amount>] popcorn plants")
                .Replace("<current>", ValueConverter.ConvertToString(current))
                .Replace("<amount>", ValueConverter.ConvertToString(amound.Value));
            base.UpdateDescription();
        }

        public override bool Duplicable(Challenge challenge)
        {
            return challenge is not BingoPopcornChallenge;
        }

        public override string ChallengeName()
        {
            return ChallengeTools.IGT.Translate("Making Popcorn");
        }

        public override Challenge Generate()
        {
            BingoPopcornChallenge ch = new();
            ch.amound = new(UnityEngine.Random.Range(3, 8), "Amount", 0);
            return ch;
        }

        public void Pop()
        {
            if (!completed)
            {
                current++;
                UpdateDescription();
                if (current >= (int)amound.Value) CompleteChallenge();
            }
        }

        public override int Points()
        {
            return (int)amound.Value * 10;
        }

        public override bool CombatRequired()
        {
            return false;
        }

        public override bool ValidForThisSlugcat(SlugcatStats.Name slugcat)
        {
            return slugcat != MoreSlugcatsEnums.SlugcatStatsName.Saint;
        }

        public override string ToString()
        {
            return string.Concat(new string[]
            {
                "BingoPopcornChallenge",
                "~",
                current.ToString(),
                "><",
                amound.ToString(),
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
                current = int.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                amound = SettingBoxFromString(array[1]) as SettingBox<int>;
                completed = (array[2] == "1");
                hidden = (array[3] == "1");
                revealed = (array[4] == "1");
                UpdateDescription();
            }
            catch (Exception ex)
            {
                ExpLog.Log("ERROR: BingoPopcornChallenge FromString() encountered an error: " + ex.Message);
            }
        }

        public void AddHooks()
        {
            IL.SeedCob.HitByWeapon += SeedCob_HitByWeapon;
        }

        public void RemoveHooks()
        {
            IL.SeedCob.HitByWeapon -= SeedCob_HitByWeapon;
        }

        public List<object> Settings() => [amound];
    }
}