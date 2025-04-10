using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OCR
{
    public static partial class Tesseract
    {
        private static bool _init = false;
        private static IntPtr _tessHandle = IntPtr.Zero;

        private static Texture2D _highlightedTexture = null;
        private static int confidense;
        private static bool highlight;

        public static void Init(string lang, int minimumConfidence = 0, bool createHighlight = false, Action onSetupComplete = null)
        {
            if (_init)
            {
                Debug.LogError("Init can only be called once...");
                return;
            }

            string datapath = Path.Combine(Application.streamingAssetsPath, "tessdata");
            if (!Directory.Exists(datapath))
            {
                Debug.LogError("\"tessdata\" folder does not exist...");
                return;
            }

            confidense = minimumConfidence;
            highlight = createHighlight;

            try
            {
                _tessHandle = API.TessBaseAPICreate();
                if (_tessHandle == IntPtr.Zero)
                {
                    Debug.LogError("TessBaseAPICreate Failed...");
                    return;
                }

                int status = API.TessBaseAPIInit3(_tessHandle, datapath, lang);
                if (status != 0)
                {
                    Debug.LogError($"TessBaseAPIInit3 Failed... (Error Code: {status})");
                    CleanUp();
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during Initialization: {e.Message}\n{e.StackTrace}");
                return;
            }

            Application.quitting += CleanUp;
            onSetupComplete?.Invoke();
            _init = true;

#if UNITY_EDITOR
            Debug.Log("Init Successful!");
#endif
        }

        public static Texture2D GetHighlight()
        {
            if (!_init)
            {
                Debug.LogError("Tesseract has not been Initialized...");
                return null;
            }

            if (!highlight)
            {
                Debug.LogError("Highlight was not enabled...");
                return null;
            }

            if (_highlightedTexture == null)
            {
                Debug.LogError("No image has been processed yet...");
                return null;
            }

            return _highlightedTexture;
        }

        public static bool GetVersion(out string ver)
        {
            try
            {
                IntPtr strPtr = API.TessVersion();
                ver = Marshal.PtrToStringAnsi(strPtr);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get version info: {e.Message}\n{e.StackTrace}");
                ver = string.Empty;
                return false;
            }
        }

        private static void DrawLines(ref Texture2D texture, Rect boundingRect, Color color)
        {
            int x1 = (int)boundingRect.x;
            int x2 = (int)(boundingRect.x + boundingRect.width);
            int y1 = (int)boundingRect.y;
            int y2 = (int)(boundingRect.y + boundingRect.height);

            for (int x = x1; x <= x2; x++)
            {
                texture.SetPixel(x, y1, color);
                texture.SetPixel(x, y2, color);
            }

            for (int y = y1; y <= y2; y++)
            {
                texture.SetPixel(x1, y, color);
                texture.SetPixel(x2, y, color);
            }

            texture.Apply();
        }

        private static void CleanUp()
        {
            if (!_init) return;

            API.TessBaseAPIEnd(_tessHandle);
            API.TessBaseAPIDelete(_tessHandle);
            _tessHandle = IntPtr.Zero;
        }
    }
}
