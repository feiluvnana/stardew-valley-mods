using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace BetterQOL
{
    /// <summary>
    /// A full-screen "Lookup Anything" window opened with F1. It INHERITS from the game's
    /// IClickableMenu base class ("class X : Y" means "X is a Y that..."), which already
    /// knows how to be positioned, receive clicks/keys/scroll, and close - we only
    /// OVERRIDE the pieces we want to customize (drawing, input handling).
    ///
    /// The menu shows either a subject's details page or live search results, with a
    /// back-button history so links can drill down through related subjects.
    /// </summary>
    public class LookupMenu : IClickableMenu
    {
        // The page currently displayed. "?" marks it nullable: null means search mode.
        private LookupSubject? CurrentSubject;
        // Navigation history. Stack<T> is LIFO (last-in-first-out): Push remembers pages,
        // Pop retrieves the most recent one - perfect for a Back button.
        private readonly Stack<LookupSubject> History = new();

        // Texture-based buttons. Each pairs a screen rectangle with a sprite cut out of
        // the game's cursors.png sheet; "?" because they're built in InitializeComponents.
        private ClickableTextureComponent? CloseButton;
        private ClickableTextureComponent? BackButton;
        private ClickableTextureComponent? UpButton;
        private ClickableTextureComponent? DownButton;

        // The native Stardew text input widget plus an invisible click target covering it.
        private TextBox? SearchBox;
        private ClickableComponent? SearchBoxComponent;
        // Previous frame's query - comparing against it detects when the user typed.
        private string LastSearchText = string.Empty;
        // Rows shown while searching; empty list = show the details page instead.
        private List<LookupLink> SearchResults = new();
        // Active filter tab for searches ("All", "Items", ...). "All" is lowercase-safe.
        private string CurrentCategory = "All";
        // "static readonly" array: shared by every instance and never reassigned.
        private static readonly string[] SearchCategories = new[] { "All", "Items", "Villagers", "Fish", "Crops", "Monsters", "Buildings", "Recipes", "Locations" };
        // Category tab hit-boxes, rebuilt each draw pass while search mode is active.
        private readonly List<ClickableComponent> CategoryButtons = new();

        // Scrolling state: how far content is shifted up, how far it MAY shift, and the
        // pixels moved per wheel notch. "const" = fixed at compile time.
        private int ScrollOffset = 0;
        private int MaxScrollOffset = 0;
        private const int ScrollStep = 40;

        // Links currently visible on screen (re-registered during every Draw call) and
        // which link/category the cursor hovers, used for highlight coloring.
        private readonly List<LookupLink> ActiveClickableLinks = new();
        private LookupLink? HoveredLink = null;
        private string? HoveredCategory = null;

        /// <summary>
        /// Builds the window centered on screen. ": base(...)" calls the parent class's
        /// constructor FIRST with named arguments; Math.Min clamps size so the menu fits
        /// smaller windows with a 24px margin on each side.
        /// </summary>
        public LookupMenu(LookupSubject? initialSubject = null)
            : base(
                x: (Game1.uiViewport.Width - Math.Min(860, Game1.uiViewport.Width - 48)) / 2,
                y: (Game1.uiViewport.Height - Math.Min(680, Game1.uiViewport.Height - 48)) / 2,
                width: Math.Min(860, Game1.uiViewport.Width - 48),
                height: Math.Min(680, Game1.uiViewport.Height - 48),
                showUpperRightCloseButton: true
            )
        {
            // No subject given? Default to the world-overview page. "??" picks the right
            // operand when the left is null.
            CurrentSubject = initialSubject ?? LookupDataManager.BuildWorldOverviewSubject();
            // Vanilla-style click feedback sound.
            Game1.playSound("bigSelect");

            InitializeComponents();
        }

        /// <summary>
        /// OVERRIDE of the base class hook that fires when the game window/resolution
        /// changes. "base.gameWindowSizeChanged(...)" runs the inherited behavior first,
        /// then we recenter and rebuild every component for the new size.
        /// </summary>
        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            width = Math.Min(860, Game1.uiViewport.Width - 48);
            height = Math.Min(680, Game1.uiViewport.Height - 48);
            xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
            yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
            InitializeComponents();
        }

        /// <summary>
        /// (Re)creates every button, the search box, and scroll buttons from current
        /// geometry. Called on construction and after any resize.
        /// </summary>
        private void InitializeComponents()
        {
            // 1. Close Button (top-right, outside inner content)
            // Arguments: screen rect, texture sheet, source rect inside that sheet (a
            // 12x12 red X), and 4f = draw scale (12px sprite -> 48px button).
            CloseButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width - 38, yPositionOnScreen - 6, 48, 48),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );

            // 2. Back Button (top-left)
            int headerTopY = yPositionOnScreen + 30;
            // Arrow sprite from the same cursors sheet at 3.5x scale.
            BackButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + 32, headerTopY + 4, 44, 44),
                Game1.mouseCursors,
                new Rectangle(352, 495, 12, 11),
                3.5f
            );

            // 3. Search Box (top-right header)
            int searchBoxW = 210;
            int searchBoxH = 48;
            // Anchor the box to the window's right edge, 48px inset.
            int searchBoxX = xPositionOnScreen + width - searchBoxW - 48;
            int searchBoxY = headerTopY + 2;

            // The game's built-in TextBox: loads its background texture by content path,
            // uses null for a custom font slot, and renders with the small UI font.
            SearchBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor
            )
            {
                // Object-initializer block sets these properties right after construction.
                X = searchBoxX,
                Y = searchBoxY,
                Width = searchBoxW,
                Height = searchBoxH
            };
            // A plain invisible component used purely for click hit-testing on the box.
            SearchBoxComponent = new ClickableComponent(new Rectangle(searchBoxX, searchBoxY, searchBoxW, searchBoxH), "SearchBox");

            // 4. Content Area Layout & Scroll Buttons
            // Everything below the 104px header band is scrollable body.
            int dividerY = yPositionOnScreen + 104;
            int contentY = dividerY + 18;
            int contentHeight = height - 172;
            int contentBottom = contentY + contentHeight;

            int btnX = xPositionOnScreen + width - 64;

            // Up/down chevron sprites pinned to the right edge of the content area.
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

        /// <summary>
        /// Jumps to a new subject page, remembering the current one on the history stack
        /// and resetting search/scroll state for a clean view.
        /// </summary>
        public void NavigateTo(LookupSubject subject)
        {
            if (CurrentSubject != null)
            {
                // Push the page we're leaving so NavigateBack can restore it later.
                History.Push(CurrentSubject);
            }
            CurrentSubject = subject;
            ScrollOffset = 0;
            if (SearchBox != null)
            {
                // Clear the query AND release keyboard focus back to the game.
                SearchBox.Text = string.Empty;
                SearchBox.Selected = false;
                Game1.keyboardDispatcher.Subscriber = null;
            }
            SearchResults.Clear();
            Game1.playSound("smallSelect");
        }

        /// <summary>
        /// Back navigation: pop the previous page from history; if history is empty,
        /// fall back to the search screen (keeping the box focused for typing).
        /// </summary>
        public void NavigateBack()
        {
            if (History.Count > 0)
            {
                // Pop returns AND removes the most recently pushed subject.
                CurrentSubject = History.Pop();
                ScrollOffset = 0;
                Game1.playSound("smallSelect");
            }
            else if (CurrentSubject != null)
            {
                // No history: switch into search mode by clearing the subject.
                CurrentSubject = null;
                ScrollOffset = 0;
                if (SearchBox != null)
                {
                    SearchBox.Selected = true;
                    // Registering as the keyboard dispatcher's "subscriber" routes all
                    // typed keys into this TextBox.
                    Game1.keyboardDispatcher.Subscriber = SearchBox;
                }
                Game1.playSound("smallSelect");
            }
        }

        /// <summary>
        /// OVERRIDE of the per-frame logic update. The base class animates buttons; we
        /// additionally poll the search text and rerun the search whenever it changed.
        /// </summary>
        public override void update(GameTime time)
        {
            // Run inherited update logic (hover animations etc.) before our additions.
            base.update(time);

            if (SearchBox != null)
            {
                SearchBox.Update();
                // Text changed since last frame? Then re-search. This "compare against
                // cached value" pattern avoids rerunning an expensive search every frame.
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
                        // Empty query: leave search mode entirely.
                        SearchResults.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// OVERRIDE called each frame with the mouse position: updates button hover
        /// animations and records which link/category chip the cursor is over.
        /// </summary>
        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);

            // tryHover scales the sprite up slightly when the mouse is over it.
            // "?." (null-conditional) skips the call when the button doesn't exist yet.
            CloseButton?.tryHover(x, y, 0.2f);
            BackButton?.tryHover(x, y, 0.2f);
            UpButton?.tryHover(x, y, 0.2f);
            DownButton?.tryHover(x, y, 0.2f);

            // Re-detect hovered link from scratch each frame; first hit wins.
            HoveredLink = null;
            foreach (var link in ActiveClickableLinks)
            {
                if (link.Bounds.Contains(x, y))
                {
                    HoveredLink = link;
                    break;
                }
            }

            // Same idea for category filter tabs.
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

        /// <summary>
        /// OVERRIDE handling left clicks. Order matters: specific controls first
        /// (close/back/tabs/scroll/search), then links, and finally clicking outside
        /// the window closes it - a standard menu event-routing pattern.
        /// </summary>
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // Close button: highest priority, handled and consumed immediately.
            if (CloseButton != null && CloseButton.containsPoint(x, y))
            {
                CloseMenu();
                return;
            }

            // Back button (only meaningful with history or an active search).
            if ((History.Count > 0 || (CurrentSubject != null && !string.IsNullOrEmpty(LastSearchText))) && BackButton != null && BackButton.containsPoint(x, y))
            {
                NavigateBack();
                return;
            }

            // Category Tab Buttons
            foreach (var catBtn in CategoryButtons)
            {
                if (catBtn.containsPoint(x, y))
                {
                    // Switching filter re-runs the search for the same query text.
                    CurrentCategory = catBtn.name;
                    SearchResults = LookupDataManager.SearchAll(LastSearchText, CurrentCategory);
                    ScrollOffset = 0;
                    Game1.playSound("smallSelect");
                    return;
                }
            }

            // Scroll buttons jump two steps per click for faster paging.
            if (UpButton != null && UpButton.containsPoint(x, y))
            {
                // Math.Max clamps so the offset can never go negative.
                ScrollOffset = Math.Max(0, ScrollOffset - ScrollStep * 2);
                Game1.playSound("shwip");
                return;
            }

            if (DownButton != null && DownButton.containsPoint(x, y))
            {
                // Math.Min clamps against the maximum computed during Draw.
                ScrollOffset = Math.Min(MaxScrollOffset, ScrollOffset + ScrollStep * 2);
                Game1.playSound("shwip");
                return;
            }

            // Click Search Box
            if (SearchBoxComponent != null && SearchBoxComponent.containsPoint(x, y))
            {
                if (SearchBox != null)
                {
                    // Focus the box so keystrokes start typing into it.
                    SearchBox.Selected = true;
                    Game1.keyboardDispatcher.Subscriber = SearchBox;
                }
                return;
            }
            else
            {
                // Clicked anywhere else while the box was focused -> release focus.
                if (SearchBox != null && SearchBox.Selected)
                {
                    SearchBox.Selected = false;
                    Game1.keyboardDispatcher.Subscriber = null;
                }
            }

            // Click Clickable Link
            // "?." short-circuits to null when no link is hovered; OnClick is the
            // delegate that manufactures the destination subject.
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

        /// <summary>
        /// OVERRIDE: right-click goes BACK through history, or closes the menu entirely
        /// when there's nowhere left to go back to.
        /// </summary>
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

        /// <summary>
        /// OVERRIDE: mouse wheel scrolling. "direction" is positive when rolling up,
        /// negative when rolling down; clamps keep the offset within valid range.
        /// </summary>
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

        /// <summary>
        /// OVERRIDE for gamepad input while the menu is open. "Buttons" is MonoGame's
        /// enum of controller buttons (B, Y, DPad, triggers...).
        /// </summary>
        public override void receiveGamePadButton(Buttons b)
        {
            // Keep vanilla gamepad behavior (snapping between components) first.
            base.receiveGamePadButton(b);

            if (b == Buttons.B)
            {
                // B is "cancel/back" on Xbox-style controllers.
                NavigateBack();
            }
            else if (b == Buttons.Y)
            {
                // Y toggles keyboard focus into/out of the search box.
                if (SearchBox != null)
                {
                    SearchBox.Selected = !SearchBox.Selected;
                    Game1.keyboardDispatcher.Subscriber = SearchBox.Selected ? SearchBox : null;
                }
            }
            else if (b == Buttons.RightThumbstickDown || b == Buttons.DPadDown || b == Buttons.RightTrigger)
            {
                // Three alternative inputs all page downward.
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

        /// <summary>
        /// OVERRIDE for discrete key presses. While search has focus it consumes keys
        /// (Escape blurs, Enter opens the top result); otherwise Escape/Backspace/arrows
        /// navigate. "(Keys)ModEntry.Config.LookupKey" CASTS a config integer into the
        /// System.Windows.Keys enum so players can bind a different close key.
        /// </summary>
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
                    // Enter jumps straight to the best-scoring result.
                    var firstResult = SearchResults[0];
                    var nextSubject = firstResult.OnClick?.Invoke();
                    if (nextSubject != null)
                    {
                        NavigateTo(nextSubject);
                    }
                }
                return;
            }

            // Outside the search box: Escape or the configured lookup key closes.
            if (key == Keys.Escape || key == (Keys)ModEntry.Config.LookupKey)
            {
                CloseMenu();
                return;
            }

            if (key == Keys.Back)
            {
                // Backspace doubles as "go back one page".
                NavigateBack();
                return;
            }

            // WASD-style scrolling alongside the arrow keys.
            if (key == Keys.Up || key == Keys.W)
            {
                ScrollOffset = Math.Max(0, ScrollOffset - ScrollStep);
            }
            else if (key == Keys.Down || key == Keys.S)
            {
                ScrollOffset = Math.Min(MaxScrollOffset, ScrollOffset + ScrollStep);
            }
        }

        /// <summary>
        /// Shuts down cleanly: release keyboard focus, play the closing sound, then let
        /// the base class remove this menu from the game's active-menu slot.
        /// </summary>
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

        /// <summary>
        /// Maps an internal category name to a translated display label. ToLowerInvariant
        /// gives culture-independent lowercasing, safe for building i18n keys like
        /// "lookup.search.category.items". "$"" marks a string with {expressions} inside.
        /// </summary>
        private string GetCategoryDisplayName(string category)
        {
            return ModEntry.I18n.Get($"lookup.search.category.{category.ToLowerInvariant()}").ToString();
        }

        /// <summary>
        /// OVERRIDE: paints the entire menu every frame (games redraw constantly rather
        /// than persisting pixels). Sequence: dark overlay, parchment panel, scrollable
        /// content culled to the viewport, header and masks on top, scrollbar, cursor.
        /// "b" is the SpriteBatch - MonoGame's batched sprite-drawing helper.
        /// </summary>
        public override void draw(SpriteBatch b)
        {
            // Rebuild the clickable-link registry from scratch; Draw is also where each
            // link's final on-screen position becomes known.
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

            // "currentY" is the moving pen: it starts at the top of the content (shifted
            // up by ScrollOffset) and advances as each element is laid out below it.
            int currentY = contentY - ScrollOffset;
            int calculatedContentHeight = 0;

            // 3. DRAW CONTENT VIEWPORT WITH BOUNDS CULLING (NO b.End() or ScissorRasterizer needed)
            // Culling trick: every element checks "would I land inside the visible band?"
            // and skips drawing otherwise - cheaper than GPU clipping machinery.
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
                    // MeasureString reports rendered pixel size BEFORE drawing - needed
                    // to size each tab around its own label.
                    Vector2 catSize = Game1.smallFont.MeasureString(catDisplayName);
                    int catW = (int)catSize.X + 16;
                    // Simple flow layout: when the next tab would overflow the row,
                    // wrap down to a new line (same rule browsers use for words).
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

                        // OrdinalIgnoreCase compares letters by code point, ignoring case -
                        // robust even if the category string came from config.
                        bool isSelected = CurrentCategory.Equals(catName, StringComparison.OrdinalIgnoreCase);
                        bool isHovered = HoveredCategory != null && string.Equals(HoveredCategory, catName, StringComparison.OrdinalIgnoreCase);

                        // Nested ternaries pick three visual states: selected > hovered > idle.
                        Color bg = isSelected ? new Color(180, 100, 30) : (isHovered ? new Color(245, 230, 200) : new Color(230, 210, 175));
                        Color border = isSelected ? new Color(110, 40, 10) : new Color(170, 130, 90);
                        Color txtColor = isSelected ? Color.White : (isHovered ? Color.DarkBlue : Game1.textColor);

                        // staminaRect is a 1x1 white pixel texture; stretching it draws any
                        // flat rectangle. First the fill, then four 1px border strips.
                        b.Draw(Game1.staminaRect, catBounds, bg);
                        b.Draw(Game1.staminaRect, new Rectangle(catBounds.X, catBounds.Y, catBounds.Width, 1), border);
                        b.Draw(Game1.staminaRect, new Rectangle(catBounds.X, catBounds.Bottom - 1, catBounds.Width, 1), border);
                        b.Draw(Game1.staminaRect, new Rectangle(catBounds.X, catBounds.Y, 1, catBounds.Height), border);
                        b.Draw(Game1.staminaRect, new Rectangle(catBounds.Right - 1, catBounds.Y, 1, catBounds.Height), border);

                        // Center the text within the tab by subtracting half its size.
                        int textX = catBounds.X + (catW - (int)catSize.X) / 2;
                        int textY = catBounds.Y + (catHeight - (int)catSize.Y) / 2;
                        Utility.drawTextWithShadow(b, catDisplayName, Game1.smallFont, new Vector2(textX, textY), txtColor);
                    }

                    catX += catW + 6;
                }
                // Account for however many rows of tabs were laid out.
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
                            // Register this visible row as clickable for this frame.
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
                                // "??" falls back to the WHOLE texture when no source
                                // region was specified on the spritesheet.
                                Rectangle src = result.IconSourceRect ?? new Rectangle(0, 0, result.Icon.Width, result.Icon.Height);
                                b.Draw(result.Icon, new Rectangle(itemIconX, currentY + 6, 32, 32), src, Color.White);
                            }

                            int labelX = itemIconX + 42;

                            // Right-aligned Subtitle
                            // Right alignment = anchor at (rightEdge - measured width).
                            int subX = contentX + contentWidth - 36;
                            if (!string.IsNullOrEmpty(result.Subtitle))
                            {
                                Vector2 subSize = Game1.smallFont.MeasureString(result.Subtitle);
                                subX = contentX + contentWidth - 36 - (int)subSize.X;
                                Utility.drawTextWithShadow(b, result.Subtitle, Game1.smallFont, new Vector2(subX, currentY + 10), Color.DarkSlateGray);
                            }

                            // Title with safety truncation if too wide
                            // Shrink one character at a time until the title (plus "...") fits.
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
                        // Append ": " to non-empty labels; ternary keeps empty labels clean.
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
                                // Each chip is sized around its own text (plus icon room).
                                Vector2 textSize = Game1.smallFont.MeasureString(link.Text);
                                int chipWidth = (int)textSize.X + (link.Icon != null ? chipIconSize + 16 : 14) + 12;

                                // Flow-wrap: when the next chip would cross the right
                                // margin, restart at the left on a fresh line.
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
                                        // Vertically center the 24px icon within the chip,
                                        // then push the text start past the icon column.
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

                                // Advance the pen rightward for the next chip.
                                chipX += chipWidth + chipSpacing;
                            }

                            currentY += chipHeight + 14;
                            calculatedContentHeight += chipHeight + 14;
                        }
                        else
                        {
                            // Plain "Label: Value" row. parseText word-wraps long values to
                            // the available width; Math.Max keeps a sane minimum column.
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

                    // Thin separator rule between sections.
                    if (currentY + 20 >= contentY && currentY <= contentBottom)
                    {
                        b.Draw(Game1.staminaRect, new Rectangle(contentX + 6, currentY + 6, contentWidth - 12, 1), Color.SaddleBrown * 0.15f);
                    }
                    currentY += 24;
                    calculatedContentHeight += 24;
                }
            }

            // Now that every element's height is known, compute how far scrolling may go
            // (0 when content fits without scrolling).
            MaxScrollOffset = Math.Max(0, calculatedContentHeight - contentHeight);

            // 4. HEADER BACKGROUND & OVERLAYS (Draw on top of scrolled content for clean visual cutoff)
            // Drawing these AFTER the body hides anything that scrolled underneath -
            // a simple alternative to real clipping/scissor rectangles.
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
                // Available width runs from here to just left of the search box.
                int maxHeaderWidth = (SearchBox != null ? SearchBox.X - 16 : xPositionOnScreen + width - 54) - headerLeftX;

                string title = CurrentSubject.Title;
                // Same character-by-character truncation trick used in search rows.
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

                // Placeholder hint text shown only while the box is empty AND unfocused.
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
                // Thumb height shrinks as content grows (window/total ratio); its
                // position slides down proportionally to the scroll offset. The cast
                // "(float)" forces floating-point division - integer math would yield 0.
                float scrollPct = (float)ScrollOffset / MaxScrollOffset;
                int thumbH = Math.Max(20, (int)(trackH * (float)contentHeight / (contentHeight + MaxScrollOffset)));
                int thumbY = trackY + (int)((trackH - thumbH) * scrollPct);
                b.Draw(Game1.staminaRect, new Rectangle(trackX - 1, thumbY, 8, thumbH), Color.SaddleBrown * 0.8f);
            }

            // Last of all: draw the custom mouse cursor so it sits above everything.
            drawMouse(b);
        }
    }
}
