using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public RawImage battleTerritory;
    public RawImage battleBullet;
    private Battle battle;
    private void Awake()
    {
        battle = new Battle(256, 256);
        var tex = new Texture2D(battle.width, battle.height) { name = "ÁìÍÁtex" };
        tex.SetPixels32(battle.rendererTerritory);
        tex.Apply();
        battleTerritory.texture = tex;
        tex = new Texture2D(battle.width, battle.height) { name = "×Óµ¯tex" };
        tex.SetPixels32(battle.rendererBullet);
        tex.Apply();
        battleBullet.texture = tex;
    }
    private void Update()
    {
        if (battle && battle.PreparedRenderer())
        {
            var tex = (Texture2D)battleTerritory.texture;
            tex.SetPixels32(battle.rendererTerritory);
            tex.Apply();

            tex = (Texture2D)battleBullet.texture;
            tex.SetPixels32(battle.rendererBullet);
            tex.Apply();

            battle.RequestRender();
        }
    }
    private void OnDestroy()
    {
        battle?.Dispose();
    }
}
