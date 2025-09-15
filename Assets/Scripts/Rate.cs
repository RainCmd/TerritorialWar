using UnityEngine;

public enum RateType
{
    Mul,
    Add
}
public class Rate : MonoBehaviour
{
    public int rate;
    public RateType type;
    private Vector3 localScale;
    public float animTime;
    private float time;
    [ContextMenu("TriggerAnim")]
    public void TriggerAnim() => time = 0;
    private void Awake()
    {
        localScale = transform.localScale;
    }
    private void Update()
    {
        if (time < animTime)
        {
            time += Time.deltaTime;
            var t = Mathf.Clamp01(time / animTime);
            t = 1 - Mathf.Pow(1 - t, 2);
            transform.localScale = localScale * (1 + 0.1f * Mathf.Sin(t * Mathf.PI * 4));
        }
    }
    public long GetValue(long value)
    {
        switch (type)
        {
            case RateType.Mul:
                value *= rate;
                break;
            case RateType.Add:
                value += rate;
                break;

        }
        return value;
    }
}
