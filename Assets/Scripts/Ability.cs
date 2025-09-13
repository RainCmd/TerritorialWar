using UnityEngine;

public enum AbilityType
{
    Strafe, //É¨Éä
    HP,     //¼ÓÑª
    Ball,   //¼ÓÇò
    Snipe,  //¾Ñ»÷
    Scatter,   //É¢²¼
    HeliumFlash, //º¤ÉÁ
}
public class Ability : MonoBehaviour
{
    public AbilityType abilityType;
}
