﻿using Expedition;
using Menu.Remix;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BingoMode.Challenges
{
    using static ChallengeHooks;
    public class BingoStealChallenge : Challenge, IBingoChallenge
    {
        public int current;
        public SettingBox<int> amount;
        public SettingBox<bool> toll;
        public SettingBox<string> subject;
        public List<EntityID> checkedIDs;
        public int Index { get; set; }
        public bool RequireSave { get; set; }
        public bool Failed { get; set; }

        public override void UpdateDescription()
        {
            this.description = ChallengeTools.IGT.Translate("Steal [<current>/<amount>] <item> from " + (toll.Value ? "a Scavenger toll" : "Scavengers"))
                .Replace("<current>", ValueConverter.ConvertToString(current))
                .Replace("<amount>", ValueConverter.ConvertToString(amount.Value))
                .Replace("<item>", ChallengeTools.ItemName(new(subject.Value)));
            base.UpdateDescription();
        }

        public override bool Duplicable(Challenge challenge)
        {
            return challenge is not BingoStealChallenge || ((challenge as BingoStealChallenge).subject != subject && (challenge as BingoStealChallenge).toll != toll);
        }

        public override string ChallengeName()
        {
            return ChallengeTools.IGT.Translate("Theft");
        }

        public override Challenge Generate()
        {
            bool taxEvasion = UnityEngine.Random.value < 0.5f;
            string itme = "Spear";
            if (taxEvasion)
            {
                itme = UnityEngine.Random.value < 0.5f ? "Spear": "DataPearl";
            }
            else itme = ChallengeUtils.StealableStolable[UnityEngine.Random.Range(0, ChallengeUtils.StealableStolable.Length)];

            return new BingoStealChallenge
            {
                checkedIDs = [],
                toll = new(taxEvasion, "From Scavenger Toll", 0),
                subject = new(itme, "Item", 1, listName: "theft"),
                amount = new(UnityEngine.Random.Range(1, itme == "ScavengerBomb" ? 3 : 5), "Amount", 2)
            };
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

        public void Stoled(AbstractPhysicalObject item, bool tollCheck)
        {
            if (!completed && item.type.value == subject.Value && tollCheck == toll.Value && !checkedIDs.Contains(item.ID))
            {
                current++;
                UpdateDescription();
                if (!RequireSave) Expedition.Expedition.coreFile.Save(false);
                if (current >= amount.Value)
                {
                    CompleteChallenge();
                }
                checkedIDs.Add(item.ID);
            }
        }

        public override string ToString()
        {
            return string.Concat(new string[]
            {
                "BingoStealChallenge",
                "~",
                subject.ToString(),
                "><",
                toll.ToString(),
                "><",
                current.ToString(),
                "><",
                amount.ToString(),
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
                subject = SettingBoxFromString(array[0]) as SettingBox<string>;
                toll = SettingBoxFromString(array[1]) as SettingBox<bool>;
                current = int.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                amount = SettingBoxFromString(array[3]) as SettingBox<int>;
                completed = (array[4] == "1");
                hidden = (array[5] == "1");
                revealed = (array[6] == "1");
                checkedIDs = [];
                UpdateDescription();
            }
            catch (Exception ex)
            {
                ExpLog.Log("ERROR: BingoStealChallenge FromString() encountered an error: " + ex.Message);
                throw ex;
            }
        }

        public void AddHooks()
        {
            On.ScavengerOutpost.PlayerTracker.Update += PlayerTracker_Update;
            On.SocialEventRecognizer.Theft += SocialEventRecognizer_Theft;
        }

        public void RemoveHooks()
        {
            On.ScavengerOutpost.PlayerTracker.Update -= PlayerTracker_Update;
            On.SocialEventRecognizer.Theft -= SocialEventRecognizer_Theft;
        }

        public List<object> Settings() => [amount, toll, subject];
    }
}