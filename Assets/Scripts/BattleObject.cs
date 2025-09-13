using UnityEngine;
using UnityEngine.UI;

public class BattleObject : MonoBehaviour
{
    public Text valueText;
    public void SetInfo(ObjectInfo info)
    {
        valueText.text = Ball.FormatNumber(info.value);
        valueText.transform.localScale = 0.075f * Mathf.Max(Mathf.Log(info.value + 1, 2), 1) * Vector3.one;
        var rt = (RectTransform)transform.parent;
        transform.localPosition = new Vector3(rt.rect.width * info.x, rt.rect.height * info.y);
    }
}
