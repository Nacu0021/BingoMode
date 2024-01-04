﻿using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Menu.Remix;
using MoreSlugcats;
using UnityEngine;
using Expedition;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace BingoMode.Challenges
{
    using static ChallengeHooks;
    //Using counts as either throwing an item, or holding it for more than 5 seconds
    public class BingoDontUseItemChallenge : Challenge, IBingoChallenge
    {
        public AbstractPhysicalObject.AbstractObjectType item;
        public bool isFood;

        public override void UpdateDescription()
        {
            this.description = ChallengeTools.IGT.Translate("Never " + (isFood ? "eat" : "use") + " <item>").Replace("<item>", ChallengeTools.ItemName(item));
            base.UpdateDescription();
        }

        public override bool Duplicable(Challenge challenge)
        {
            return challenge is not BingoDontUseItemChallenge c || c.item != item;
        }

        public override string ChallengeName()
        {
            return ChallengeTools.IGT.Translate("Restraint");
        }

        public override Challenge Generate()
        {
            bool edible = UnityEngine.Random.value < 0.5f;
            AbstractPhysicalObject.AbstractObjectType type = ChallengeUtils.Bannable[UnityEngine.Random.Range(edible ? 6 : 0, edible ? ChallengeUtils.Bannable.Length : 6)];

            return new BingoDontUseItemChallenge
            {
                item = type,
                isFood = edible,
                completed = true
            };
        }

        public void Used(AbstractPhysicalObject.AbstractObjectType used)
        {
            if (used == item)
            {
                completed = false;
                //
            }
        }

        public override void Update()
        {
            base.Update();

            if (isFood) return;
            for (int i = 0; i < BingoData.heldItemsTime.Length; i++)
            {
                if (i == (int)item && BingoData.heldItemsTime[i] > 200) Used(item); 
            }
        }

        public override int Points()
        {
            return 20;
        }

        public override bool CombatRequired()
        {
            return false;
        }

        public override bool ValidForThisSlugcat(SlugcatStats.Name slugcat)
        {
            return true;
        }

        public override string ToString()
        {
            return string.Concat(new string[]
            {
                "Restraint",
                "~",
                ValueConverter.ConvertToString(item),
                "><",
                isFood ? "1" : "0",
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
                item = new(array[0], false);
                isFood = (array[1] == "1");
                completed = (array[2] == "1");
                hidden = (array[3] == "1");
                revealed = (array[4] == "1");
                UpdateDescription();
            }
            catch (Exception ex)
            {
                ExpLog.Log("ERROR: Restraint FromString() encountered an error: " + ex.Message);
            }
        }

        public void AddHooks()
        {
            On.Player.ThrowObject += Player_ThrowObject;
            On.Player.GrabUpdate += Player_GrabUpdate;
        }

        public void RemoveHooks()
        {
            On.Player.ThrowObject -= Player_ThrowObject;
            On.Player.GrabUpdate -= Player_GrabUpdate;
        }
    }
}