using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tesseract
{
    internal class Wrapper : IDisposable
    {
        private const string TesseractDllName = "libtesseract-4";
        private readonly int minimumConfidence;
        private readonly bool productHighlight;

        private Texture2D _highlightedTexture;
        private IntPtr _tessHandle;

        [DllImport(TesseractDllName)]
        private static extern IntPtr TessVersion();

        [DllImport(TesseractDllName)]
        private static extern IntPtr TessBaseAPICreate();

        [DllImport(TesseractDllName)]
        private static extern int TessBaseAPIInit3(IntPtr handle, string dataPath, string language);

        [DllImport(TesseractDllName)]
        private static extern void TessBaseAPIDelete(IntPtr handle);

        [DllImport(TesseractDllName)]
        private static extern void TessBaseAPISetImage(IntPtr handle, IntPtr imagedata, int width, int height,
            int bytes_per_pixel, int bytes_per_line);

        [DllImport(TesseractDllName)]
        private static extern void TessBaseAPISetImage2(IntPtr handle, IntPtr pix);

        [DllImport(TesseractDllName)]
        private static extern int TessBaseAPIRecognize(IntPtr handle, IntPtr monitor);

        [DllImport(TesseractDllName)]
        private static extern IntPtr TessBaseAPIGetUTF8Text(IntPtr handle);

        [DllImport(TesseractDllName)]
        private static extern void TessDeleteText(IntPtr text);

        [DllImport(TesseractDllName)]
        private static extern void TessBaseAPIEnd(IntPtr handle);

        [DllImport(TesseractDllName)]
        private static extern void TessBaseAPIClear(IntPtr handle);

        [DllImport(TesseractDllName)]
        private static extern IntPtr TessBaseAPIGetWords(IntPtr handle, IntPtr pixa);

        [DllImport(TesseractDllName)]
        private static extern IntPtr TessBaseAPIAllWordConfidences(IntPtr handle);

        public Wrapper(int confidence, bool highlight)
        {
            _tessHandle = IntPtr.Zero;
            minimumConfidence = confidence;
            productHighlight = highlight;
        }

        public static string Version()
        {
            IntPtr strPtr = TessVersion();
            string tessVersion = Marshal.PtrToStringAnsi(strPtr);
            return tessVersion;
        }

        public bool Init(string lang, string dataPath)
        {
            if (!_tessHandle.Equals(IntPtr.Zero))
                Close();

            try
            {
                _tessHandle = TessBaseAPICreate();
                if (_tessHandle.Equals(IntPtr.Zero))
                {
                    Debug.LogError("TessAPICreate failed");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(dataPath))
                {
                    Debug.LogError("Invalid DataPath");
                    return false;
                }

                int init = TessBaseAPIInit3(_tessHandle, dataPath, lang);
                if (init != 0)
                {
                    Close();
                    Debug.LogError($"TessAPIInit failed. Output: {init}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.GetType()}: {e.Message}\n{e.StackTrace}");
                return false;
            }

            return true;
        }

        public async Task<string> Recognize(Texture2D texture)
        {
            if (_tessHandle.Equals(IntPtr.Zero))
                return null;

            int width = texture.width;
            int height = texture.height;
            int count = width * height;

            _highlightedTexture = new Texture2D(width, height);
            _highlightedTexture.SetPixels(texture.GetPixels());
            _highlightedTexture.Apply();

            Color32[] colors = _highlightedTexture.GetPixels32();

            const int bytesPerPixel = 4;
            byte[] dataBytes = new byte[count * bytesPerPixel];
            int bytePtr = 0;

            await Task.Run(() =>
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int colorIdx = y * width + x;
                        dataBytes[bytePtr++] = colors[colorIdx].r;
                        dataBytes[bytePtr++] = colors[colorIdx].g;
                        dataBytes[bytePtr++] = colors[colorIdx].b;
                        dataBytes[bytePtr++] = colors[colorIdx].a;
                    }
                }
            });

            IntPtr imagePtr = IntPtr.Zero;

            bool success = await Task.Run(() =>
            {
                imagePtr = Marshal.AllocCoTaskMem(count * bytesPerPixel);
                Marshal.Copy(dataBytes, 0, imagePtr, count * bytesPerPixel);

                TessBaseAPISetImage(_tessHandle, imagePtr, width, height, bytesPerPixel, width * bytesPerPixel);

                return TessBaseAPIRecognize(_tessHandle, IntPtr.Zero) == 0;
            });

            Marshal.FreeCoTaskMem(imagePtr);
            if (!success)
                return null;

            IntPtr confidencesPointer = TessBaseAPIAllWordConfidences(_tessHandle);
            int i = 0;
            List<int> confidence = new List<int>();

            while (true)
            {
                int tempConfidence = Marshal.ReadInt32(confidencesPointer, i * 4);

                if (tempConfidence < 0) break;

                i++;
                confidence.Add(tempConfidence);
            }


            if (productHighlight)
            {
                int pointerSize = Marshal.SizeOf(typeof(IntPtr));
                IntPtr intPtr = TessBaseAPIGetWords(_tessHandle, IntPtr.Zero);
                Boxa boxa = Marshal.PtrToStructure<Boxa>(intPtr);
                Box[] boxes = new Box[boxa.n];

                for (int index = 0; index < boxes.Length; index++)
                {
                    if (confidence[index] >= minimumConfidence)
                    {
                        IntPtr boxPtr = Marshal.ReadIntPtr(boxa.box, index * pointerSize);
                        boxes[index] = Marshal.PtrToStructure<Box>(boxPtr);
                        Box box = boxes[index];
                        DrawLines(ref _highlightedTexture,
                            new Rect(box.x, _highlightedTexture.height - box.y - box.h, box.w, box.h),
                            Color.green);
                    }
                }
            }

            IntPtr stringPtr = TessBaseAPIGetUTF8Text(_tessHandle);
            if (stringPtr.Equals(IntPtr.Zero))
                return null;

            string recognizedText = Marshal.PtrToStringAnsi(stringPtr);

            TessBaseAPIClear(_tessHandle);
            TessDeleteText(stringPtr);

            if (minimumConfidence < 0)
                return recognizedText;

            string[] words = recognizedText.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder result = new StringBuilder();

            for (i = 0; i < words.Length; i++)
            {
#if UNITY_EDITOR
                Debug.Log($"[{confidence[i]}] {words[i]}");
#endif
                if (confidence[i] >= minimumConfidence)
                {
                    result.Append(words[i]);
                    result.Append(" ");
                }
            }

            return result.ToString();
        }

        private void DrawLines(ref Texture2D texture, Rect boundingRect, Color color, int thickness = 3)
        {
            int x1 = (int)boundingRect.x;
            int x2 = (int)(boundingRect.x + boundingRect.width);
            int y1 = (int)boundingRect.y;
            int y2 = (int)(boundingRect.y + boundingRect.height);

            for (int x = x1; x <= x2; x++)
            {
                for (int i = 0; i < thickness; i++)
                {
                    texture.SetPixel(x, y1 + i, color);
                    texture.SetPixel(x, y2 - i, color);
                }
            }

            for (int y = y1; y <= y2; y++)
            {
                for (int i = 0; i < thickness; i++)
                {
                    texture.SetPixel(x1 + i, y, color);
                    texture.SetPixel(x2 - i, y, color);
                }
            }

            texture.Apply();
        }

        public Texture2D GetHighlightedTexture()
        {
            if (!productHighlight)
                return null;
            return _highlightedTexture;
        }

        public void Dispose()
        {
            Close();
        }

        private void Close()
        {
            if (_tessHandle.Equals(IntPtr.Zero))
                return;

            TessBaseAPIEnd(_tessHandle);
            TessBaseAPIDelete(_tessHandle);
            _tessHandle = IntPtr.Zero;
        }
    }
}
