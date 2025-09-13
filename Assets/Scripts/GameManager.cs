using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct ObjectInfo
{
    public float x, y;
    public long value;

    public ObjectInfo(float x, float y, long value)
    {
        this.x = x;
        this.y = y;
        this.value = value;
    }
}
public class GameManager : MonoBehaviour
{
    public RawImage battleTerritory;
    public RawImage battleBullet;
    public RawImage battleShield;
    public Transform objectContainer;
    public BattleObject prefabBattleObject;
    public Transform ballContainer;
    public Ball prefabBall;
    public PlayerInfo[] playerInfos;
    public GameObject victoryPanel;
    public Text victoryText;
    private float victoryPanelShowTime;
    private Battle battle;
    public Transform startPosition;
    public int battleSize = 256;
    private List<Ball> balls = new();
    private Stack<Ball> pool = new();
    private List<BattleObject> battleObjects = new();
    private Stack<BattleObject> battleObjectPool = new();
    private Queue<byte> playerBallShoot = new();
    private float shootCD = 0.5f;
    private void Awake()
    {
        Instance = this;
        StartNewBattle();
    }
    [ContextMenu("StartNewBattle")]
    public void StartNewBattle()
    {
        battle?.Dispose();
        while (balls.Count > 0) Recycle(balls[0]);
        playerBallShoot.Clear();

        battle = new Battle(battleSize, battleSize);
        var tex = new Texture2D(battle.width, battle.height) { name = "领土tex" };
        tex.SetPixels32(battle.rendererTerritory);
        tex.Apply();
        battleTerritory.texture = tex;
        tex = new Texture2D(battle.width, battle.height) { name = "子弹tex" };
        tex.SetPixels32(battle.rendererBullet);
        tex.Apply();
        battleBullet.texture = tex;
        tex = new Texture2D(battle.width, battle.height) { name = "盾tex" };
        tex.SetPixels32(battle.rendererShield);
        tex.Apply();
        battleShield.texture = tex;

        var list = new List<byte>();
        for (byte i = 0; i < Battle.PlayerColors.Length; i++)
            for (int j = 0; j < 5; j++)
                list.Add(i);
        for (int i = 0; i < list.Count - 1; i++)
        {
            var j = Random.Range(i, list.Count);
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
        foreach (var item in list)
            playerBallShoot.Enqueue(item);
    }
    private void Update()
    {
        if (shootCD > 0) shootCD -= Time.deltaTime;
        else if (battle && playerBallShoot.Count > 0)
        {
            var player = playerBallShoot.Dequeue();
            CreateBall(player);
            shootCD = 0.25f;
        }
        if (battle && battle.PreparedRenderer())
        {
            var tex = (Texture2D)battleTerritory.texture;
            tex.SetPixels32(battle.rendererTerritory);
            tex.Apply();

            tex = (Texture2D)battleBullet.texture;
            tex.SetPixels32(battle.rendererBullet);
            tex.Apply();

            tex = (Texture2D)battleShield.texture;
            tex.SetPixels32(battle.rendererShield);
            tex.Apply();

            while (battle.renderObjects.Count < battleObjects.Count)
            {
                var obj = battleObjects[battleObjects.Count - 1];
                obj.gameObject.SetActive(false);
                battleObjects.RemoveAt(battleObjects.Count - 1);
                battleObjectPool.Push(obj);
            }
            while (battle.renderObjects.Count > battleObjects.Count)
            {
                BattleObject obj;
                if (battleObjectPool.Count > 0)
                {
                    obj = battleObjectPool.Pop();
                }
                else
                {
                    obj = Instantiate(prefabBattleObject);
                    obj.transform.SetParent(objectContainer);
                }
                obj.gameObject.SetActive(true);
                battleObjects.Add(obj);
            }
            for (int i = 0; i < battleObjects.Count; i++)
            {
                battleObjects[i].SetInfo(battle.renderObjects[i]);
            }

            var rt = (RectTransform)objectContainer;
            for (int i = 0; i < playerInfos.Length; i++)
            {
                var info = playerInfos[i];
                var player = battle.players[i];
                info.SetValue(player.shield, player.bullet);
                info.transform.localPosition = new Vector3(rt.rect.width * player.x / battle.width, rt.rect.height * player.y / battle.height, 0);
            }

            for (int i = 0; i < balls.Count; i++)
            {
                var ball = balls[i];
                if (battle.players[ball.Player].shield == 0)
                {
                    Recycle(ball);
                    i--;
                }
            }

            battle.RequestRender();
        }
        if (!battle)
        {
            if (victoryPanel.activeSelf)
            {
                victoryPanelShowTime -= Time.deltaTime;
                if(victoryPanelShowTime <= 0)
                {
                    victoryPanel.SetActive(false);
                    StartNewBattle();
                }
            }
            else
            {
                victoryPanel.SetActive(true);
                for (int i = 0; i < battle.players.Length; i++)
                {
                    if (battle.players[i].shield > 0)
                    {
                        victoryText.text = $"恭喜{Battle.PlayerColorNames[i]}获得了胜利！";
                        break;
                    }
                }
                victoryPanelShowTime = 5;
            }
        }
    }
    private void OnDestroy()
    {
        battle?.Dispose();
    }
    private void Recycle(Ball ball)
    {
        ball.gameObject.SetActive(false);
        balls.Remove(ball);
        pool.Push(ball);
    }
    public void CreateBall(byte player)
    {
        Ball ball;
        if (pool.Count > 0)
        {
            ball = pool.Pop();
        }
        else
        {
            ball = Instantiate(prefabBall);
            ball.transform.SetParent(ballContainer);
        }
        balls.Add(ball);
        ball.Player = player;
        ball.gameObject.SetActive(true);
        ball.Launch(startPosition.position);
    }
    public void OnAbility(Ball ball, Ability ability)
    {
        battle?.OnCmd(new Command(ball.Player, ability.abilityType, ball.Value));
        playerBallShoot.Enqueue(ball.Player);
        Recycle(ball);
    }
    public static GameManager Instance { get; private set; }
}
