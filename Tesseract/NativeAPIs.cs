using System;
using System.Runtime.InteropServices;

namespace OCR
{
    public static partial class Tesseract
    {
        private static class API
        {
            private const string TesseractDLL = "libtesseract-4";

            [DllImport(TesseractDLL)]
            public static extern IntPtr TessVersion();

            [DllImport(TesseractDLL)]
            public static extern IntPtr TessBaseAPICreate();

            [DllImport(TesseractDLL)]
            public static extern int TessBaseAPIInit3(IntPtr handle, string dataPath, string language);

            [DllImport(TesseractDLL)]
            public static extern void TessBaseAPIDelete(IntPtr handle);

            [DllImport(TesseractDLL)]
            public static extern void TessBaseAPISetImage(IntPtr handle, IntPtr imagedata, int width, int height, int bytes_per_pixel, int bytes_per_line);

            [DllImport(TesseractDLL)]
            public static extern int TessBaseAPIRecognize(IntPtr handle, IntPtr monitor);

            [DllImport(TesseractDLL)]
            public static extern void TessDeleteText(IntPtr text);

            [DllImport(TesseractDLL)]
            public static extern void TessBaseAPIEnd(IntPtr handle);

            [DllImport(TesseractDLL)]
            public static extern void TessBaseAPIClear(IntPtr handle);

            [DllImport(TesseractDLL)]
            public static extern IntPtr TessBaseAPIGetUTF8Text(IntPtr handle);

            [DllImport(TesseractDLL)]
            public static extern IntPtr TessBaseAPIGetWords(IntPtr handle, IntPtr pixa);

            [DllImport(TesseractDLL)]
            public static extern IntPtr TessBaseAPIAllWordConfidences(IntPtr handle);
        }
    }
}
