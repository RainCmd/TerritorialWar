using UnityEngine;
using UnityEngine.UI;

public class PlayerInfo : MonoBehaviour
{
    public Text text;
    public void SetValue(long hpValue, int territory, long bulletValue, long laserValue)
    {
        gameObject.SetActive(hpValue > 0);
        text.text = "HP:" + Ball.FormatNumber(hpValue);
        text.text += "\n����:" + territory.ToString();
        if (bulletValue > 0)
        {
            text.text += "\nɨ��:" + bulletValue.ToString();
        }
        if (laserValue > 0)
        {
            text.text += "\n����:" + laserValue.ToString();
        }
    }
}
