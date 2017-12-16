using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeper
{
    public class Widget
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Pad { get; set; }

        public Widget(int Left= 0, int Top = 0, int Pad = 0)
        {
            this.Left = Left;
            this.Top = Top;
            this.Pad = Pad;
        }
    }
}
