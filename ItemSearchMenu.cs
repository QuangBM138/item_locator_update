﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using Find_Item.Utilities;

namespace Find_Item
{


    /// <summary>A simple search UI which mimics Lookup Anything's search menu but only for items.</summary>
    public class ItemSearchMenu : IClickableMenu
    {
        private List<ItemSubject> AllSubjects;
        private List<ItemSubject> FilteredSubjects;
        private TextBox SearchBox;
        private Action<Item> OnSelect;
        private int targetScrollOffset = 0;
        private float smoothScrollOffset = 0;
        private const int ItemHeight = 100; // Increased from 40
        private const string SearchHintText = "Type to search...";

        // New fields for dragging scroll.
        private bool isDraggingScroll = false;
        private int lastDragY = 0;

        // Add an additional parameter (with a default empty string) to the constructor.
        public ItemSearchMenu(List<ItemSubject> subjects, Action<Item> onSelect, string initialQuery = "")
            : base(
                  GetViewportAdjustedX(),
                  GetViewportAdjustedY(),
                  GetViewportAdjustedWidth(),
                  GetViewportAdjustedHeight(),
                  true) // showUpperCloseButton
        {
            this.AllSubjects = subjects;
            this.FilteredSubjects = subjects;
            this.OnSelect = onSelect;

            // Initialize a simple TextBox for search input.
            Texture2D textBoxTexture = Game1.content.Load<Texture2D>("LooseSprites\\textBox");
            this.SearchBox = new TextBox(textBoxTexture, null, Game1.smallFont, Game1.textColor)
            {
                X = this.xPositionOnScreen + 20,
                Y = this.yPositionOnScreen + 20,
                Width = this.width - 40,
                Height = 40,
                Text = initialQuery,  // Set the initial search query.
            };
            this.SearchBox.Selected = true; // Automatically select the textbox for input
            Game1.keyboardDispatcher.Subscriber = this.SearchBox; // Ensure the textbox is the keyboard subscriber

        }

        // Add helper methods for zoom-adjusted positioning
        private static int GetViewportAdjustedX()
        {
            return Game1.uiViewport.Width / 4;
        }

        private static int GetViewportAdjustedY()
        {
            return Game1.uiViewport.Height / 4;
        }

        private static int GetViewportAdjustedWidth()
        {
            return Game1.uiViewport.Width / 2;
        }

        private static int GetViewportAdjustedHeight()
        {
            return Game1.uiViewport.Height / 2;
        }

        // Update the gameWindowSizeChanged override to pass along the current search query.
        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            // Preserve the current search text.
            string currentQuery = this.SearchBox.Text;
            // Create a new instance of the menu with the stored query.
            Game1.activeClickableMenu = new ItemSearchMenu(this.AllSubjects, this.OnSelect, currentQuery);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // Check each subject row.
            int startY = this.SearchBox.Y + this.SearchBox.Height + 10;
            for (int i = 0; i < this.FilteredSubjects.Count; i++)
            {
                Rectangle r = new Rectangle(this.xPositionOnScreen + 20, startY + i * ItemHeight - (int)smoothScrollOffset, this.width - 40, ItemHeight);
                if (r.Contains(x, y))
                {
                    this.OnSelect(this.FilteredSubjects[i].Item);
                    this.exitThisMenu();
                    break;
                }
            }
            base.receiveLeftClick(x, y, playSound);
        }

        // New override to handle drag scrolling.
        public override void leftClickHeld(int x, int y)
        {
            int listStartY = this.SearchBox.Y + this.SearchBox.Height + 10;
            Rectangle scrollRect = new Rectangle(this.xPositionOnScreen + 20, listStartY, this.width - 40, this.height - (listStartY - this.yPositionOnScreen));
            if (scrollRect.Contains(x, y))
            {
                if (!isDraggingScroll)
                {
                    isDraggingScroll = true;
                    lastDragY = y;
                }
                else
                {
                    int delta = y - lastDragY;
                    lastDragY = y;
                    int totalContentHeight = this.FilteredSubjects.Count * ItemHeight;
                    int visibleHeight = this.height - ((this.SearchBox.Y + this.SearchBox.Height + 10) - this.yPositionOnScreen);
                    int maxScroll = Math.Max(0, totalContentHeight - visibleHeight);
                    targetScrollOffset = Math.Max(0, Math.Min(targetScrollOffset - delta, maxScroll));
                }
            }
            base.leftClickHeld(x, y);
        }

        public override void releaseLeftClick(int x, int y)
        {
            isDraggingScroll = false;
            base.releaseLeftClick(x, y);
        }

        public override void receiveScrollWheelAction(int direction)
        {
            // Compute total content height and visible height.
            int totalContentHeight = this.FilteredSubjects.Count * ItemHeight;
            int visibleHeight = this.height - ((this.SearchBox.Y + this.SearchBox.Height + 10) - this.yPositionOnScreen);
            int maxScroll = Math.Max(0, totalContentHeight - visibleHeight);
            // Use a lower scroll increment (2 instead of 5) for even slower scrolling.
            int scrollIncrement = 2;
            targetScrollOffset = Math.Max(0, Math.Min(targetScrollOffset - direction * scrollIncrement, maxScroll));
        }

