using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Utils.Misc.Forms
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public static readonly RECT Empty = new RECT();


        private int left, top, right, bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }
        public RECT(RECT rcSrc)
        {
            this.left = rcSrc.left;
            this.top = rcSrc.top;
            this.right = rcSrc.right;
            this.bottom = rcSrc.bottom;
        }


        public int Width
        {
            get { return Math.Abs(right - left); }  
        }


        public int Height
        {
            get { return bottom - top; }
        }

        public bool IsEmpty
        {
            get
            {
                return left >= right || top >= bottom;
            }
        }

        public int Left { get => left; set => left = value; }
        public int Top { get => top; set => top = value; }
        public int Right { get => right; set => right = value; }
        public int Bottom { get => bottom; set => bottom = value; }

        public override bool Equals(object obj)
        {
            if (!(obj is RECT)) { return false; }
            return (this == (RECT)obj);
        }

        public override int GetHashCode()
        {
            return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
        }


        public static bool operator ==(RECT rect1, RECT rect2)
        {
            return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom);
        }

        public static bool operator !=(RECT rect1, RECT rect2)
        {
            return !(rect1 == rect2);
        }

    }
}
