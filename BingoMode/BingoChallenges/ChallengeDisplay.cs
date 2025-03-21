﻿using UnityEngine;
using RWCustom;
using System.Collections.Generic;

namespace BingoMode.BingoChallenges
{
    public class Phrase
    {
        public List<Word> words;
        public Vector2 centerPos;
        public float scale;
        public int[] newLines;
        public bool applyScale;

        public Phrase(List<Word> words, int[] newLines)
        {
            this.words = words;
            this.newLines = newLines;
        }

        public void Draw()
        {
            float horizontalDist = 27.5f * scale;
            float verticalDist = 27.5f * scale;
            int iconAmount = words.Count;
            Vector2[] positiones = new Vector2[iconAmount];
            int distToNewLine = newLines.Length == 0 ? (iconAmount - 1) : (newLines[0] - 1);
            Vector2 startPos = centerPos + new Vector2(distToNewLine * 0.5f * -horizontalDist, newLines.Length * 0.5f * verticalDist) + new Vector2(0.01f, 0.01f);
            float carryOver = 0f;
            for (int i = 0; i < iconAmount; i++)
            {
                positiones[i] = startPos;
                if (newLines.Length > 0)
                {
                    bool resetCarry = false;
                    for (int n = 0; n < newLines.Length; n++)
                    {
                        if (i >= newLines[n])
                        {
                            Vector2 downThingIdk = new Vector2(0f, verticalDist);
                            positiones[i] -= downThingIdk;
                        }
                        if (i + 1 == newLines[n])
                        {
                            int gruh = (n + 1) > (newLines.Length - 1) ? (iconAmount - newLines[n] - 1) : (newLines[n + 1] - newLines[n] - 1);
                            startPos = centerPos + new Vector2(gruh * 0.5f * -horizontalDist, newLines.Length * 0.5f * verticalDist);
                            resetCarry = true;
                        }
                    }
                    positiones[i] += new Vector2(carryOver, 0f);
                    carryOver += horizontalDist;
                    if (resetCarry) carryOver = 0f;
                    words[i].display.SetPosition(positiones[i]);
                    if (words[i].background != null) words[i].background.SetPosition(positiones[i]);
                    if (applyScale) words[i].display.scale = scale;
                    continue;
                }
                positiones[i] += new Vector2(carryOver, 0f);
                carryOver += horizontalDist;
                words[i].display.SetPosition(positiones[i]);
                if (words[i].background != null) words[i].background.SetPosition(positiones[i]);
                if (applyScale) words[i].display.scale = scale;
            }
        }

        public void AddAll(FContainer container)
        {
            foreach (Word word in words)
            {
                if (word.background != null) container.AddChild(word.background);
                container.AddChild(word.display);
            }
        }

        public void ClearAll()
        {
            foreach (Word word in words)
            {
                word.background?.RemoveFromContainer();
                word.display.RemoveFromContainer();
            }
        }

        public void SetAlpha(float alpha)
        {
            foreach (Word word in words)
            {
                if (word.background != null) word.background.alpha = alpha;
                word.display.alpha = alpha;
            }
        }

        public static Phrase LockPhrase()
        {
            return new Phrase([new Icon("bingolock", 1f, new Color(0.01f, 0.01f, 0.01f))], []);
        }
    }

    public abstract class Word
    {
        public FNode display;
        public FNode background;

        public Word()
        {
        }
    }

    public class Counter : Word
    {
        public Counter(int current, int max) : base()
        {
            display = new FLabel(Custom.GetFont(), $"[{current}/{max}]");
        }
    }

    public class Verse : Word
    {
        public Verse(string text) : base()
        {
            display = new FLabel(Custom.GetFont(), text);
        }
    }

    public class Icon : Word
    {
        public Icon(string element, float scale, Color color, float rotation = 0f) : base()
        {
            display = new FSprite(element)
            {
                scale = scale,
                color = color,
                rotation = rotation
            };
        }
    }
}
