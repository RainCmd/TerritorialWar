using UnityEngine;
using UnityEngine.UI;

public class PlayerInfo : MonoBehaviour
{
    public Text text;
    public void SetValue(long hpValue, long bulletValue, long laserValue)
    {
        gameObject.SetActive(hpValue > 0);
        text.text = "HP:" + Ball.FormatNumber(hpValue);
        if (bulletValue > 0)
        {
            text.text += "\nÉ¨Éä:" + bulletValue.ToString();
        }
        if (laserValue > 0)
        {
            text.text += "\n¼¤¹â:" + laserValue.ToString();
        }
    }
}
