using UnityEngine;

public enum AbilityType
{
    Strafe, //É¨Éä
    HP,     //¼ÓÑª
    Ball,   //¼ÓÇò
    Snipe,  //¾Ñ»÷
    Scatter,   //É¢²¼
    Laser,    //¼¤¹â
}
public class Ability : MonoBehaviour
{
    public AbilityType abilityType;
}