        public override void update(GameTime time)
        {
            base.update(time);
            this.SearchBox.Update();

            // Filter subjects based on the search query.
            string query = this.SearchBox.Text.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(query))
                this.FilteredSubjects = this.AllSubjects;
            else
                this.FilteredSubjects = this.AllSubjects.Where(s => s.Name.ToLowerInvariant().Contains(query)).ToList();

            // Clamp targetScrollOffset based on updated filtered items.
            int totalContentHeight = this.FilteredSubjects.Count * ItemHeight;
            int visibleHeight = this.height - ((this.SearchBox.Y + this.SearchBox.Height + 10) - this.yPositionOnScreen);
            int maxScroll = Math.Max(0, totalContentHeight - visibleHeight);
            targetScrollOffset = Math.Max(0, Math.Min(targetScrollOffset, maxScroll));

            // Smoothly interpolate the scroll offset for a smooth scrolling effect.
            smoothScrollOffset = MathHelper.Lerp(smoothScrollOffset, targetScrollOffset, 0.1f);
        }

        public override void draw(SpriteBatch b)
        {
            // Sửa phần vẽ background để sử dụng uiViewport thay vì viewport
            b.Draw(
                Game1.fadeToBlackRect,
        new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * 0.75f
            );

            // Draw the menu box.
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                this.xPositionOnScreen,
                this.yPositionOnScreen,
                this.width,
                this.height,
                Color.White,
                1f,
                drawShadow: true);

            // Draw the search box.
            this.SearchBox.Draw(b);

            // Prepare to clip the list of filtered items.
            int listStartY = this.SearchBox.Y + this.SearchBox.Height + 10;
            // The clipping rectangle covers the remaining area exactly without extra margin.
            Rectangle clipRect = new Rectangle(
                this.xPositionOnScreen + 20,
                listStartY,
                this.width - 40,
                this.height - (listStartY - this.yPositionOnScreen));

            // End the current sprite batch to set the scissor rectangle.
            b.End();
            RasterizerState rState = new RasterizerState { ScissorTestEnable = true };
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, rState);
            Game1.graphics.GraphicsDevice.ScissorRectangle = clipRect;

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            for (int i = 0; i < this.FilteredSubjects.Count; i++)
            {
                Rectangle r = new Rectangle(this.xPositionOnScreen + 20, listStartY + i * ItemHeight - (int)smoothScrollOffset, this.width - 40, ItemHeight);
                bool hovered = r.Contains(mouseX, mouseY);

                // Draw hover background effect
                if (hovered)
                {
                    // Draw semi-transparent white background for hover effect
                    b.Draw(Game1.staminaRect, r, Color.White * 0.15f);
                }

                // Draw item icon using ItemDrawHelper with larger scale
                Item currentItem = this.FilteredSubjects[i].Item;
                float itemScale = 2.5f; // Increased from 1.5f
                Vector2 iconPos = new Vector2(r.X, r.Y + (r.Height - 16 * itemScale) / 2);
                ItemDrawHelper.DrawItemIcon(b, currentItem, iconPos, itemScale);

                // Get icon width using helper
                float iconWidth = ItemDrawHelper.GetIconWidth(itemScale);
                Vector2 textSize = Game1.smallFont.MeasureString(this.FilteredSubjects[i].Name);
                Vector2 pos = new Vector2(r.X + iconWidth, r.Y + (r.Height - textSize.Y) / 2);

                // Draw item name with hover effect
                Color textColor = hovered ? Color.White : Color.Black;
                float textAlpha = hovered ? 1f : 0.9f; // Slight fade for non-hovered items
                b.DrawString(Game1.smallFont, this.FilteredSubjects[i].Name, pos, textColor * textAlpha);

                // Draw separator line between items (except for the last item)
                if (i < this.FilteredSubjects.Count - 1)
                {
                    int lineY = r.Bottom - 1;
                    Color lineColor = Color.Gray * 0.3f; // Light gray, semi-transparent line
                    b.Draw(Game1.staminaRect,
                        new Rectangle(r.X, lineY, r.Width, 1),
                        lineColor);
                }
            }

            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            if (string.IsNullOrEmpty(this.SearchBox.Text))
            {
                // Draw hint text
                Vector2 hintPos = new Vector2(this.SearchBox.X + 10, this.SearchBox.Y + 10);
                b.DrawString(Game1.smallFont, SearchHintText, hintPos, Color.Gray * 0.6f);
            }

            // Draw the upper-right close button.
            base.draw(b);
            this.drawMouse(b);
        }

        public override void receiveKeyPress(Keys key)
        {
            // Only exit if Escape is pressed; allow all other keys (e.g., "e") to be processed by the textbox.
            if (key == Keys.Escape)
                this.exitThisMenu();
        }


    }
}