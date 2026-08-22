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
        private readonly List<ClickableComponent> Components = new();

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
        private const int ScrollStep = 36;

        private readonly List<LookupLink> ActiveClickableLinks = new();
        private LookupLink? HoveredLink = null;

        public LookupMenu(LookupSubject? initialSubject = null)
            : base(
                x: (Game1.uiViewport.Width - Math.Min(820, Game1.uiViewport.Width - 64)) / 2,
                y: (Game1.uiViewport.Height - Math.Min(640, Game1.uiViewport.Height - 64)) / 2,
                width: Math.Min(820, Game1.uiViewport.Width - 64),
                height: Math.Min(640, Game1.uiViewport.Height - 64),
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
            Components.Clear();

            // 1. Close Button (top-right)
            CloseButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width - 38, yPositionOnScreen - 6, 48, 48),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );
            Components.Add(CloseButton);

            // 2. Back Button (top-left)
            BackButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + 28, yPositionOnScreen + 28, 44, 44),
                Game1.mouseCursors,
                new Rectangle(352, 495, 12, 11),
                3.5f
            );

            // 3. Search Box (top-right header)
            int searchBoxW = 240;
            int searchBoxH = 44;
            int searchBoxX = xPositionOnScreen + width - searchBoxW - 56;
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

            // 4. Scroll Buttons (right margin)
            UpButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width - 36, yPositionOnScreen + 104, 44, 48),
                Game1.mouseCursors,
                new Rectangle(421, 459, 11, 12),
                4f
            );
            Components.Add(UpButton);

            DownButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width - 36, yPositionOnScreen + height - 64, 44, 48),
                Game1.mouseCursors,
                new Rectangle(421, 472, 11, 12),
                4f
            );
            Components.Add(DownButton);
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
            int headerX = xPositionOnScreen + 32;
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
                    b.Draw(CurrentSubject.Portrait, new Rectangle(headerX, headerY, 52, 52), src, Color.White);
                    headerX += 64;
                }
                else if (CurrentSubject.MainIcon != null)
                {
                    Rectangle src = CurrentSubject.MainIconSourceRect ?? new Rectangle(0, 0, CurrentSubject.MainIcon.Width, CurrentSubject.MainIcon.Height);
                    b.Draw(CurrentSubject.MainIcon, new Rectangle(headerX, headerY + 2, 44, 44), src, Color.White);
                    headerX += 54;
                }

                // Title & Subtitle (with width clamping so it never overlaps search box)
                int maxTitleWidth = (SearchBox != null ? SearchBox.X - 40 : xPositionOnScreen + width - 60) - headerX;
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
                    Utility.drawTextWithShadow(b, CurrentSubject.Subtitle, Game1.smallFont, new Vector2(headerX, headerY + 32), Color.DimGray);
                }
            }
            else
            {
                // Search Mode Title
                Utility.drawTextWithShadow(b, "Find Anything (F1)", Game1.dialogueFont, new Vector2(headerX, headerY - 2), Game1.textColor);
                Utility.drawTextWithShadow(b, "Type to query any item, villager, monster, or recipe...", Game1.smallFont, new Vector2(headerX, headerY + 32), Color.DimGray);
            }

            // Search Box
            if (SearchBox != null)
            {
                SearchBox.Draw(b);

                // Search icon
                int iconX = SearchBox.X - 28;
                int iconY = SearchBox.Y + 8;
                Utility.drawTextWithShadow(b, "🔍", Game1.smallFont, new Vector2(iconX, iconY), Game1.textColor);

                if (string.IsNullOrEmpty(SearchBox.Text) && !SearchBox.Selected)
                {
                    Utility.drawTextWithShadow(b, "Type to search...", Game1.smallFont, new Vector2(SearchBox.X + 14, SearchBox.Y + 10), Color.Gray * 0.8f);
                }
            }

            // Header Divider
            int dividerY = yPositionOnScreen + 86;
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 28, dividerY, width - 56, 2), Color.SaddleBrown * 0.3f);

            // 4. Content Area Layout & Strict Padding Clamping
            int contentX = xPositionOnScreen + 34;
            int contentY = dividerY + 14;
            int contentWidth = width - 82;
            int contentHeight = height - 150;
            int contentBottom = contentY + contentHeight;

            int currentY = contentY - ScrollOffset;
            int totalContentHeight = 0;

            if (!string.IsNullOrWhiteSpace(LastSearchText))
            {
                // Search Results Mode
                if (SearchResults.Count == 0)
                {
                    Utility.drawTextWithShadow(b, $"No results found for '{LastSearchText}'", Game1.smallFont, new Vector2(contentX, contentY + 20), Color.DarkSlateGray);
                }
                else
                {
                    foreach (var result in SearchResults)
                    {
                        int rowHeight = 44;
                        Rectangle rowBounds = new Rectangle(contentX, currentY, contentWidth, rowHeight);

                        // Strict clamping inside content viewport
                        if (currentY >= contentY - 8 && currentY + rowHeight <= contentBottom + 8)
                        {
                            result.Bounds = rowBounds;
                            ActiveClickableLinks.Add(result);

                            bool isHovered = HoveredLink == result;

                            if (isHovered)
                            {
                                b.Draw(Game1.staminaRect, rowBounds, Color.SaddleBrown * 0.15f);
                            }

                            int itemIconX = contentX + 8;
                            if (result.Icon != null)
                            {
                                Rectangle src = result.IconSourceRect ?? new Rectangle(0, 0, result.Icon.Width, result.Icon.Height);
                                b.Draw(result.Icon, new Rectangle(itemIconX, currentY + 6, 32, 32), src, Color.White);
                            }

                            int labelX = itemIconX + 44;
                            Utility.drawTextWithShadow(b, result.Text, Game1.dialogueFont, new Vector2(labelX, currentY + 2), isHovered ? Color.DarkBlue : result.TextColor, 0.7f);

                            if (!string.IsNullOrEmpty(result.Subtitle))
                            {
                                Utility.drawTextWithShadow(b, result.Subtitle, Game1.smallFont, new Vector2(labelX + 220, currentY + 10), Color.DarkSlateGray);
                            }

                            Utility.drawTextWithShadow(b, ">", Game1.dialogueFont, new Vector2(contentX + contentWidth - 28, currentY + 4), Color.SaddleBrown * 0.5f, 0.7f);
                            b.Draw(Game1.staminaRect, new Rectangle(contentX, currentY + rowHeight - 2, contentWidth, 1), Color.SaddleBrown * 0.15f);
                        }

                        currentY += rowHeight;
                        totalContentHeight += rowHeight;
                    }
                }
            }
            else if (CurrentSubject != null)
            {
                // Full Subject Details
                foreach (var section in CurrentSubject.Sections)
                {
                    // Section Header
                    if (currentY >= contentY - 8 && currentY + 30 <= contentBottom + 8)
                    {
                        Utility.drawTextWithShadow(b, section.Title, Game1.dialogueFont, new Vector2(contentX, currentY), new Color(115, 40, 10));
                    }
                    currentY += 34;
                    totalContentHeight += 34;

                    foreach (var field in section.Fields)
                    {
                        string label = !string.IsNullOrEmpty(field.Label) ? $"{field.Label}: " : string.Empty;
                        Vector2 labelSize = Game1.smallFont.MeasureString(label);

                        if (field.Links.Count > 0)
                        {
                            if (currentY >= contentY - 8 && currentY + 24 <= contentBottom + 8)
                            {
                                Utility.drawTextWithShadow(b, label, Game1.smallFont, new Vector2(contentX + 10, currentY), Game1.textColor);
                            }
                            currentY += 26;
                            totalContentHeight += 26;

                            int chipX = contentX + 20;
                            int chipSpacing = 10;

                            foreach (var link in field.Links)
                            {
                                Vector2 textSize = Game1.smallFont.MeasureString(link.Text);
                                int chipWidth = (int)textSize.X + (link.Icon != null ? 36 : 18);
                                int chipHeight = 28;

                                if (chipX + chipWidth > contentX + contentWidth - 10)
                                {
                                    chipX = contentX + 20;
                                    currentY += chipHeight + 6;
                                    totalContentHeight += chipHeight + 6;
                                }

                                Rectangle chipBounds = new Rectangle(chipX, currentY, chipWidth, chipHeight);

                                if (currentY >= contentY - 8 && currentY + chipHeight <= contentBottom + 8)
                                {
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
                                }

                                chipX += chipWidth + chipSpacing;
                            }

                            currentY += 34;
                            totalContentHeight += 34;
                        }
                        else
                        {
                            int valWidth = contentWidth - (int)labelSize.X - 24;
                            string wrappedValue = Game1.parseText(field.Value ?? string.Empty, Game1.smallFont, Math.Max(160, valWidth));
                            Vector2 valSize = Game1.smallFont.MeasureString(wrappedValue);
                            int lineH = (int)Math.Max(26, valSize.Y + 4);

                            if (currentY >= contentY - 8 && currentY + lineH <= contentBottom + 8)
                            {
                                Utility.drawTextWithShadow(b, label, Game1.smallFont, new Vector2(contentX + 10, currentY), Game1.textColor);
                                Utility.drawTextWithShadow(b, wrappedValue, Game1.smallFont, new Vector2(contentX + 10 + labelSize.X, currentY), field.ValueColor);
                            }

                            currentY += lineH;
                            totalContentHeight += lineH;
                        }
                    }

                    currentY += 10;
                    totalContentHeight += 10;
                }
            }

            MaxScrollOffset = Math.Max(0, totalContentHeight - contentHeight);

            // 5. Draw Controls & Scrollbars
            CloseButton?.draw(b);

            if (MaxScrollOffset > 0)
            {
                UpButton?.draw(b);
                DownButton?.draw(b);

                int trackX = xPositionOnScreen + width - 22;
                int trackY = yPositionOnScreen + 154;
                int trackH = height - 230;

                b.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, 6, trackH), Color.SaddleBrown * 0.2f);

                float scrollPct = (float)ScrollOffset / MaxScrollOffset;
                int thumbY = trackY + (int)((trackH - 24) * scrollPct);
                b.Draw(Game1.staminaRect, new Rectangle(trackX - 1, thumbY, 8, 24), Color.SaddleBrown * 0.7f);
            }

            drawMouse(b);
        }
    }
}
