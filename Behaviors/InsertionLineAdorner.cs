using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;

namespace TOLTECH_APPLICATION.Behaviors
{
    public class InsertionLineAdorner : Adorner
    {
        private readonly bool _insertAbove;
        private readonly Pen _pen;

        public InsertionLineAdorner(UIElement adornedElement, bool insertAbove)
            : base(adornedElement)
        {
            _insertAbove = insertAbove;
            _pen = new Pen(new SolidColorBrush(Color.FromRgb(0, 120, 215)), 2);
            _pen.Freeze();
        }

        protected override void OnRender(DrawingContext dc)
        {
            var adornedElementRect = new Rect(this.AdornedElement.RenderSize);
            double y = _insertAbove ? 0 : adornedElementRect.Bottom;
            dc.DrawLine(_pen, new Point(0, y), new Point(adornedElementRect.Right, y));
        }
    }
}
