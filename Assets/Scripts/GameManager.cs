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
    public RectTransform productionPanel;
    public Text battleInfo;
    public Transform objectContainer;
    public BattleObject prefabBattleObject;
    public Transform ballContainer;
    public Ball prefabBall;
    public PlayerInfo playerInfoPrefab;
    private List<PlayerInfo> playerInfoList = new();
    public GameObject victoryPanel;
    public GameObject readyPanel;
    public Text victoryText;
    private Battle battle;
    public Transform startPosition;
    public int battleSize = 256;
    public int rateBallCount = 5;
    public long initialHP = 1_000_000;
    [Range(0, 2)]
    public float areaPowerRate = 1.0f;
    private List<Ball> balls = new();
    private Stack<Ball> pool = new();
    private List<BattleObject> battleObjects = new();
    private Stack<BattleObject> battleObjectPool = new();
    private Queue<byte> playerBallShoot = new();
    private float shootCD = 0.5f;
    private void Awake()
    {
        Instance = this;
    }
    [ContextMenu("StartNewBattle")]
    public void StartNewBattle()
    {
        battle?.Dispose();

        battle = new Battle(battleSize, battleSize, initialHP, areaPowerRate);
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

        while (playerInfoList.Count < battle.players.Length)
        {
            var info = Instantiate(playerInfoPrefab);
            info.gameObject.SetActive(true);
            info.transform.SetParent(objectContainer);
            playerInfoList.Add(info);
        }
        while (playerInfoList.Count > battle.players.Length)
        {
            var info = playerInfoList[playerInfoList.Count - 1];
            info.gameObject.SetActive(false);
            playerInfoList.RemoveAt(playerInfoList.Count - 1);
        }

        var list = new List<byte>();
        for (byte i = 0; i < battle.players.Length; i++)
            for (int j = 0; j < rateBallCount; j++)
                list.Add(i);
        for (int i = 0; i < list.Count - 1; i++)
        {
            var j = Random.Range(i, list.Count);
            (list[j], list[i]) = (list[i], list[j]);
        }
        foreach (var item in list)
            playerBallShoot.Enqueue(item);
    }
    private void Start()
    {
        OnResolutionRatioChanged();
    }
    public void OnResolutionRatioChanged()
    {
        var rt = transform as RectTransform;
        var rect = rt.rect;
        productionPanel.sizeDelta = new Vector2(-rect.height, 0);
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
        if (battle && battle.PreparedRenderer()) RendererBattle();
        if (!battle && !victoryPanel.activeSelf && !readyPanel.activeSelf)
        {
            while (balls.Count > 0) Recycle(balls[0]);
            playerBallShoot.Clear();
            victoryPanel.SetActive(true);
            foreach (var player in battle.players)
                if (player.shield > 0)
                {
                    victoryText.text = $"恭喜 <color=#{ColorUtility.ToHtmlStringRGBA(player.color)}>{player.name}</color> 获得了胜利！";
                    break;
                }
        }
        if (battle != null && !battle && battle.PreparedRenderer())//结束后再渲染一帧，避免画面停留在最后一个玩家死之前
        {
            RendererBattle();
            battle = null;
        }
    }
    private void RendererBattle()
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
            battleObjects[i].SetInfo(battle.renderObjects[i]);

        var rt = (RectTransform)objectContainer;
        for (int i = 0; i < playerInfoList.Count; i++)
        {
            var info = playerInfoList[i];
            var player = battle.players[i];
            info.SetValue(player.shield, player.territory, player.bullet, player.laser);
            info.transform.localPosition = new Vector3(rt.rect.width * player.x / battle.width, rt.rect.height * player.y / battle.height, 0);
            if (player.shield == 0)
            {
                var laser = player.laser;
                if (laser > 0)
                {
                    battle.OnCmd(new Command((byte)i, AbilityType.Snipe, laser));
                    player.laser = 0;
                }
                var bullet = player.bullet;
                if (bullet > 0)
                {
                    battle.OnCmd(new Command((byte)i, AbilityType.Scatter, bullet));
                    player.bullet = 0;
                }
            }
        }

        for (int i = 0; i < balls.Count; i++)
        {
            var ball = balls[i];
            if (battle.players[ball.player].shield == 0)
            {
                if (battle)
                    battle.OnCmd(new Command(ball.player, AbilityType.Ball, ball.Value));
                Recycle(ball);
                i--;
            }
        }
        battleInfo.text = $"逻辑帧耗时:{LogicFrameTime}ms\n粒子数量:{battleBulletCount}";
        battle.RequestRender();
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
        ball.SetPlayer(player, battle.players[player].color);
        ball.gameObject.SetActive(true);
        ball.Launch(startPosition.position);
    }
    public void OnAbility(Ball ball, Ability ability)
    {
        battle?.OnCmd(new Command(ball.player, ability.abilityType, ball.Value));
        playerBallShoot.Enqueue(ball.player);
        Recycle(ball);
    }
    public static GameManager Instance { get; private set; }
    public static long LogicFrameTime;
    public static int battleBulletCount;
}
