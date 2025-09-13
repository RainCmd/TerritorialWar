using UnityEngine;
using UnityEngine.UI;

public class PlayerInfo : MonoBehaviour
{
    public Text text;
    public void SetValue(long hpValue, long bulletValue)
    {
        gameObject.SetActive(hpValue > 0);
        text.text = "HP:" + Ball.FormatNumber(hpValue);
        if(bulletValue > 0)
        {
            text.text += "\n…®…‰:" + bulletValue.ToString();
        }
    }
}
