using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace BetterQOL
{
    public class LookupMenu : IClickableMenu
    {
        private LookupSubject? CurrentSubject;
        private readonly Stack<LookupSubject> History = new();

        private ClickableTextureComponent? CloseButton;
        private ClickableTextureComponent? BackButton;
        private ClickableTextureComponent? UpButton;
        private ClickableTextureComponent? DownButton;

        private TextBox? SearchBox;
        private ClickableComponent? SearchBoxComponent;
        private string LastSearchText = string.Empty;
        private List<LookupLink> SearchResults = new();

        private int ScrollOffset = 0;
        private int MaxScrollOffset = 0;
        private const int ScrollStep = 40;

        private readonly List<LookupLink> ActiveClickableLinks = new();
        private LookupLink? HoveredLink = null;

        private static readonly RasterizerState ScissorRasterizer = new() { ScissorTestEnable = true };

        public LookupMenu(LookupSubject? initialSubject = null)
            : base(
                x: (Game1.uiViewport.Width - Math.Min(840, Game1.uiViewport.Width - 64)) / 2,
                y: (Game1.uiViewport.Height - Math.Min(660, Game1.uiViewport.Height - 64)) / 2,
                width: Math.Min(840, Game1.uiViewport.Width - 64),
                height: Math.Min(660, Game1.uiViewport.Height - 64),
                showUpperRightCloseButton: true
            )
        {
            CurrentSubject = initialSubject;
            Game1.playSound("bigSelect");

            InitializeComponents();

            if (CurrentSubject == null && SearchBox != null)
            {
                SearchBox.Selected = true;
                Game1.keyboardDispatcher.Subscriber = SearchBox;
            }
        }

        private void InitializeComponents()
        {
            // 1. Close Button (top-right, nicely tucked inside frame)
            CloseButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width - 40, yPositionOnScreen - 6, 48, 48),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );

            // 2. Back Button (top-left)
            BackButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + 30, yPositionOnScreen + 28, 44, 44),
                Game1.mouseCursors,
                new Rectangle(352, 495, 12, 11),
                3.5f
            );

            // 3. Search Box (top-right header)
            int searchBoxW = 230;
            int searchBoxH = 42;
            int searchBoxX = xPositionOnScreen + width - searchBoxW - 58;
            int searchBoxY = yPositionOnScreen + 26;

            SearchBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor
            )
            {
                X = searchBoxX,
                Y = searchBoxY,
                Width = searchBoxW,
                Height = searchBoxH
            };
            SearchBoxComponent = new ClickableComponent(new Rectangle(searchBoxX, searchBoxY, searchBoxW, searchBoxH), "SearchBox");

            // 4. Scroll Buttons (well inside right parchment margin)
            int contentY = yPositionOnScreen + 104;
            int contentHeight = height - 152;
            int contentBottom = contentY + contentHeight;

            int btnX = xPositionOnScreen + width - 64;

            UpButton = new ClickableTextureComponent(
                new Rectangle(btnX, contentY, 36, 40),
                Game1.mouseCursors,
                new Rectangle(421, 459, 11, 12),
                3.2f
            );

            DownButton = new ClickableTextureComponent(
                new Rectangle(btnX, contentBottom - 40, 36, 40),
                Game1.mouseCursors,
                new Rectangle(421, 472, 11, 12),
                3.2f
            );
        }

        public void NavigateTo(LookupSubject subject)
        {
            if (CurrentSubject != null)
            {
                History.Push(CurrentSubject);
            }
            CurrentSubject = subject;
            ScrollOffset = 0;
            if (SearchBox != null)
            {
                SearchBox.Text = string.Empty;
                SearchBox.Selected = false;
                Game1.keyboardDispatcher.Subscriber = null;
            }
            SearchResults.Clear();
            Game1.playSound("smallSelect");
        }

        public void NavigateBack()
        {
            if (History.Count > 0)
            {
                CurrentSubject = History.Pop();
                ScrollOffset = 0;
                Game1.playSound("smallSelect");
            }
            else if (CurrentSubject != null)
            {
                CurrentSubject = null;
                ScrollOffset = 0;
                if (SearchBox != null)
                {
                    SearchBox.Selected = true;
                    Game1.keyboardDispatcher.Subscriber = SearchBox;
                }
                Game1.playSound("smallSelect");
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);

            if (SearchBox != null)
            {
                SearchBox.Update();
                if (SearchBox.Text != LastSearchText)
                {
                    LastSearchText = SearchBox.Text;
                    ScrollOffset = 0;
                    if (!string.IsNullOrWhiteSpace(LastSearchText))
                    {
                        SearchResults = LookupDataManager.SearchAll(LastSearchText);
                    }
                    else
                    {
                        SearchResults.Clear();
                    }
                }
            }
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);

            CloseButton?.tryHover(x, y, 0.2f);
            BackButton?.tryHover(x, y, 0.2f);
            UpButton?.tryHover(x, y, 0.2f);
            DownButton?.tryHover(x, y, 0.2f);

            HoveredLink = null;
            foreach (var link in ActiveClickableLinks)
            {
                if (link.Bounds.Contains(x, y))
                {
                    HoveredLink = link;
                    break;
                }
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (CloseButton != null && CloseButton.containsPoint(x, y))
            {
                CloseMenu();
                return;
            }

            if ((History.Count > 0 || (CurrentSubject != null && !string.IsNullOrEmpty(LastSearchText))) && BackButton != null && BackButton.containsPoint(x, y))
            {
                NavigateBack();
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

            // Click Search Box
            if (SearchBoxComponent != null && SearchBoxComponent.containsPoint(x, y))
            {
                if (SearchBox != null)
                {
                    SearchBox.Selected = true;
                    Game1.keyboardDispatcher.Subscriber = SearchBox;
                }
                return;
            }
            else
            {
                if (SearchBox != null && SearchBox.Selected)
                {
                    SearchBox.Selected = false;
                    Game1.keyboardDispatcher.Subscriber = null;
                }
            }

            // Click Clickable Link
            if (HoveredLink?.OnClick != null)
            {
                var nextSubject = HoveredLink.OnClick();
                if (nextSubject != null)
                {
                    NavigateTo(nextSubject);
                    return;
                }
            }

            // Click outside bounds closes menu
            if (!new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height).Contains(x, y))
            {
                CloseMenu();
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            if (History.Count > 0 || CurrentSubject != null)
            {
                NavigateBack();
            }
            else
            {
                CloseMenu();
            }
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
            if (SearchBox != null && SearchBox.Selected)
            {
                if (key == Keys.Escape)
                {
                    SearchBox.Selected = false;
                    Game1.keyboardDispatcher.Subscriber = null;
                }
                else if (key == Keys.Enter && SearchResults.Count > 0)
                {
                    var firstResult = SearchResults[0];
                    var nextSubject = firstResult.OnClick?.Invoke();
                    if (nextSubject != null)
                    {
                        NavigateTo(nextSubject);
                    }
                }
                return;
            }

            if (key == Keys.Escape || key == (Keys)ModEntry.Config.LookupKey)
            {
                CloseMenu();
                return;
            }

            if (key == Keys.Back)
            {
                NavigateBack();
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

        private void CloseMenu()
        {
            if (SearchBox != null)
            {
                SearchBox.Selected = false;
                Game1.keyboardDispatcher.Subscriber = null;
            }
            Game1.playSound("bigDeSelect");
            exitThisMenu();
        }

        public override void draw(SpriteBatch b)
        {
            ActiveClickableLinks.Clear();

            // 1. Dark semi-transparent background overlay
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.55f);

            // 2. Main Parchment Texture Box
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

            // 3. Header Bar Layout
            int headerX = xPositionOnScreen + 34;
            int headerY = yPositionOnScreen + 24;

            bool canGoBack = History.Count > 0 || (CurrentSubject != null && !string.IsNullOrEmpty(LastSearchText));
            if (canGoBack && BackButton != null)
            {
                BackButton.draw(b);
                headerX += 48;
            }

            if (CurrentSubject != null)
            {
                // Subject Portrait or Icon
                if (CurrentSubject.Portrait != null)
                {
                    Rectangle src = CurrentSubject.PortraitSourceRect ?? new Rectangle(0, 0, 64, 64);
                    b.Draw(CurrentSubject.Portrait, new Rectangle(headerX, headerY, 50, 50), src, Color.White);
                    headerX += 62;
                }
                else if (CurrentSubject.MainIcon != null)
                {
                    Rectangle src = CurrentSubject.MainIconSourceRect ?? new Rectangle(0, 0, CurrentSubject.MainIcon.Width, CurrentSubject.MainIcon.Height);
                    b.Draw(CurrentSubject.MainIcon, new Rectangle(headerX, headerY + 2, 42, 42), src, Color.White);
                    headerX += 52;
                }

                // Title & Subtitle (truncated before reaching search box)
                int maxTitleWidth = (SearchBox != null ? SearchBox.X - 36 : xPositionOnScreen + width - 60) - headerX;
                string title = CurrentSubject.Title;
                if (Game1.dialogueFont.MeasureString(title).X > maxTitleWidth && maxTitleWidth > 60)
                {
                    while (title.Length > 3 && Game1.dialogueFont.MeasureString(title + "...").X > maxTitleWidth)
                    {
                        title = title.Substring(0, title.Length - 1);
                    }
                    title += "...";
                }

                Utility.drawTextWithShadow(b, title, Game1.dialogueFont, new Vector2(headerX, headerY - 4), Game1.textColor);
                if (!string.IsNullOrEmpty(CurrentSubject.Subtitle))
                {
                    Utility.drawTextWithShadow(b, CurrentSubject.Subtitle, Game1.smallFont, new Vector2(headerX, headerY + 30), Color.DimGray);
                }
            }
            else
            {
                // Search Mode Title
                Utility.drawTextWithShadow(b, "Find Anything (F1)", Game1.dialogueFont, new Vector2(headerX, headerY - 2), Game1.textColor);
                Utility.drawTextWithShadow(b, "Type to query any item, villager, monster, or recipe...", Game1.smallFont, new Vector2(headerX, headerY + 30), Color.DimGray);
            }

            // Search Box
            if (SearchBox != null)
            {
                SearchBox.Draw(b);

                // Search icon
                int iconX = SearchBox.X - 26;
                int iconY = SearchBox.Y + 8;
                Utility.drawTextWithShadow(b, "🔍", Game1.smallFont, new Vector2(iconX, iconY), Game1.textColor);

                if (string.IsNullOrEmpty(SearchBox.Text) && !SearchBox.Selected)
                {
                    Utility.drawTextWithShadow(b, "Type to search...", Game1.smallFont, new Vector2(SearchBox.X + 12, SearchBox.Y + 10), Color.Gray * 0.8f);
                }
            }

            // Header Divider
            int dividerY = yPositionOnScreen + 88;
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 32, dividerY, width - 64, 2), Color.SaddleBrown * 0.3f);

            // 4. Content Area Layout & GPU-Clipping Scissor Rect
            int contentX = xPositionOnScreen + 36;
            int contentY = dividerY + 14;
            int contentWidth = width - 116; // Leaves ample space for scrollbar on the right
            int contentHeight = height - 152;
            int contentBottom = contentY + contentHeight;

            int currentY = contentY - ScrollOffset;
            int calculatedContentHeight = 0;

            // Start Scissor-Clipped Drawing for Content Viewport
            b.End();

            float scale = Game1.options.uiScale;
            Rectangle clipRect = new Rectangle(
                (int)(contentX * scale),
                (int)(contentY * scale),
                (int)(contentWidth * scale),
                (int)(contentHeight * scale)
            );

            // Clamp clip rect to screen bounds
            clipRect.X = Math.Max(0, Math.Min(clipRect.X, b.GraphicsDevice.Viewport.Width));
            clipRect.Y = Math.Max(0, Math.Min(clipRect.Y, b.GraphicsDevice.Viewport.Height));
            clipRect.Width = Math.Max(0, Math.Min(clipRect.Width, b.GraphicsDevice.Viewport.Width - clipRect.X));
            clipRect.Height = Math.Max(0, Math.Min(clipRect.Height, b.GraphicsDevice.Viewport.Height - clipRect.Y));

            Rectangle oldScissor = b.GraphicsDevice.ScissorRectangle;
            b.GraphicsDevice.ScissorRectangle = clipRect;

            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, ScissorRasterizer);

            // DRAW CONTENT INSIDE SCISSOR VIEWPORT
            if (!string.IsNullOrWhiteSpace(LastSearchText))
            {
                // Search Results Mode
                if (SearchResults.Count == 0)
                {
                    Utility.drawTextWithShadow(b, $"No results found for '{LastSearchText}'", Game1.smallFont, new Vector2(contentX, currentY + 16), Color.DarkSlateGray);
                    calculatedContentHeight += 50;
                }
                else
                {
                    foreach (var result in SearchResults)
                    {
                        int rowHeight = 44;
                        Rectangle rowBounds = new Rectangle(contentX, currentY, contentWidth, rowHeight);

                        // Track active clickable link
                        result.Bounds = rowBounds;
                        ActiveClickableLinks.Add(result);

                        bool isHovered = HoveredLink == result;

                        if (isHovered)
                        {
                            b.Draw(Game1.staminaRect, rowBounds, Color.SaddleBrown * 0.15f);
                        }

                        int itemIconX = contentX + 6;
                        if (result.Icon != null)
                        {
                            Rectangle src = result.IconSourceRect ?? new Rectangle(0, 0, result.Icon.Width, result.Icon.Height);
                            b.Draw(result.Icon, new Rectangle(itemIconX, currentY + 6, 32, 32), src, Color.White);
                        }

                        int labelX = itemIconX + 42;
                        Utility.drawTextWithShadow(b, result.Text, Game1.dialogueFont, new Vector2(labelX, currentY + 2), isHovered ? Color.DarkBlue : result.TextColor, 0.7f);

                        if (!string.IsNullOrEmpty(result.Subtitle))
                        {
                            Utility.drawTextWithShadow(b, result.Subtitle, Game1.smallFont, new Vector2(labelX + 220, currentY + 10), Color.DarkSlateGray);
                        }

                        Utility.drawTextWithShadow(b, ">", Game1.dialogueFont, new Vector2(contentX + contentWidth - 24, currentY + 4), Color.SaddleBrown * 0.5f, 0.7f);
                        b.Draw(Game1.staminaRect, new Rectangle(contentX, currentY + rowHeight - 2, contentWidth, 1), Color.SaddleBrown * 0.15f);

                        currentY += rowHeight;
                        calculatedContentHeight += rowHeight;
                    }
                }
            }
            else if (CurrentSubject != null)
            {
                // Full Subject Details
                foreach (var section in CurrentSubject.Sections)
                {
                    // Section Header
                    Utility.drawTextWithShadow(b, section.Title, Game1.dialogueFont, new Vector2(contentX, currentY), new Color(115, 40, 10));
                    currentY += 34;
                    calculatedContentHeight += 34;

                    foreach (var field in section.Fields)
                    {
                        string label = !string.IsNullOrEmpty(field.Label) ? $"{field.Label}: " : string.Empty;
                        Vector2 labelSize = Game1.smallFont.MeasureString(label);

                        if (field.Links.Count > 0)
                        {
                            Utility.drawTextWithShadow(b, label, Game1.smallFont, new Vector2(contentX + 10, currentY), Game1.textColor);
                            currentY += 26;
                            calculatedContentHeight += 26;

                            int chipX = contentX + 16;
                            int chipSpacing = 8;

                            foreach (var link in field.Links)
                            {
                                Vector2 textSize = Game1.smallFont.MeasureString(link.Text);
                                int chipWidth = (int)textSize.X + (link.Icon != null ? 36 : 18);
                                int chipHeight = 28;

                                if (chipX + chipWidth > contentX + contentWidth - 8)
                                {
                                    chipX = contentX + 16;
                                    currentY += chipHeight + 6;
                                    calculatedContentHeight += chipHeight + 6;
                                }

                                Rectangle chipBounds = new Rectangle(chipX, currentY, chipWidth, chipHeight);
                                link.Bounds = chipBounds;
                                ActiveClickableLinks.Add(link);

                                bool isHovered = HoveredLink == link;

                                IClickableMenu.drawTextureBox(
                                    b,
                                    Game1.menuTexture,
                                    new Rectangle(0, 256, 60, 60),
                                    chipBounds.X,
                                    chipBounds.Y,
                                    chipBounds.Width,
                                    chipBounds.Height,
                                    isHovered ? Color.Wheat : Color.White,
                                    0.6f,
                                    drawShadow: false
                                );

                                int drawTextX = chipBounds.X + 8;
                                if (link.Icon != null)
                                {
                                    Rectangle src = link.IconSourceRect ?? new Rectangle(0, 0, link.Icon.Width, link.Icon.Height);
                                    b.Draw(link.Icon, new Rectangle(drawTextX, chipBounds.Y + 3, 22, 22), src, Color.White);
                                    drawTextX += 26;
                                }

                                Utility.drawTextWithShadow(b, link.Text, Game1.smallFont, new Vector2(drawTextX, chipBounds.Y + 4), isHovered ? Color.DarkBlue : link.TextColor);

                                chipX += chipWidth + chipSpacing;
                            }

                            currentY += 34;
                            calculatedContentHeight += 34;
                        }
                        else
                        {
                            int valWidth = contentWidth - (int)labelSize.X - 24;
                            string wrappedValue = Game1.parseText(field.Value ?? string.Empty, Game1.smallFont, Math.Max(140, valWidth));
                            Vector2 valSize = Game1.smallFont.MeasureString(wrappedValue);
                            int lineH = (int)Math.Max(26, valSize.Y + 4);

                            Utility.drawTextWithShadow(b, label, Game1.smallFont, new Vector2(contentX + 10, currentY), Game1.textColor);
                            Utility.drawTextWithShadow(b, wrappedValue, Game1.smallFont, new Vector2(contentX + 10 + labelSize.X, currentY), field.ValueColor);

                            currentY += lineH;
                            calculatedContentHeight += lineH;
                        }
                    }

                    currentY += 12;
                    calculatedContentHeight += 12;
                }
            }

            // End Scissor Drawing & Restore Sprite Batch
            b.End();
            b.GraphicsDevice.ScissorRectangle = oldScissor;
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            MaxScrollOffset = Math.Max(0, calculatedContentHeight - contentHeight);

            // 5. Draw Scrollbar Track & Up/Down Buttons (Nicely inset inside right parchment border)
            CloseButton?.draw(b);

            if (MaxScrollOffset > 0)
            {
                UpButton?.draw(b);
                DownButton?.draw(b);

                int trackX = xPositionOnScreen + width - 48;
                int trackY = contentY + 44;
                int trackH = contentHeight - 88;

                // Track Background
                b.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, 6, trackH), Color.SaddleBrown * 0.25f);

                // Scroll Thumb
                float scrollPct = (float)ScrollOffset / MaxScrollOffset;
                int thumbH = Math.Max(20, (int)(trackH * (float)contentHeight / (contentHeight + MaxScrollOffset)));
                int thumbY = trackY + (int)((trackH - thumbH) * scrollPct);
                b.Draw(Game1.staminaRect, new Rectangle(trackX - 1, thumbY, 8, thumbH), Color.SaddleBrown * 0.8f);
            }

            drawMouse(b);
        }
    }
}
