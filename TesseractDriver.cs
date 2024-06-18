using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Tesseract
{
    public static class Driver
    {
        private const string tessdata = "tessdata";
        private static Wrapper _tesseract = null;

        public static bool Init(string lang, int confidence, bool highlight = false, Action onSetupComplete = null)
        {
            string datapath = Path.Combine(Application.streamingAssetsPath, tessdata);

            _tesseract = new Wrapper(confidence, highlight);
            if (_tesseract.Init(lang, datapath))
            {
#if UNITY_EDITOR
                Debug.Log("Init Successful!");
#endif
                Application.quitting += () => _tesseract.Dispose();
                onSetupComplete?.Invoke();
                return true;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("Init Unsuccessful...");
#endif
                return false;
            }
        }

        public static async Task<string> Recognize(Texture2D imageToRecognize)
        {
            if (_tesseract == null)
            {
                Debug.LogError("Tesseract not Initialized...");
                return string.Empty;
            }

            return await _tesseract.Recognize(imageToRecognize);
        }

        public static Texture2D GetHighlightedTexture()
        {
            if (_tesseract == null)
            {
                Debug.LogError("Tesseract not Initialized...");
                return null;
            }

            return _tesseract.GetHighlightedTexture();
        }

        public static string GetVersion()
        {
            try { return $"Tesseract version: {Wrapper.Version()}"; }
            catch (Exception e) { return $"{e.GetType()}: {e.Message}\n{e.StackTrace}"; }
        }
    }
}
