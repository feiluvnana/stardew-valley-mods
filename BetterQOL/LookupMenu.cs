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
        private readonly Stack<LookupSubject> ForwardHistory = new();

        private ClickableTextureComponent? CloseButton;
        private ClickableTextureComponent? BackButton;
        private ClickableTextureComponent? ForwardButton;
        private ClickableTextureComponent? UpButton;
        private ClickableTextureComponent? DownButton;

        private TextBox? SearchBox;
        private ClickableComponent? SearchBoxComponent;
        private string LastSearchText = string.Empty;
        private List<LookupLink> SearchResults = new();
        private string CurrentCategory = "All";
        private static readonly string[] SearchCategories = new[] { "All", "Items", "Villagers", "Fish", "Crops", "Monsters", "Buildings", "Recipes", "Locations" };
        private readonly List<ClickableComponent> CategoryButtons = new();

        private int ScrollOffset = 0;
        private int MaxScrollOffset = 0;
        private const int ScrollStep = 40;

        private readonly List<LookupLink> ActiveClickableLinks = new();
        private LookupLink? HoveredLink = null;
        private string? HoveredCategory = null;

        public LookupMenu(LookupSubject? initialSubject = null)
            : base(
                x: (Game1.uiViewport.Width - Math.Min(860, Game1.uiViewport.Width - 48)) / 2,
                y: (Game1.uiViewport.Height - Math.Min(680, Game1.uiViewport.Height - 48)) / 2,
                width: Math.Min(860, Game1.uiViewport.Width - 48),
                height: Math.Min(680, Game1.uiViewport.Height - 48),
                showUpperRightCloseButton: true
            )
        {
            CurrentSubject = initialSubject ?? LookupDataManager.BuildWorldOverviewSubject();
            Game1.playSound("bigSelect");

            InitializeComponents();
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            width = Math.Min(860, Game1.uiViewport.Width - 48);
            height = Math.Min(680, Game1.uiViewport.Height - 48);
            xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
            yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // 1. Close Button (top-right, outside inner content)
            CloseButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width - 38, yPositionOnScreen - 6, 48, 48),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );

            // 2. Back and Forward Buttons (top-left)
            int headerTopY = yPositionOnScreen + 30;
            BackButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + 32, headerTopY + 4, 44, 44),
                Game1.mouseCursors,
                new Rectangle(352, 495, 12, 11),
                3.5f
            );

            ForwardButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + 80, headerTopY + 4, 44, 44),
                Game1.mouseCursors,
                new Rectangle(365, 495, 12, 11),
                3.5f
            );

            // 3. Search Box (top-right header)
            int searchBoxW = 210;
            int searchBoxH = 48;
            int searchBoxX = xPositionOnScreen + width - searchBoxW - 48;
            int searchBoxY = headerTopY + 2;

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

            // 4. Content Area Layout & Scroll Buttons
            int dividerY = yPositionOnScreen + 104;
            int contentY = dividerY + 18;
            int contentHeight = height - 172;
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
            ForwardHistory.Clear();
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
                if (CurrentSubject != null)
                {
                    ForwardHistory.Push(CurrentSubject);
                }
                CurrentSubject = History.Pop();
                ScrollOffset = 0;
                Game1.playSound("smallSelect");
            }
            else if (CurrentSubject != null)
            {
                ForwardHistory.Push(CurrentSubject);
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

        public void NavigateForward()
        {
            if (ForwardHistory.Count > 0)
            {
                if (CurrentSubject != null)
                {
                    History.Push(CurrentSubject);
                }
                CurrentSubject = ForwardHistory.Pop();
                ScrollOffset = 0;
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
                        SearchResults = LookupDataManager.SearchAll(LastSearchText, CurrentCategory);
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
            ForwardButton?.tryHover(x, y, 0.2f);
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

            HoveredCategory = null;
            foreach (var catBtn in CategoryButtons)
            {
                if (catBtn.containsPoint(x, y))
                {
                    HoveredCategory = catBtn.name;
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

            if (ForwardHistory.Count > 0 && ForwardButton != null && ForwardButton.containsPoint(x, y))
            {
                NavigateForward();
                return;
            }

            // Category Tab Buttons
            foreach (var catBtn in CategoryButtons)
            {
                if (catBtn.containsPoint(x, y))
                {
                    CurrentCategory = catBtn.name;
                    SearchResults = LookupDataManager.SearchAll(LastSearchText, CurrentCategory);
                    ScrollOffset = 0;
                    Game1.playSound("smallSelect");
                    return;
                }
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

        public override void receiveGamePadButton(Buttons b)
        {
            base.receiveGamePadButton(b);

            if (b == Buttons.B)
            {
                NavigateBack();
            }
            else if (b == Buttons.Y)
            {
                if (SearchBox != null)
                {
                    SearchBox.Selected = !SearchBox.Selected;
                    Game1.keyboardDispatcher.Subscriber = SearchBox.Selected ? SearchBox : null;
                }
            }
            else if (b == Buttons.RightThumbstickDown || b == Buttons.DPadDown || b == Buttons.RightTrigger)
            {
                ScrollOffset = Math.Min(MaxScrollOffset, ScrollOffset + ScrollStep * 2);
                Game1.playSound("shiny4");
            }
            else if (b == Buttons.RightThumbstickUp || b == Buttons.DPadUp || b == Buttons.LeftTrigger)
            {
                ScrollOffset = Math.Max(0, ScrollOffset - ScrollStep * 2);
                Game1.playSound("shiny4");
            }
            else if (b == Buttons.RightStick || b == Buttons.Back)
            {
                CloseMenu();
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

        private string GetCategoryDisplayName(string category)
        {
            return ModEntry.I18n.Get($"lookup.search.category.{category.ToLowerInvariant()}").ToString();
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

            // Layout metrics
            int dividerY = yPositionOnScreen + 104;
            int contentX = xPositionOnScreen + 36;
            int contentY = dividerY + 18;
            int contentWidth = width - 116; // Leaves dedicated space for scrollbar on the right
            int contentHeight = height - 172;
            int contentBottom = contentY + contentHeight;

            int currentY = contentY - ScrollOffset;
            int calculatedContentHeight = 0;

            // 3. DRAW CONTENT VIEWPORT WITH BOUNDS CULLING (NO b.End() or ScissorRasterizer needed)
            if (!string.IsNullOrWhiteSpace(LastSearchText))
            {
                // Search Category Filter Tabs
                CategoryButtons.Clear();
                int catX = contentX + 4;
                int catY = currentY;
                int catHeight = 30;
                int startY = currentY;
                foreach (var catName in SearchCategories)
                {
                    string catDisplayName = GetCategoryDisplayName(catName);
                    Vector2 catSize = Game1.smallFont.MeasureString(catDisplayName);
                    int catW = (int)catSize.X + 16;
                    if (catX + catW > contentX + contentWidth && catX > contentX + 4)
                    {
                        catX = contentX + 4;
                        catY += catHeight + 6;
                    }

                    Rectangle catBounds = new Rectangle(catX, catY, catW, catHeight);

                    // Only draw and register clickable if visible in viewport
                    if (catY + catHeight >= contentY && catY <= contentBottom)
                    {
                        CategoryButtons.Add(new ClickableComponent(catBounds, catName));

                        bool isSelected = CurrentCategory.Equals(catName, StringComparison.OrdinalIgnoreCase);
                        bool isHovered = HoveredCategory != null && string.Equals(HoveredCategory, catName, StringComparison.OrdinalIgnoreCase);

                        Color bg = isSelected ? new Color(180, 100, 30) : (isHovered ? new Color(245, 230, 200) : new Color(230, 210, 175));
                        Color border = isSelected ? new Color(110, 40, 10) : new Color(170, 130, 90);
                        Color txtColor = isSelected ? Color.White : (isHovered ? Color.DarkBlue : Game1.textColor);

                        b.Draw(Game1.staminaRect, catBounds, bg);
                        b.Draw(Game1.staminaRect, new Rectangle(catBounds.X, catBounds.Y, catBounds.Width, 1), border);
                        b.Draw(Game1.staminaRect, new Rectangle(catBounds.X, catBounds.Bottom - 1, catBounds.Width, 1), border);
                        b.Draw(Game1.staminaRect, new Rectangle(catBounds.X, catBounds.Y, 1, catBounds.Height), border);
                        b.Draw(Game1.staminaRect, new Rectangle(catBounds.Right - 1, catBounds.Y, 1, catBounds.Height), border);

                        int textX = catBounds.X + (catW - (int)catSize.X) / 2;
                        int textY = catBounds.Y + (catHeight - (int)catSize.Y) / 2;
                        Utility.drawTextWithShadow(b, catDisplayName, Game1.smallFont, new Vector2(textX, textY), txtColor);
                    }

                    catX += catW + 6;
                }
                int totalCatHeaderHeight = (catY - startY) + catHeight + 12;
                currentY += totalCatHeaderHeight;
                calculatedContentHeight += totalCatHeaderHeight;

                // Search Results Mode
                if (SearchResults.Count == 0)
                {
                    if (currentY + 40 >= contentY && currentY <= contentBottom)
                    {
                        Utility.drawTextWithShadow(b, ModEntry.I18n.Get("lookup.menu.no-results", new { query = LastSearchText, category = GetCategoryDisplayName(CurrentCategory) }).ToString(), Game1.smallFont, new Vector2(contentX, currentY + 16), Color.DarkSlateGray);
                    }
                    calculatedContentHeight += 50;
                }
                else
                {
                    foreach (var result in SearchResults)
                    {
                        int rowHeight = 44;
                        Rectangle rowBounds = new Rectangle(contentX, currentY, contentWidth, rowHeight);

                        if (currentY + rowHeight >= contentY && currentY <= contentBottom)
                        {
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

                            // Right-aligned Subtitle
                            int subX = contentX + contentWidth - 36;
                            if (!string.IsNullOrEmpty(result.Subtitle))
                            {
                                Vector2 subSize = Game1.smallFont.MeasureString(result.Subtitle);
                                subX = contentX + contentWidth - 36 - (int)subSize.X;
                                Utility.drawTextWithShadow(b, result.Subtitle, Game1.smallFont, new Vector2(subX, currentY + 10), Color.DarkSlateGray);
                            }

                            // Title with safety truncation if too wide
                            int maxLabelW = subX - labelX - 16;
                            string titleText = result.Text;
                            if (Game1.dialogueFont.MeasureString(titleText).X * 0.7f > maxLabelW && maxLabelW > 40)
                            {
                                while (titleText.Length > 3 && Game1.dialogueFont.MeasureString(titleText + "...").X * 0.7f > maxLabelW)
                                {
                                    titleText = titleText.Substring(0, titleText.Length - 1);
                                }
                                titleText += "...";
                            }

                            Utility.drawTextWithShadow(b, titleText, Game1.dialogueFont, new Vector2(labelX, currentY + 2), isHovered ? Color.DarkBlue : result.TextColor, 0.7f);

                            Utility.drawTextWithShadow(b, ">", Game1.dialogueFont, new Vector2(contentX + contentWidth - 24, currentY + 4), Color.SaddleBrown * 0.5f, 0.7f);
                            b.Draw(Game1.staminaRect, new Rectangle(contentX, currentY + rowHeight - 2, contentWidth, 1), Color.SaddleBrown * 0.15f);
                        }

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
                    if (currentY + 40 >= contentY && currentY <= contentBottom)
                    {
                        Utility.drawTextWithShadow(b, section.Title, Game1.dialogueFont, new Vector2(contentX + 4, currentY), new Color(115, 40, 10));
                    }
                    currentY += 46;
                    calculatedContentHeight += 46;

                    foreach (var field in section.Fields)
                    {
                        string label = !string.IsNullOrEmpty(field.Label) ? $"{field.Label}: " : string.Empty;
                        Vector2 labelSize = Game1.smallFont.MeasureString(label);

                        if (field.Links.Count > 0)
                        {
                            if (currentY + 28 >= contentY && currentY <= contentBottom)
                            {
                                Utility.drawTextWithShadow(b, label, Game1.smallFont, new Vector2(contentX + 12, currentY), Game1.textColor);
                            }
                            currentY += 34;
                            calculatedContentHeight += 34;

                            int chipX = contentX + 16;
                            int chipSpacing = 10;
                            int chipHeight = 36;
                            int chipIconSize = 24;

                            foreach (var link in field.Links)
                            {
                                Vector2 textSize = Game1.smallFont.MeasureString(link.Text);
                                int chipWidth = (int)textSize.X + (link.Icon != null ? chipIconSize + 16 : 14) + 12;

                                if (chipX + chipWidth > contentX + contentWidth - 8)
                                {
                                    chipX = contentX + 16;
                                    currentY += chipHeight + 8;
                                    calculatedContentHeight += chipHeight + 8;
                                }

                                Rectangle chipBounds = new Rectangle(chipX, currentY, chipWidth, chipHeight);

                                if (currentY + chipHeight >= contentY && currentY <= contentBottom)
                                {
                                    link.Bounds = chipBounds;
                                    ActiveClickableLinks.Add(link);

                                    bool isHovered = HoveredLink == link;

                                    Color bgColor = isHovered ? new Color(255, 245, 215) : new Color(248, 230, 192);
                                    Color borderColor = isHovered ? new Color(110, 35, 10) : new Color(185, 135, 90);

                                    b.Draw(Game1.staminaRect, chipBounds, bgColor);

                                    b.Draw(Game1.staminaRect, new Rectangle(chipBounds.X, chipBounds.Y, chipBounds.Width, 1), borderColor);
                                    b.Draw(Game1.staminaRect, new Rectangle(chipBounds.X, chipBounds.Bottom - 1, chipBounds.Width, 1), borderColor);
                                    b.Draw(Game1.staminaRect, new Rectangle(chipBounds.X, chipBounds.Y, 1, chipBounds.Height), borderColor);
                                    b.Draw(Game1.staminaRect, new Rectangle(chipBounds.Right - 1, chipBounds.Y, 1, chipBounds.Height), borderColor);

                                    int drawIconX = chipBounds.X + 8;
                                    int drawTextX = drawIconX;

                                    if (link.Icon != null)
                                    {
                                        int iconY = chipBounds.Y + (chipHeight - chipIconSize) / 2;
                                        Rectangle src = link.IconSourceRect ?? new Rectangle(0, 0, link.Icon.Width, link.Icon.Height);
                                        b.Draw(link.Icon, new Rectangle(drawIconX, iconY, chipIconSize, chipIconSize), src, Color.White);
                                        drawTextX += chipIconSize + 8;
                                    }
                                    else
                                    {
                                        drawTextX += 2;
                                    }

                                    int textY = chipBounds.Y + (chipHeight - (int)textSize.Y) / 2 + 1;
                                    Utility.drawTextWithShadow(b, link.Text, Game1.smallFont, new Vector2(drawTextX, textY), isHovered ? Color.DarkBlue : link.TextColor);
                                }

                                chipX += chipWidth + chipSpacing;
                            }

                            currentY += chipHeight + 14;
                            calculatedContentHeight += chipHeight + 14;
                        }
                        else
                        {
                            int valWidth = contentWidth - (int)labelSize.X - 32;
                            string wrappedValue = Game1.parseText(field.Value ?? string.Empty, Game1.smallFont, Math.Max(140, valWidth));
                            Vector2 valSize = Game1.smallFont.MeasureString(wrappedValue);
                            int lineH = (int)Math.Max(30, valSize.Y + 8);

                            if (currentY + lineH >= contentY && currentY <= contentBottom)
                            {
                                Utility.drawTextWithShadow(b, label, Game1.smallFont, new Vector2(contentX + 12, currentY), Game1.textColor);
                                Utility.drawTextWithShadow(b, wrappedValue, Game1.smallFont, new Vector2(contentX + 12 + labelSize.X, currentY), field.ValueColor);
                            }

                            currentY += lineH;
                            calculatedContentHeight += lineH;
                        }
                    }

                    if (currentY + 20 >= contentY && currentY <= contentBottom)
                    {
                        b.Draw(Game1.staminaRect, new Rectangle(contentX + 6, currentY + 6, contentWidth - 12, 1), Color.SaddleBrown * 0.15f);
                    }
                    currentY += 24;
                    calculatedContentHeight += 24;
                }
            }

            MaxScrollOffset = Math.Max(0, calculatedContentHeight - contentHeight);

            // 4. HEADER BACKGROUND & OVERLAYS (Draw on top of scrolled content for clean visual cutoff)
            // Solid parchment top mask
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 16, yPositionOnScreen + 16, width - 32, 92), new Color(248, 230, 192));
            // Solid parchment bottom mask
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 16, contentBottom + 2, width - 32, yPositionOnScreen + height - contentBottom - 18), new Color(248, 230, 192));

            // Header Divider
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 32, dividerY, width - 64, 2), Color.SaddleBrown * 0.3f);

            // Header Bar Layout
            int headerTopY = yPositionOnScreen + 30;
            int headerLeftX = xPositionOnScreen + 34;

            bool canGoBack = History.Count > 0 || (CurrentSubject != null && !string.IsNullOrEmpty(LastSearchText));
            if (canGoBack && BackButton != null)
            {
                BackButton.draw(b);
                headerLeftX += 46;
            }

            bool canGoForward = ForwardHistory.Count > 0;
            if (canGoForward && ForwardButton != null)
            {
                ForwardButton.draw(b);
                headerLeftX += 46;
            }

            if (CurrentSubject != null)
            {
                // Subject Portrait or Icon
                if (CurrentSubject.Portrait != null)
                {
                    Rectangle src = CurrentSubject.PortraitSourceRect ?? new Rectangle(0, 0, 64, 64);
                    b.Draw(CurrentSubject.Portrait, new Rectangle(headerLeftX, headerTopY, 56, 56), src, Color.White);
                    headerLeftX += 68;
                }
                else if (CurrentSubject.MainIcon != null)
                {
                    Rectangle src = CurrentSubject.MainIconSourceRect ?? new Rectangle(0, 0, CurrentSubject.MainIcon.Width, CurrentSubject.MainIcon.Height);
                    b.Draw(CurrentSubject.MainIcon, new Rectangle(headerLeftX, headerTopY + 4, 48, 48), src, Color.White);
                    headerLeftX += 58;
                }

                // Title & Subtitle with proper breathing room
                int maxHeaderWidth = (SearchBox != null ? SearchBox.X - 16 : xPositionOnScreen + width - 54) - headerLeftX;

                string title = CurrentSubject.Title;
                if (Game1.dialogueFont.MeasureString(title).X > maxHeaderWidth && maxHeaderWidth > 60)
                {
                    while (title.Length > 3 && Game1.dialogueFont.MeasureString(title + "...").X > maxHeaderWidth)
                    {
                        title = title.Substring(0, title.Length - 1);
                    }
                    title += "...";
                }

                Utility.drawTextWithShadow(b, title, Game1.dialogueFont, new Vector2(headerLeftX, headerTopY - 2), Game1.textColor);

                if (!string.IsNullOrEmpty(CurrentSubject.Subtitle))
                {
                    string subtitle = CurrentSubject.Subtitle;
                    if (Game1.smallFont.MeasureString(subtitle).X > maxHeaderWidth && maxHeaderWidth > 60)
                    {
                        while (subtitle.Length > 3 && Game1.smallFont.MeasureString(subtitle + "...").X > maxHeaderWidth)
                        {
                            subtitle = subtitle.Substring(0, subtitle.Length - 1);
                        }
                        subtitle += "...";
                    }
                    Utility.drawTextWithShadow(b, subtitle, Game1.smallFont, new Vector2(headerLeftX, headerTopY + 36), Color.DimGray);
                }
            }
            else
            {
                // Search Mode Title
                string searchTitle = ModEntry.I18n.Get("lookup.menu.search-title").ToString();
                string searchSub = ModEntry.I18n.Get("lookup.menu.search-subtitle").ToString();
                int maxSearchW = (SearchBox != null ? SearchBox.X - 16 : xPositionOnScreen + width - 54) - headerLeftX;
                if (Game1.smallFont.MeasureString(searchSub).X > maxSearchW && maxSearchW > 60)
                {
                    while (searchSub.Length > 3 && Game1.smallFont.MeasureString(searchSub + "...").X > maxSearchW)
                    {
                        searchSub = searchSub.Substring(0, searchSub.Length - 1);
                    }
                    searchSub += "...";
                }
                Utility.drawTextWithShadow(b, searchTitle, Game1.dialogueFont, new Vector2(headerLeftX, headerTopY - 2), Game1.textColor);
                Utility.drawTextWithShadow(b, searchSub, Game1.smallFont, new Vector2(headerLeftX, headerTopY + 36), Color.DimGray);
            }

            // Search Box (Rendered cleanly with native texture and no cropping)
            if (SearchBox != null)
            {
                SearchBox.Draw(b);

                if (string.IsNullOrEmpty(SearchBox.Text) && !SearchBox.Selected)
                {
                    Utility.drawTextWithShadow(b, ModEntry.I18n.Get("lookup.menu.search-placeholder").ToString(), Game1.smallFont, new Vector2(SearchBox.X + 16, SearchBox.Y + 12), Color.Gray * 0.75f);
                }
            }

            // 5. Scrollbar Track & Up/Down Buttons
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
