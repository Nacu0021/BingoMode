﻿using Expedition;
using Menu.Remix;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BingoMode.Challenges
{
    using static ChallengeHooks;
    public class BingoVistaChallenge : Challenge, IBingoChallenge
    {
        public SettingBox<string> room;
        public string region;
        public Vector2 location;
        public int Index { get; set; }
        public bool RequireSave { get; set; }
        public bool Failed { get; set; }

        public override void Update()
        {
            base.Update();
            if (!this.completed)
            {
                for (int i = 0; i < this.game.Players.Count; i++)
                {
                    if (this.game.Players[i].realizedCreature != null && this.game.Players[i].realizedCreature.room != null && this.game.Players[i].realizedCreature.room.abstractRoom.name == this.room.Value && Vector2.Distance(this.game.Players[i].realizedCreature.mainBodyChunk.pos, this.location) < 30f)
                    {
                        this.CompleteChallenge();
                    }
                }
                if (this.game.world != null && this.game.world.activeRooms != null)
                {
                    for (int j = 0; j < this.game.world.activeRooms.Count; j++)
                    {
                        if (this.game.world.activeRooms[j].abstractRoom.name == this.room.Value)
                        {
                            for (int k = 0; k < this.game.world.activeRooms[j].updateList.Count; k++)
                            {
                                if (this.game.world.activeRooms[j].updateList[k] is BingoVistaChallenge.BingoVistaPoint)
                                {
                                    return;
                                }
                            }
                            ExpLog.Log("SPAWN BVISTA");
                            this.game.world.activeRooms[j].AddObject(new BingoVistaChallenge.BingoVistaPoint(this.game.world.activeRooms[j], this, this.location));
                            return;
                        }
                    }
                }
            }
        }

        public override string ChallengeName()
        {
            return ChallengeTools.IGT.Translate("Vista Visiting");
        }

        public override void UpdateDescription()
        {
            this.description = ChallengeTools.IGT.Translate("Reach the vista point in <region_name>").Replace("<region_name>", ChallengeTools.IGT.Translate(Region.GetRegionFullName(this.region, ExpeditionData.slugcatPlayer)));
            base.UpdateDescription();
        }

        public override bool Duplicable(Challenge challenge)
        {
            return !(challenge is BingoVistaChallenge) || (challenge as BingoVistaChallenge).region != this.region;
        }

        public override Challenge Generate()
        {
            List<ValueTuple<string, string>> list = new List<ValueTuple<string, string>>();
            foreach (KeyValuePair<string, Dictionary<string, Vector2>> keyValuePair in ChallengeTools.VistaLocations)
            {
                if (SlugcatStats.SlugcatStoryRegions(ExpeditionData.slugcatPlayer).Contains(keyValuePair.Key))
                {
                    foreach (KeyValuePair<string, Vector2> keyValuePair2 in keyValuePair.Value)
                    {
                        list.Add(new ValueTuple<string, string>(keyValuePair.Key, keyValuePair2.Key));
                    }
                }
            }
            ValueTuple<string, string> valueTuple = list[UnityEngine.Random.Range(0, list.Count)];
            string item = valueTuple.Item1;
            string item2 = valueTuple.Item2;
            Vector2 vector = ChallengeTools.VistaLocations[item][item2];
            BingoVistaChallenge vistaChallenge = new BingoVistaChallenge
            {
                region = item,
                room = new(item2, "Room", 0, listName: "vista"),
                location = vector
            };
            ModifyVistaCandidates(vistaChallenge);
            return vistaChallenge;
        }

        public static void ModifyVistaCandidates(BingoVistaChallenge input)
        {
            if (input.room.Value == "GW_E02" && ModManager.MSC && (ExpeditionData.slugcatPlayer == MoreSlugcatsEnums.SlugcatStatsName.Artificer || ExpeditionData.slugcatPlayer == MoreSlugcatsEnums.SlugcatStatsName.Spear))
            {
                input.room.Value = "GW_E02_PAST";
                ExpLog.Log("Switch room to past version");
                return;
            }
            if (input.room.Value == "GW_D01" && ModManager.MSC && (ExpeditionData.slugcatPlayer == MoreSlugcatsEnums.SlugcatStatsName.Artificer || ExpeditionData.slugcatPlayer == MoreSlugcatsEnums.SlugcatStatsName.Spear))
            {
                input.room.Value = "GW_D01_PAST";
                ExpLog.Log("Switch room to past version");
                return;
            }
            if (input.room.Value == "UW_C02" && ModManager.MSC && ExpeditionData.slugcatPlayer == MoreSlugcatsEnums.SlugcatStatsName.Rivulet)
            {
                input.room.Value = "UW_C02RIV";
                input.location = new Vector2(450f, 1170f);
                ExpLog.Log("Switch room to future version");
            }
        }

        public override int Points()
        {
            return 40;
        }

        public override bool CombatRequired()
        {
            return false;
        }

        public override string ToString()
        {
            return string.Concat(new string[]
            {
                "BingoVistaChallenge",
                "~",
                this.region,
                "><",
                this.room.ToString(),
                "><",
                ValueConverter.ConvertToString<float>(this.location.x),
                "><",
                ValueConverter.ConvertToString<float>(this.location.y),
                "><",
                this.completed ? "1" : "0",
                "><",
                this.hidden ? "1" : "0",
                "><",
                this.revealed ? "1" : "0"
            });
        }

        public override void FromString(string args)
        {
            try
            {
                string[] array = Regex.Split(args, "><");
                this.region = array[0];
                this.room = SettingBoxFromString(array[1]) as SettingBox<string>;
                this.location = default(Vector2);
                this.location.x = float.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                this.location.y = float.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                this.completed = (array[4] == "1");
                this.hidden = (array[5] == "1");
                this.revealed = (array[6] == "1");
                this.UpdateDescription();
            }
            catch (Exception ex)
            {
                ExpLog.Log("ERROR: BingoVistaChallenge FromString() encountered an error: " + ex.Message);
                throw ex;
            }
        }

        public void AddHooks()
        {
        }

        public void RemoveHooks()
        {
        }

        public List<object> Settings() => [room];

        public class BingoVistaPoint : UpdatableAndDeletable, IDrawable
        {
            public BingoVistaPoint(Room room, BingoVistaChallenge vista, Vector2 inRoomPos)
            {
                this.vista = vista;
                this.room = room;
                this.inRoomPos = inRoomPos;
                this.notify = false;
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[3]);
                rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[0]);
                rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[1]);
                rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[2]);
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                sLeaser.sprites[0].x = this.inRoomPos.x - rCam.pos.x;
                sLeaser.sprites[0].y = this.inRoomPos.y - rCam.pos.y;
                sLeaser.sprites[1].x = this.inRoomPos.x - rCam.pos.x;
                sLeaser.sprites[1].y = this.inRoomPos.y - rCam.pos.y;
                sLeaser.sprites[2].x = this.inRoomPos.x - rCam.pos.x;
                sLeaser.sprites[2].y = this.inRoomPos.y - rCam.pos.y;
                sLeaser.sprites[3].x = this.inRoomPos.x - rCam.pos.x;
                sLeaser.sprites[3].y = this.inRoomPos.y - rCam.pos.y;
                if (this.vista != null && this.vista.completed)
                {
                    sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Basic"];
                    sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders["Basic"];
                    sLeaser.sprites[0].alpha = 0f;
                    sLeaser.sprites[1].alpha = 0f;
                    sLeaser.sprites[2].alpha = 0f;
                    sLeaser.sprites[3].alpha = 0f;
                }
                this.phase += 0.13f * Time.deltaTime;
                if (this.phase > 1f)
                {
                    this.phase = 0f;
                }
                this.color = new HSLColor(this.phase, 0.85f, 0.75f).rgb;
                sLeaser.sprites[0].scaleX = Mathf.Sin(this.time / 20f);
                sLeaser.sprites[1].scaleX = Mathf.Sin(this.time / 20f) * 1.3f;
                sLeaser.sprites[0].y = this.inRoomPos.y - rCam.pos.y + 3f * Mathf.Sin(this.time / 20f);
                sLeaser.sprites[1].y = this.inRoomPos.y - rCam.pos.y + 3f * Mathf.Sin(this.time / 20f);
                sLeaser.sprites[0].color = this.color;
                sLeaser.sprites[1].color = this.color;
                sLeaser.sprites[2].color = this.color;
                sLeaser.sprites[3].color = new Color(0.01f, 0.01f, 0.01f);
                if (this.lightSource != null)
                {
                    this.lightSource.color = this.color;
                }
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[4];
                sLeaser.sprites[0] = new FSprite("TravellerB", true);
                sLeaser.sprites[0].x = this.inRoomPos.x - rCam.pos.x;
                sLeaser.sprites[0].y = this.inRoomPos.y - rCam.pos.y;
                sLeaser.sprites[0].scaleX = 1.25f;
                sLeaser.sprites[0].scaleY = 1.25f;
                sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["GateHologram"];
                sLeaser.sprites[0].alpha = 0.85f;
                sLeaser.sprites[1] = new FSprite("TravellerB", true);
                sLeaser.sprites[1].x = this.inRoomPos.x - rCam.pos.x;
                sLeaser.sprites[1].y = this.inRoomPos.y - rCam.pos.y;
                sLeaser.sprites[1].scaleX = 1.55f;
                sLeaser.sprites[1].scaleY = 1.55f;
                sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders["GateHologram"];
                sLeaser.sprites[1].alpha = 0.35f;
                sLeaser.sprites[2] = new FSprite("Futile_White", true);
                sLeaser.sprites[2].x = this.inRoomPos.x - rCam.pos.x;
                sLeaser.sprites[2].y = this.inRoomPos.y - rCam.pos.y;
                sLeaser.sprites[2].scaleX = 15f;
                sLeaser.sprites[2].scaleY = 15f;
                sLeaser.sprites[2].shader = rCam.game.rainWorld.Shaders["FlatLight"];
                sLeaser.sprites[2].alpha = 0.35f;
                sLeaser.sprites[3] = new FSprite("Futile_White", true);
                sLeaser.sprites[3].x = this.inRoomPos.x - rCam.pos.x;
                sLeaser.sprites[3].y = this.inRoomPos.y - rCam.pos.y;
                sLeaser.sprites[3].scaleX = 6f;
                sLeaser.sprites[3].scaleY = 6f;
                sLeaser.sprites[3].shader = rCam.game.rainWorld.Shaders["FlatLight"];
                sLeaser.sprites[3].alpha = 0.3f;
                this.AddToContainer(sLeaser, rCam, null);
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                this.lastTime = this.time;
                this.time += 1f;
                if (this.room.BeingViewed && !this.notify)
                {
                    this.room.game.cameras[0].hud.textPrompt.AddMessage(ChallengeTools.IGT.Translate("You feel the presence of a vista . . ."), 20, 150, true, true);
                    this.notify = true;
                }
                if (this.lightSource == null)
                {
                    this.lightSource = new LightSource(this.inRoomPos, false, new Color(1f, 0.85f, 0.2f), this);
                    this.lightSource.setRad = new float?(130f);
                    this.lightSource.setAlpha = new float?(1f);
                    this.room.AddObject(this.lightSource);
                }
                if (this.vista.completed && !this.collected)
                {
                    this.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, this.inRoomPos, 1f, 1f);
                    for (int i = 0; i < 20; i++)
                    {
                        this.room.AddObject(new Spark(this.inRoomPos, Custom.RNV() * (25f * UnityEngine.Random.value), this.color, null, 70, 150));
                    }
                    this.collected = true;
                    return;
                }
                if (this.vista.completed && this.collected)
                {
                    base.RemoveFromRoom();
                    this.Destroy();
                }
            }

            public Vector2 inRoomPos;

            public BingoVistaChallenge vista;

            public LightSource lightSource;

            public Color color;

            public bool collected;

            public bool notify;

            public float phase;

            public float time;

            public float lastTime;
        }
    }
}
