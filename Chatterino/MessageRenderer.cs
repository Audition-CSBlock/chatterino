﻿using Chatterino.Common;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chatterino
{
    public static class MessageRenderer
    {
        public static SharpDX.Direct2D1.Factory D2D1Factory = new SharpDX.Direct2D1.Factory();
        public static SharpDX.Direct2D1.RenderTargetProperties RenderTargetProperties = new SharpDX.Direct2D1.RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied)) { Usage = SharpDX.Direct2D1.RenderTargetUsage.GdiCompatible };

        static Brush selectionBrush = new SolidBrush(Color.FromArgb(127, Color.Orange));

        public static void DrawMessage(object graphics, Common.Message message, int xOffset, int yOffset, Selection selection, int currentLine, bool drawText)
        {
            message.X = xOffset;
            message.Y = yOffset;

            Graphics g = (Graphics)graphics;

            int spaceWidth = TextRenderer.MeasureText(g, " ", Fonts.GdiMedium, Size.Empty, App.DefaultTextFormatFlags).Width;

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            message.X = xOffset;
            var textColor = App.ColorScheme.Text;

            if (message.Highlighted)
                g.FillRectangle(App.ColorScheme.ChatBackgroundHighlighted, 0, yOffset, g.ClipBounds.Width, message.Height);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;

            for (int i = 0; i < message.Words.Count; i++)
            {
                var word = message.Words[i];

                if (word.Type == SpanType.Text)
                {
                    if (drawText)
                    {
                        Font font = Fonts.GetFont(word.Font);

                        Color color = word.Color == null ? textColor : Color.FromArgb(word.Color.Value);
                        HSLColor hsl = new HSLColor(color);

                        if (App.ColorScheme.IsLightTheme)
                        {
                            if (hsl.Luminosity > 170)
                                hsl.Luminosity = 170;
                        }
                        else
                        {
                            if (hsl.Luminosity < 170)
                                hsl.Luminosity = 170;
                        }

                        color = hsl;

                        if (word.SplitSegments == null)
                        {
                            TextRenderer.DrawText(g, (string)word.Value, font, new Point(xOffset + word.X, yOffset + word.Y), color, App.DefaultTextFormatFlags);
                        }
                        else
                        {
                            var segments = word.SplitSegments;
                            for (int x = 0; x < segments.Length; x++)
                            {
                                TextRenderer.DrawText(g, segments[x].Item1, font, new Point(xOffset + segments[x].Item2.X, yOffset + segments[x].Item2.Y), color, App.DefaultTextFormatFlags);
                            }
                        }
                    }
                }
                else if (word.Type == SpanType.Emote)
                {
                    var emote = (TwitchEmote)word.Value;
                    var img = (Image)emote.Image;
                    if (img != null)
                    {
                        lock (img)
                        {
                            g.DrawImage(img, word.X + xOffset, word.Y + yOffset, word.Width, word.Height);
                        }
                    }
                    else
                    {
                        //g.DrawRectangle(Pens.Red, xOffset + word.X, word.Y + yOffset, word.Width, word.Height);
                    }
                }
                else if (word.Type == SpanType.Image)
                {
                    var img = (Image)word.Value;
                    if (img != null)
                        g.DrawImage(img, word.X + xOffset, word.Y + yOffset, word.Width, word.Height);
                }
            }

            if (selection != null && !selection.IsEmpty && selection.First.MessageIndex <= currentLine && selection.Last.MessageIndex >= currentLine)
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                var first = selection.First;
                var last = selection.Last;

                for (int i = 0; i < message.Words.Count; i++)
                {
                    if ((currentLine != first.MessageIndex || i >= first.WordIndex) && (currentLine != last.MessageIndex || i <= last.WordIndex))
                    {
                        var word = message.Words[i];

                        if (word.Type == SpanType.Text)
                        {
                            for (int j = 0; j < (word.SplitSegments?.Length ?? 1); j++)
                            {
                                if ((first.MessageIndex == currentLine && first.WordIndex == i && first.SplitIndex > j) || (last.MessageIndex == currentLine && last.WordIndex == i && last.SplitIndex < j))
                                    continue;

                                var split = word.SplitSegments?[j];
                                string text = split?.Item1 ?? (string)word.Value;
                                CommonRectangle rect = split?.Item2 ?? new CommonRectangle(word.X, word.Y, word.Width, word.Height);

                                int textLength = text.Length;

                                int offset = (first.MessageIndex == currentLine && first.SplitIndex == j && first.WordIndex == i) ? first.CharIndex : 0;
                                int length = ((last.MessageIndex == currentLine && last.SplitIndex == j && last.WordIndex == i) ? last.CharIndex : textLength) - offset;

                                if (offset == 0 && length == text.Length)
                                    g.FillRectangle(selectionBrush, rect.X + xOffset, rect.Y + yOffset, GuiEngine.Current.MeasureStringSize(App.UseDirectX ? null : g, word.Font, text).Width + spaceWidth, rect.Height);
                                else if (offset == text.Length)
                                    g.FillRectangle(selectionBrush, rect.X + xOffset + rect.Width, rect.Y + yOffset, spaceWidth, rect.Height);
                                else
                                    g.FillRectangle(selectionBrush,
                                        rect.X + xOffset + (offset == 0 ? 0 : GuiEngine.Current.MeasureStringSize(App.UseDirectX ? null : g, word.Font, text.Remove(offset)).Width),
                                        rect.Y + yOffset,
                                        GuiEngine.Current.MeasureStringSize(App.UseDirectX ? null : g, word.Font, text.Substring(offset, length)).Width +
                                        ((last.MessageIndex > currentLine || last.SplitIndex > j || last.WordIndex > i) ? spaceWidth : 0),
                                        rect.Height);
                            }
                        }
                        else if (word.Type == SpanType.Image)
                        {
                            int textLength = 2;

                            int offset = (first.MessageIndex == currentLine && first.WordIndex == i) ? first.CharIndex : 0;
                            int length = ((last.MessageIndex == currentLine && last.WordIndex == i) ? last.CharIndex : textLength) - offset;

                            g.FillRectangle(selectionBrush, word.X + xOffset + (offset == 0 ? 0 : word.Width), word.Y + yOffset, (offset == 0 ? word.Width : 0) + (offset + length == 2 ? spaceWidth : 0), word.Height);
                        }
                        else if (word.Type == SpanType.Emote)
                        {
                            int textLength = 2;

                            int offset = (first.MessageIndex == currentLine && first.WordIndex == i) ? first.CharIndex : 0;
                            int length = ((last.MessageIndex == currentLine && last.WordIndex == i) ? last.CharIndex : textLength) - offset;

                            if (!((TwitchEmote)word.Value).Animated)
                                g.FillRectangle(selectionBrush, word.X + xOffset, word.Y + yOffset, word.Width + spaceWidth, word.Height);
                        }
                    }
                }
            }
        }

        public static void DrawGifEmotes(object graphics, Common.Message message, Selection selection, int currentLine)
        {
            var Words = message.Words;
            Graphics g = (Graphics)graphics;

            int spaceWidth = TextRenderer.MeasureText(g, " ", Fonts.GdiMedium, Size.Empty, App.DefaultTextFormatFlags).Width;

            for (int i = 0; i < Words.Count; i++)
            {
                var word = Words[i];

                TwitchEmote emote;
                if (word.Type == SpanType.Emote && (emote = (TwitchEmote)word.Value).Animated)
                {
                    if (emote.Image != null)
                    {
                        lock (emote.Image)
                        {
                            var CurrentXOffset = message.X;
                            var CurrentYOffset = message.Y;

                            g.FillRectangle(message.Highlighted ? App.ColorScheme.ChatBackgroundHighlighted : App.ColorScheme.ChatBackground, word.X + CurrentXOffset, word.Y + CurrentYOffset, word.Width, word.Height);
                            g.DrawImage((Image)emote.Image, word.X + CurrentXOffset, word.Y + CurrentYOffset, word.Width, word.Height);

                            //if (message.Highlighted)
                            //    g.FillRectangle(, word.X + CurrentXOffset, word.Y + CurrentYOffset, word.Width, word.Height);

                            if (selection != null && !selection.IsEmpty && (currentLine > selection.First.MessageIndex || (currentLine == selection.First.MessageIndex && i >= selection.First.WordIndex)) && (currentLine < selection.Last.MessageIndex || (selection.Last.MessageIndex == currentLine && i < selection.Last.WordIndex)))
                                g.FillRectangle(selectionBrush, word.X + CurrentXOffset, word.Y + CurrentYOffset, word.Width, word.Height);

                            if (message.Disabled)
                            {
                                g.FillRectangle(new SolidBrush(Color.FromArgb(172, (App.ColorScheme.ChatBackground as SolidBrush)?.Color ?? Color.Black)),
                                    word.X + CurrentXOffset, word.Y + CurrentYOffset, word.Width + spaceWidth, word.Height);
                            }
                        }
                    }
                }
            }
        }
    }
}
