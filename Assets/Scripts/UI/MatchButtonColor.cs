using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchButtonColor: MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private Button button;
    private Image background;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        button = transform.parent.GetComponent<Button>();
        background = transform.parent.Find("Background")?.GetComponent<Image>();
    }

    private void Update()
    {
        var bc = button.targetGraphic.canvasRenderer.GetColor();
        textMesh.faceColor = bc;
        if (background)
        {
            background.color = new Color(bc.r,bc.b,bc.g,background.color.a);
        }
    }
}