using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CurePlease
{
    public class DebuffSpell
    {
        public string Name { get; set; }
        public int BuffId { get; set; }
        public int Duration { get; set; }
    }

    public class GroupBoxEx : GroupBox
    {
        private Color _borderColor = Color.Black;

        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //get the text size in groupbox
            Size tSize = TextRenderer.MeasureText(this.Text, this.Font);

            //
            Rectangle borderRect = this.ClientRectangle;
            borderRect.Y = (borderRect.Y + (tSize.Height / 2));
            borderRect.Height = (borderRect.Height - (tSize.Height / 2));
            ControlPaint.DrawBorder(e.Graphics, borderRect, this._borderColor, ButtonBorderStyle.Solid);

            Rectangle textRect = this.ClientRectangle;
            textRect.X = (textRect.X + 6);
            textRect.Width = tSize.Width;
            textRect.Height = tSize.Height;
            e.Graphics.FillRectangle(new SolidBrush(this.BackColor), textRect);
            e.Graphics.DrawString(this.Text, this.Font, new SolidBrush(this.ForeColor), textRect);
        }
    }

    public class NewTabControl : TabControl
    {
        private Color _borderColor = Color.Black;
        private Color _headerColor = Color.White;
        private Color _selectedHeaderColor = Color.LightGray;

        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Invalidate(); }
        }

        public Color HeaderColor
        {
            get { return _headerColor; }
            set { _headerColor = value; Invalidate(); }
        }

        public Color SelectedHeaderColor
        {
            get { return _selectedHeaderColor; }
            set { _selectedHeaderColor = value; Invalidate(); }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            base.OnDrawItem(e);
            Rectangle rect = e.Bounds;
            Graphics g = e.Graphics;
            Font font = this.Font;

            if (e.Index == this.SelectedIndex)
            {
                //This is the selected tab
                g.FillRectangle(new SolidBrush(_selectedHeaderColor), rect);
                TextRenderer.DrawText(g, this.TabPages[e.Index].Text, font, rect, Color.Black);
            }
            else
            {
                //This is an unselected tab
                g.FillRectangle(new SolidBrush(_headerColor), rect);
                TextRenderer.DrawText(g, this.TabPages[e.Index].Text, font, rect, Color.Black);
            }
        }
    }
}