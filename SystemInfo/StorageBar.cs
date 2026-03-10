using System;
using Terminal.Gui;

namespace SystemInfo
{
    /// <summary>
    /// A horizontal two-color bar: red background = used, green background = free.
    /// Set <see cref="Fraction"/> to a value between 0.0 (empty) and 1.0 (full/used).
    /// </summary>
    class StorageBar : View
    {
        private double _fraction;

        public double Fraction
        {
            get => _fraction;
            set
            {
                _fraction = Math.Max(0.0, Math.Min(1.0, value));
                SetNeedsDisplay();
            }
        }

        public StorageBar() : base()
        {
            CanFocus = false;
        }

        public override void Redraw(Rect bounds)
        {
            if (bounds.Width <= 0)
                return;

            int usedCells = (int)Math.Round(bounds.Width * _fraction);
            usedCells = Math.Max(0, Math.Min(bounds.Width, usedCells));
            int freeCells = bounds.Width - usedCells;

            var usedAttr = Application.Driver.MakeAttribute(Color.White, Color.Red);
            var freeAttr = Application.Driver.MakeAttribute(Color.Black, Color.Green);

            // Used portion (red background)
            Application.Driver.SetAttribute(usedAttr);
            for (int x = 0; x < usedCells; x++)
                AddRune(x, 0, (System.Rune)'█');

            // Free portion (green background)
            Application.Driver.SetAttribute(freeAttr);
            for (int x = usedCells; x < usedCells + freeCells; x++)
                AddRune(x, 0, (System.Rune)'░');
        }
    }
}
