using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

public class Battle : IDisposable
{
    private class Player
    {
        public int id;
        public float x, y;
        public float angle;
        public long shield;
        public long bullet;
        public Color32 color;
        public float radius;
    }
    private struct Bullet
    {
        public float x, y;
        public float vx, vy;
        public int player;
        public Bullet(float x, float y, float angle, float v, int player)
        {
            this.x = x;
            this.y = y;
            vx = (float)Math.Cos(angle) * v;
            vy = (float)Math.Sin(angle) * v;
            this.player = player;
        }
    }
    private struct TerritoryMarkingOrb
    {
        public float x, y;
        public float vx, vy;
        public int player;
        public long bullet;
        public readonly float Radius => (float)Math.Max(Math.Log(bullet, 2f), 1);
        public TerritoryMarkingOrb(Player player, long bullet, float v)
        {
            x = player.x;
            y = player.y;
            this.player = player.id;
            this.bullet = bullet;
            vx = (float)Math.Cos(player.angle) * v;
            vy = (float)Math.Sin(player.angle) * v;
        }
    }
    private readonly System.Random random = new();
    private bool disposed = false;
    private readonly int[] territory;
    public readonly Color32[] rendererBullet, rendererTerritory;
    public readonly int width, height;
    private bool renderer = false;
    private readonly Player[] players = new Player[4];
    private readonly List<Bullet> bullets = new();
    private readonly List<TerritoryMarkingOrb> territoryMarkingOrbs = new();
    public Battle(int width, int height)
    {
        this.width = width;
        this.height = height;
        territory = new int[width * height];
        for (int i = 0; i < territory.Length; i++)
        {
            var x = i % width;
            var y = i / width;
            if (x < width / 2)
            {
                if (y < height / 2)
                    territory[i] = 0;
                else
                    territory[i] = 1;
            }
            else
            {
                if (y < height / 2)
                    territory[i] = 2;
                else
                    territory[i] = 3;
            }
        }
        rendererBullet = new Color32[width * height];
        rendererTerritory = new Color32[width * height];
        var radius = Math.Min(width, height) / 16f;
        players[0] = new Player { id = 0, x = width / 8, y = height / 8, angle = (float)Math.PI / 4, shield = 100000, bullet = 0, color = Color.red, radius = radius };
        players[1] = new Player { id = 1, x = width / 8, y = height * 7 / 8, angle = (float)Math.PI * 3 / 4, shield = 100000, bullet = 0, color = Color.green, radius = radius };
        players[2] = new Player { id = 2, x = width * 7 / 8, y = height / 8, angle = (float)Math.PI * 5 / 4, shield = 100000, bullet = 0, color = Color.yellow, radius = radius };
        players[3] = new Player { id = 3, x = width * 7 / 8, y = height * 7 / 8, angle = (float)Math.PI * 7 / 4, shield = 100000, bullet = 1000000000, color = Color.blue, radius = radius };
        Renderer();

        new Thread(Update).Start();
    }
    public void RequestRender()
    {
        renderer = false;
    }
    public bool PreparedRenderer() => renderer;
    private void Renderer()
    {
        for (int i = 0; i < territory.Length; i++)
            rendererTerritory[i] = players[territory[i]].color;
        Array.Clear(rendererBullet, 0, rendererBullet.Length);
        foreach (var buttle in bullets)
        {
            var index = (int)buttle.y * width + (int)buttle.x;
            rendererBullet[index] = new Color32(255, 255, 255, 255);
        }
        foreach (var orb in territoryMarkingOrbs)
        {
            var radius = orb.Radius;
            for (int x = 0; x < radius * 2; x++)
                for (int y = 0; y < radius * 2; y++)
                {
                    var px = (int)(orb.x - radius + x);
                    var py = (int)(orb.y - radius + y);
                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        var dx = x - radius;
                        var dy = y - radius;
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            var index = py * width + px;
                            rendererBullet[index] = new Color32(0, 0, 0, 128);
                        }
                    }
                }
        }
        renderer = true;
    }

    private void OnPlayerDead(Player player)
    {
        //todo 有玩家死亡，给渲染层发送通知，同时检测是否游戏结束
    }

    private void GenPlayerButtle(Player player, float scattering)
    {
        var buttle = new Bullet(player.x, player.y, player.angle + (float)random.NextDouble() * scattering, (float)random.NextDouble() * 0.25f + 0.75f, player.id);
        bullets.Add(buttle);
    }
    private void GenTMOrb(Player player, long bullet)
    {
        var orb = new TerritoryMarkingOrb(player, bullet, 1f);
        territoryMarkingOrbs.Add(orb);
    }

    private long Wipe(float cx, float cy, float radius, int player, long bullet)
    {
        for (int x = 0; x < radius * 2; x++)
            for (int y = 0; y < radius * 2; y++)
            {
                var px = (int)(cx - radius + x);
                var py = (int)(cy - radius + y);
                if (px >= 0 && px < width && py >= 0 && py < height)
                {
                    var dx = x - radius;
                    var dy = y - radius;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        var index = py * width + px;
                        if (territory[index] != player)
                        {
                            if (bullet > 0)
                            {
                                bullet--;
                                territory[index] = player;
                                if (bullet == 0)
                                    return 0;
                            }
                        }
                    }
                }
            }
        return bullet;
    }
    private void PlayerLogic()
    {
        foreach (var player in players)
            if (player.shield > 0)
            {
                for (int i = 0; i < 10 && player.bullet > 0; i++, player.bullet--)
                    GenPlayerButtle(player, 0.3f);
                GenPlayerButtle(player, 0);
                player.angle += (float)Math.PI * 0.01f;

                player.shield = Wipe(player.x, player.y, player.radius, player.id, player.shield);
                if (player.shield == 0) OnPlayerDead(player);

                if (random.NextDouble() < 0.001) GenTMOrb(player, random.Next() % 1000000);
            }
    }
    private void BulletLogic()
    {
        for (int i = 0; i < bullets.Count; i++)
        {
            var bullet = bullets[i];
            bullet.x += bullet.vx;
            bullet.y += bullet.vy;
            if (bullet.x < 0)
            {
                bullet.x = -bullet.x;
                bullet.vx = -bullet.vx;
            }
            else if (bullet.x >= width)
            {
                bullet.x = 2 * (width - 1) - bullet.x;
                bullet.vx = -bullet.vx;
            }
            if (bullet.y < 0)
            {
                bullet.y = -bullet.y;
                bullet.vy = -bullet.vy;
            }
            else if (bullet.y >= height)
            {
                bullet.y = 2 * (height - 1) - bullet.y;
                bullet.vy = -bullet.vy;
            }
            bullets[i] = bullet;

            var index = (int)bullet.y * width + (int)bullet.x;
            if (index < 0 || index >= territory.Length)
            {
                Debug($"无效的索引,子弹坐标:({bullet.x},{bullet.y})");
                bullets[i] = bullets[^1];
                bullets.RemoveAt(bullets.Count - 1);
                i--;
            }
            else if (territory[index] != bullet.player)
            {
                territory[index] = bullet.player;
                bullets[i] = bullets[^1];
                bullets.RemoveAt(bullets.Count - 1);
                i--;
            }
        }
    }
    private void TerritoryMarkingOrbLogic()
    {
        for (int i = 0; i < territoryMarkingOrbs.Count; i++)
        {
            var orb = territoryMarkingOrbs[i];
            orb.x += orb.vx;
            orb.y += orb.vy;
            var radius = orb.Radius;

            if (orb.x - radius < 0)
            {
                orb.x = radius * 2 - orb.x;
                orb.vx = -orb.vx;
            }
            else if (orb.x + radius >= width)
            {
                orb.x = 2 * (width - radius - 1) - orb.x;
                orb.vx = -orb.vx;
            }
            if (orb.y - radius < 0)
            {
                orb.y = radius * 2 - orb.y;
                orb.vy = -orb.vy;
            }
            else if (orb.y + radius >= height)
            {
                orb.y = 2 * (height - radius - 1) - orb.y;
                orb.vy = -orb.vy;
            }
            orb.bullet = Wipe(orb.x, orb.y, radius, orb.player, orb.bullet);
            territoryMarkingOrbs[i] = orb;
            if (orb.bullet == 0)
            {
                territoryMarkingOrbs[i] = territoryMarkingOrbs[^1];
                territoryMarkingOrbs.RemoveAt(territoryMarkingOrbs.Count - 1);
                i--;
            }
        }
        for (int x = 0; x < territoryMarkingOrbs.Count; x++)
        {
            for (int y = x + 1; y < territoryMarkingOrbs.Count; y++)
            {
                var a = territoryMarkingOrbs[x];
                var b = territoryMarkingOrbs[y];
                var ra = a.Radius;
                var rb = b.Radius;
                var d = ra + rb;
                var dx = a.x - b.x;
                var dy = a.y - b.y;
                if (dx * dx + dy * dy < d * d)
                {
                    d = (float)Math.Sqrt(dx * dx + dy * dy);
                    dx /= d;
                    dy /= d;
                    var vx = a.vx - b.vx;
                    var vy = a.vy - b.vy;
                    var dot = vx * dx + vy * dy;
                    var nvx = dot * dx;
                    var nvy = dot * dy;

                    var vax = (ra - rb) * nvx / (ra + rb);
                    var vay = (ra - rb) * nvy / (ra + rb);
                    var vbx = 2 * ra * nvx / (ra + rb);
                    var vby = 2 * ra * nvy / (ra + rb);
                    a.vx = vax + vx - nvx + b.vx;
                    a.vy = vay + vy - nvy + b.vy;
                    b.vx += vbx;
                    b.vy += vby;
                    d = (ra + rb - d) * .5f;
                    a.x += dx * d;
                    a.y += dy * d;
                    b.x -= dx * d;
                    b.y -= dy * d;
                    if (a.player != b.player)
                    {
                        var lose = Math.Min(a.bullet, b.bullet) / 2;
                        a.bullet -= lose;
                        b.bullet -= lose;
                    }
                    territoryMarkingOrbs[x] = a;
                    territoryMarkingOrbs[y] = b;
                }
            }
            foreach (var player in players)
            {
                var orb = territoryMarkingOrbs[x];
                if (player.shield > 0 && orb.player != player.id)
                {
                    var radius = orb.Radius;
                    var d = radius + player.radius;
                    var dx = orb.x - player.x;
                    var dy = orb.y - player.y;
                    if (dx * dx + dy * dy < d * d)
                    {
                        d = (float)Math.Sqrt(dx * dx + dy * dy);
                        dx /= d;
                        dy /= d;

                        var dot = orb.vx * dx + orb.vy * dy;
                        var nvx = dot * dx;
                        var nvy = dot * dy;
                        orb.vx -= nvx * 2;
                        orb.vy -= nvy * 2;

                        var lose = Math.Min(orb.bullet, player.shield) / 2;
                        orb.bullet -= lose;
                        player.shield -= lose;

                        orb.x = player.x + dx * (radius + player.radius);
                        orb.y = player.y + dy * (radius + player.radius);
                        territoryMarkingOrbs[x] = orb;
                    }
                }
            }
        }
    }
    private void BattleLogic()
    {
        PlayerLogic();
        BulletLogic();
        TerritoryMarkingOrbLogic();
    }
    private void Update()
    {
        var sw = new Stopwatch();
        while (!disposed)
        {
            sw.Restart();
            lock (this)
            {
                if (disposed)
                    break;
                BattleLogic();
                if (!renderer)
                    Renderer();
            }
            sw.Stop();
            var millis = 10 - sw.ElapsedMilliseconds;
            Debug("逻辑帧耗时: " + sw.ElapsedMilliseconds + "ms");
            if (millis > 0)
                Thread.Sleep((int)millis);
        }
    }
    public void Dispose()
    {
        lock (this) disposed = true;
        GC.SuppressFinalize(this);
    }
    public static implicit operator bool(Battle battle) => battle != null && !battle.disposed;
    private static void Debug(string msg) => UnityEngine.Debug.Log(msg);
}
