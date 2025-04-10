using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OCR
{
    public static partial class Tesseract
    {
        public static async Task<string> Recognize(Texture2D image, bool verbose = false)
        {
            if (!_init)
            {
                Debug.LogError("Tesseract has not been Initialized...");
                return string.Empty;
            }

            int width = image.width;
            int height = image.height;
            int pixelCount = width * height;

            const int RGB = 3;
            int idx = 0;

            Color32[] pixels = image.GetPixels32();
            byte[] dataBytes = new byte[pixelCount * RGB];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int colorIdx = (height - y - 1) * width + x;
                    dataBytes[idx++] = pixels[colorIdx].r;
                    dataBytes[idx++] = pixels[colorIdx].g;
                    dataBytes[idx++] = pixels[colorIdx].b;
                }
            }

            IntPtr imagePtr = IntPtr.Zero;
            imagePtr = Marshal.AllocCoTaskMem(dataBytes.Length);
            Marshal.Copy(dataBytes, 0, imagePtr, dataBytes.Length);
            API.TessBaseAPISetImage(_tessHandle, imagePtr, width, height, RGB, width * RGB);
            Marshal.FreeCoTaskMem(imagePtr);

            int status = await Task.Run(() => API.TessBaseAPIRecognize(_tessHandle, IntPtr.Zero)).ConfigureAwait(true);
            if (status != 0)
                return null;

            IntPtr stringPtr = API.TessBaseAPIGetUTF8Text(_tessHandle);
            if (stringPtr == IntPtr.Zero)
                return null;

            IntPtr confidencesPointer = API.TessBaseAPIAllWordConfidences(_tessHandle);
            var confidences = new List<int>();
            idx = 0;

            while (true)
            {
                int conf = Marshal.ReadInt32(confidencesPointer, idx * 4);
                if (conf < 0) break;

                confidences.Add(conf);
                idx++;
            }

            if (highlight)
            {
                if (_highlightedTexture != null) UnityEngine.Object.DestroyImmediate(_highlightedTexture);
                _highlightedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                _highlightedTexture.SetPixels32(pixels);
                _highlightedTexture.Apply();

                int pointerSize = Marshal.SizeOf(typeof(IntPtr));
                IntPtr intPtr = API.TessBaseAPIGetWords(_tessHandle, IntPtr.Zero);
                Boxes boxa = Marshal.PtrToStructure<Boxes>(intPtr);

                for (idx = 0; idx < boxa.n; idx++)
                {
                    if (confidences[idx] < confidense)
                        continue;

                    IntPtr boxPtr = Marshal.ReadIntPtr(boxa.box, idx * pointerSize);
                    Box box = Marshal.PtrToStructure<Box>(boxPtr);
                    DrawLines(
                        ref _highlightedTexture,
                        new Rect(box.x, _highlightedTexture.height - box.y - box.h, box.w, box.h),
                        Color.green
                    );
                }
            }

            API.TessBaseAPIClear(_tessHandle);
            string recognizedText = Marshal.PtrToStringAnsi(stringPtr);
            API.TessDeleteText(stringPtr);

            if (confidense < 0)
                return recognizedText;

            string[] words = recognizedText.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();

            for (idx = 0; idx < words.Length; idx++)
            {
                if (verbose) Debug.Log($"[{confidences[idx]}] {words[idx]}");
                if (confidences[idx] >= confidense)
                {
                    result.Append(words[idx]);
                    result.Append(" ");
                }
            }

            return result.ToString();
        }
    }
}
