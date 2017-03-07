using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Ships
{
    class Ship
    {
        public int Size { get; set; }
        public int XCellCoords { get; set; }
        public int YCellCoords { get; set; }
        public bool IsHorizontal { get; set; }
        public Rectangle Rect { get; set; }

        public Ship()
        {
            Rect = new Rectangle();
        }
    }
}
