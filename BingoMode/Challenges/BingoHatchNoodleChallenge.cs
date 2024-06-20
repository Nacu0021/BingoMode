﻿using BingoMode.BingoSteamworks;
using Expedition;
using Menu.Remix;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BingoMode.Challenges
{
    using static ChallengeHooks;
    public class BingoHatchNoodleChallenge : BingoChallenge
    {
        public SettingBox<int> amount;
        public int current;
        public SettingBox<bool> atOnce;

        public override void UpdateDescription()
        {
            this.description = ChallengeTools.IGT.Translate("Hatch [<current>/<amount>] noodleflies from eggs" + (atOnce.Value ? " in one cycle" : ""))
                .Replace("<current>", ValueConverter.ConvertToString(current))
                .Replace("<amount>", ValueConverter.ConvertToString(amount.Value));
            base.UpdateDescription();
        }

        public override bool RequireSave() => false;

        public override Phrase ConstructPhrase()
        {
            Phrase p = new Phrase([new Icon("needleEggSymbol", 1f, ChallengeUtils.ItemOrCreatureIconColor("needleEggSymbol")), new Icon("Kill_SmallNeedleWorm", 1f, ChallengeUtils.ItemOrCreatureIconColor("SmallNeedleWorm"))], [atOnce.Value ? 3 : 2]);
            if (atOnce.Value) p.words.Add(new Icon("cycle_limit", 1f, UnityEngine.Color.white));
            p.words.Add(new Counter(current, amount.Value));
            return p;
        }

        public override bool Duplicable(Challenge challenge)
        {
            return challenge is not BingoHatchNoodleChallenge;
        }

        public override string ChallengeName()
        {
            return ChallengeTools.IGT.Translate("Noodlefly Hatching");
        }

        public override Challenge Generate()
        {
            bool onc = UnityEngine.Random.value < 0.5f;
            return new BingoHatchNoodleChallenge
            {
                atOnce = new(onc, "At Once", 0),
                amount = new(UnityEngine.Random.Range(1, onc ? 3 : 5) * 2, "Amount", 1),
            };
        }

        public void Hatch()
        {
            Plugin.logger.LogMessage("Trying to hatch this mf");
            if (!completed && !revealed && !TeamsCompleted[SteamTest.team] && !hidden)
            {
                Plugin.logger.LogMessage("Hatching this mf");
                current++;
                UpdateDescription();
                if (!RequireSave() && !atOnce.Value) Expedition.Expedition.coreFile.Save(false);
                if (current >= amount.Value) CompleteChallenge();
            }
        }

        public override int Points()
        {
            return amount.Value * 10;
        }

        public override bool CombatRequired()
        {
            return false;
        }

        public override bool ValidForThisSlugcat(SlugcatStats.Name slugcat)
        {
            return true;
        }

        public override void Reset()
        {
            base.Reset();
            current = 0;
        }

        public override string ToString()
        {
            return string.Concat(new string[]
            {
                "BingoHatchNoodleChallenge",
                "~",
                atOnce.Value && !completed ? "0" : current.ToString(),
                "><",
                amount.ToString(),
                "><",
                atOnce.ToString(),
                "><",
                completed ? "1" : "0",
                "><",
                hidden ? "1" : "0",
                "><",
                revealed ? "1" : "0",
                "><",
                TeamsToString()
            });
        }

        public override void FromString(string args)
        {
            try
            {
                string[] array = Regex.Split(args, "><");
                current = int.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                amount = SettingBoxFromString(array[1]) as SettingBox<int>;
                atOnce = SettingBoxFromString(array[2]) as SettingBox<bool>;
                completed = (array[3] == "1");
                hidden = (array[4] == "1");
                revealed = (array[5] == "1");
                TeamsFromString(array[6]);
                UpdateDescription();
            }
            catch (Exception ex)
            {
                ExpLog.Log("ERROR: BingoHatchNoodleChallenge FromString() encountered an error: " + ex.Message);
                throw ex;
            }
        }

        public override void AddHooks()
        {
            On.SmallNeedleWorm.PlaceInRoom += SmallNeedleWorm_PlaceInRoom;
        }

        public override void RemoveHooks()
        {
            On.SmallNeedleWorm.PlaceInRoom -= SmallNeedleWorm_PlaceInRoom;
        }

        public override List<object> Settings() => [atOnce, amount];
        public List<string> SettingNames() => ["At Once", "Amount"];
    }
}