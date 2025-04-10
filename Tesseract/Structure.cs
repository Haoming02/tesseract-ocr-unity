using System;
using System.Runtime.InteropServices;

namespace OCR
{
    public static partial class Tesseract
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Box
        {
            public int x;
            public int y;
            public int w;
            public int h;
            public int refcount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Boxes
        {
            public int n;
            public int nalloc;
            public int refcount;
            public IntPtr box;
        }
    }
}
