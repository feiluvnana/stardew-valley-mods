using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace BetterQOL
{
    public class LookupMenu : IClickableMenu
    {
        private readonly LookupSubject Subject;
        private readonly List<ClickableComponent> Components = new();

        private ClickableTextureComponent? CloseButton;
        private ClickableTextureComponent? UpButton;
        private ClickableTextureComponent? DownButton;

        private int ScrollOffset = 0;
        private int MaxScrollOffset = 0;
        private const int ScrollStep = 36;

        public LookupMenu(LookupSubject subject)
            : base(
                x: (Game1.uiViewport.Width - Math.Min(760, Game1.uiViewport.Width - 48)) / 2,
                y: (Game1.uiViewport.Height - Math.Min(600, Game1.uiViewport.Height - 48)) / 2,
                width: Math.Min(760, Game1.uiViewport.Width - 48),
                height: Math.Min(600, Game1.uiViewport.Height - 48),
                showUpperRightCloseButton: true
            )
        {
            Subject = subject;
            Game1.playSound("bigSelect");

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Components.Clear();

            // Close button
            CloseButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width - 36, yPositionOnScreen - 8, 48, 48),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );
            Components.Add(CloseButton);

            // Up / Down Scroll Buttons
            UpButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width - 32, yPositionOnScreen + 104, 44, 48),
                Game1.mouseCursors,
                new Rectangle(421, 459, 11, 12),
                4f
            );
            Components.Add(UpButton);

            DownButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width - 32, yPositionOnScreen + height - 60, 44, 48),
                Game1.mouseCursors,
                new Rectangle(421, 472, 11, 12),
                4f
            );
            Components.Add(DownButton);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (CloseButton != null && CloseButton.containsPoint(x, y))
            {
                Game1.playSound("bigDeSelect");
                exitThisMenu();
                return;
            }

            if (UpButton != null && UpButton.containsPoint(x, y))
            {
                ScrollOffset = Math.Max(0, ScrollOffset - ScrollStep * 2);
                Game1.playSound("shwip");
                return;
            }

            if (DownButton != null && DownButton.containsPoint(x, y))
            {
                ScrollOffset = Math.Min(MaxScrollOffset, ScrollOffset + ScrollStep * 2);
                Game1.playSound("shwip");
                return;
            }

            // Click outside bounds closes menu
            if (!new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height).Contains(x, y))
            {
                Game1.playSound("bigDeSelect");
                exitThisMenu();
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            Game1.playSound("bigDeSelect");
            exitThisMenu();
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (direction > 0)
            {
                ScrollOffset = Math.Max(0, ScrollOffset - ScrollStep);
                Game1.playSound("shiny4");
            }
            else if (direction < 0)
            {
                ScrollOffset = Math.Min(MaxScrollOffset, ScrollOffset + ScrollStep);
                Game1.playSound("shiny4");
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if (key == Keys.Escape || (key == (Keys)ModEntry.Config.LookupKey))
            {
                Game1.playSound("bigDeSelect");
                exitThisMenu();
                return;
            }

            if (key == Keys.Up || key == Keys.W)
            {
                ScrollOffset = Math.Max(0, ScrollOffset - ScrollStep);
            }
            else if (key == Keys.Down || key == Keys.S)
            {
                ScrollOffset = Math.Min(MaxScrollOffset, ScrollOffset + ScrollStep);
            }
        }

        public override void draw(SpriteBatch b)
        {
            // Dark background overlay
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);

            // Main Parchment Box
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                xPositionOnScreen,
                yPositionOnScreen,
                width,
                height,
                Color.White,
                1f,
                drawShadow: true
            );

            // 1. Draw Header
            int headerX = xPositionOnScreen + 32;
            int headerY = yPositionOnScreen + 28;
            int textStartX = headerX;

            // Portrait or Icon
            if (Subject.Portrait != null)
            {
                Rectangle src = Subject.PortraitSourceRect ?? new Rectangle(0, 0, 64, 64);
                b.Draw(Subject.Portrait, new Rectangle(headerX, headerY, 64, 64), src, Color.White);
                textStartX += 76;
            }
            else if (Subject.MainIcon != null)
            {
                Rectangle src = Subject.MainIconSourceRect ?? new Rectangle(0, 0, Subject.MainIcon.Width, Subject.MainIcon.Height);
                b.Draw(Subject.MainIcon, new Rectangle(headerX, headerY, 56, 56), src, Color.White);
                textStartX += 68;
            }

            // Title
            Utility.drawTextWithShadow(b, Subject.Title, Game1.dialogueFont, new Vector2(textStartX, headerY - 4), Game1.textColor);

            // Subtitle
            if (!string.IsNullOrEmpty(Subject.Subtitle))
            {
                Utility.drawTextWithShadow(b, Subject.Subtitle, Game1.smallFont, new Vector2(textStartX, headerY + 36), Color.DimGray);
            }

            // Header Divider Line
            int dividerY = yPositionOnScreen + 104;
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 24, dividerY, width - 48, 2), Color.SaddleBrown * 0.35f);

            // 2. Scrollable Body
            int contentX = xPositionOnScreen + 36;
            int contentY = dividerY + 14;
            int contentWidth = width - 88;
            int contentHeight = height - 134;
            int contentBottom = contentY + contentHeight;

            int currentY = contentY - ScrollOffset;
            int totalContentHeight = 0;

            foreach (var section in Subject.Sections)
            {
                // Section Title
                if (currentY >= contentY - 30 && currentY <= contentBottom)
                {
                    Utility.drawTextWithShadow(b, section.Title, Game1.dialogueFont, new Vector2(contentX, currentY), new Color(115, 40, 10));
                }
                currentY += 34;
                totalContentHeight += 34;

                // Section Fields
                foreach (var field in section.Fields)
                {
                    string label = !string.IsNullOrEmpty(field.Label) ? $"{field.Label}: " : string.Empty;
                    Vector2 labelSize = Game1.smallFont.MeasureString(label);

                    int valWidth = contentWidth - (int)labelSize.X - 24;
                    string wrappedValue = Game1.parseText(field.Value, Game1.smallFont, Math.Max(180, valWidth));
                    Vector2 valSize = Game1.smallFont.MeasureString(wrappedValue);
                    int lineH = (int)Math.Max(26, valSize.Y + 4);

                    if (currentY >= contentY - lineH && currentY <= contentBottom)
                    {
                        Utility.drawTextWithShadow(b, label, Game1.smallFont, new Vector2(contentX + 12, currentY), Game1.textColor);
                        Utility.drawTextWithShadow(b, wrappedValue, Game1.smallFont, new Vector2(contentX + 12 + labelSize.X, currentY), field.ValueColor);
                    }

                    currentY += lineH;
                    totalContentHeight += lineH;
                }

                currentY += 12;
                totalContentHeight += 12;
            }

            MaxScrollOffset = Math.Max(0, totalContentHeight - contentHeight + 20);

            // 3. Draw Controls
            CloseButton?.draw(b);

            if (MaxScrollOffset > 0)
            {
                UpButton?.draw(b);
                DownButton?.draw(b);

                // Scrollbar track & thumb
                int trackX = xPositionOnScreen + width - 20;
                int trackY = yPositionOnScreen + 154;
                int trackH = height - 224;

                b.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, 6, trackH), Color.SaddleBrown * 0.2f);

                float scrollPct = (float)ScrollOffset / MaxScrollOffset;
                int thumbY = trackY + (int)((trackH - 24) * scrollPct);
                b.Draw(Game1.staminaRect, new Rectangle(trackX - 1, thumbY, 8, 24), Color.SaddleBrown * 0.7f);
            }

            drawMouse(b);
        }
    }
}
