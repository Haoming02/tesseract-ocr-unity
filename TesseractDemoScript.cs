using UnityEngine;
using UnityEngine.UI;

public class TesseractDemoScript : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Remember to Enable Read/Write")]
    private Texture2D imageToRecognize;

    [SerializeField]
    private RawImage displayResult;

    [SerializeField]
    private Text displayText;

    public enum Language { EN, JP, ZH };

    [SerializeField]
    private Language lang;

    void Start()
    {
        Debug.Log(Tesseract.Driver.GetVersion());

        switch (lang)
        {
            case Language.EN:
                Tesseract.Driver.Init("eng", 72, true);
                break;
            case Language.JP:
                Tesseract.Driver.Init("jpn", -1, true);
                break;
            case Language.ZH:
                Tesseract.Driver.Init("chi_tra", -1, true);
                break;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            Recoginze();
    }

    private async void Recoginze()
    {
        displayText.text = await Tesseract.Driver.Recognize(imageToRecognize);
        displayResult.texture = Tesseract.Driver.GetHighlightedTexture();
    }
}
