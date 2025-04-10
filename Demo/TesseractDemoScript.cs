using OCR;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TesseractDemoScript : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private RawImage displayInput;
    [SerializeField]
    private TMP_Text displayText;
    [SerializeField]
    private RawImage displayOutput;

    [Header("Option")]
    [SerializeField]
    private Texture2D imageToRecognize;
    [SerializeField]
    private Language detectionLanguage;

    public enum Language
    {
        EN,
        JP,
        // ZH
    };

    void Start()
    {
        displayInput.texture = imageToRecognize;

        if (Tesseract.GetVersion(out string ver))
            Debug.Log(ver);

        switch (detectionLanguage)
        {
            case Language.EN:
                Tesseract.Init("eng", 64, true);
                break;
            case Language.JP:
                Tesseract.Init("jpn", -1, true);
                break;
                // case Language.ZH:
                //     Tesseract.Init("chi_tra", -1, true);
                //     break;
        }
    }

    public async void Process()
    {
        displayText.text = await Tesseract.Recognize(imageToRecognize);
        displayOutput.texture = Tesseract.GetHighlight();
    }
}
