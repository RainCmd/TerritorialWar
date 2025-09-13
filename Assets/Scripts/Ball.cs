using System;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public TextMesh mesh;
    public Collider2D col;
    public Rigidbody2D rb;
    public SpriteRenderer sprite;
    private byte player;
    public byte Player
    {
        get => player;
        set
        {
            player = value;
            if (player < Battle.PlayerColors.Length)
                sprite.color = Battle.PlayerColors[player];
            else
                sprite.color = Color.white;
        }
    }
    private long value = 1;
    public long Value
    {
        get => value;
        set
        {
            this.value = value;
            OnValueChanged();
        }
    }
    private void Awake()
    {
        OnValueChanged();
    }
    private void Update()
    {
        if (mesh)
        {
            mesh.transform.rotation = Quaternion.identity;
            mesh.text = FormatNumber(Value);
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        var rate = collision.collider.GetComponent<Rate>();
        if (rate)
        {
            Value *= rate.rate;
            rate.TriggerAnim();
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.name == "·â¿ÚTrigger")
            if (rb)
                rb.excludeLayers = 0;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "·¢ÉäÆ÷")
            if (rb)
                rb.velocity = Vector2.up * UnityEngine.Random.Range(60, 60);
        var ability = collision.GetComponent<Ability>();
        if (ability && GameManager.Instance)
            GameManager.Instance.OnAbility(this, ability);
    }
    public void Launch(Vector3 position)
    {
        Value = 1;
        transform.position = position;
        rb.excludeLayers = LayerMask.GetMask("Birth");
    }
    private void OnValueChanged()
    {
        transform.localScale = (float)(10 + Math.Log10(Value + 1)) * .3f * Vector3.one;
        if (col)
        {
            if (!col.sharedMaterial)
                col.sharedMaterial = new PhysicsMaterial2D() { name = "<Ball Material>", friction = 0, bounciness = 1 };
            var bounciness = Mathf.Clamp01(10 / (10 + (float)Math.Log10(Value + 1)));
            col.sharedMaterial.bounciness = MathF.Pow(bounciness, 0.5f);
        }
        if (rb)
        {
            rb.mass = 10 + (float)Math.Log10(Value + 1);
        }
    }
    public static string FormatNumber(long num)
    {
        var s = num.ToString();
        if (s.Length <= 3) return s;
        var unitIndex = (s.Length - 1) / 3;
        var part = s[..3];
        if (s.Length % 3 == 0) return part + units[unitIndex];
        return part.Insert(s.Length % 3, ".") + units[unitIndex];
    }
    public static readonly string[] units = new string[] { "", "K", "M", "B", "T", "P", "E", "Z", "Y", "N", "D" };
}
