using UnityEngine;

public enum AbilityType
{
    Strafe, //ɨ��
    HP,     //��Ѫ
    Ball,   //����
    Snipe,  //�ѻ�
    Scatter,   //ɢ��
    Laser,    //����
}
public class Ability : MonoBehaviour
{
    public AbilityType abilityType;
}
