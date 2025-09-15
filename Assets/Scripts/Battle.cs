using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

public struct Command
{
    public byte player;
    public AbilityType type;
    public long value;

    public Command(byte player, AbilityType type, long value)
    {
        this.player = player;
        this.type = type;
        this.value = value;
    }
}
public class Battle : IDisposable
{
    public class Player
    {
        public byte id;
        public string name;
        public float x, y;
        public float angle;
        public long shield = 10_000_000;
        public long bullet = 0;
        public long laser = 0;
        public float radius;
        public Color32 color;
        public Color32 territoryColor;
        public Color32 shieldColor;
        public Color32 LaserColor;
        public Player(byte id, string name, float x, float y, float angle, float radius, Color color)
        {
            this.id = id;
            this.name = name;
            this.x = x;
            this.y = y;
            this.angle = angle;
            this.radius = radius;
            this.color = color;
            territoryColor = color * .75f;
            shieldColor = color;
            LaserColor = color;
            LaserColor.a = 192;
        }
    }
    private struct Bullet
    {
        public float x, y;
        public float vx, vy;
        public byte player;
        public long damage;
        public Bullet(float x, float y, float angle, float v, byte player, long damage)
        {
            this.x = x;
            this.y = y;
            vx = (float)Math.Cos(angle) * v;
            vy = (float)Math.Sin(angle) * v;
            this.player = player;
            this.damage = damage;
        }
    }
    private struct TerritoryMarkingOrb
    {
        public float x, y;
        public float vx, vy;
        public byte player;
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
    private struct Scatter
    {
        public float x, y;
        public float angle;
        public float vx, vy;
        public byte player;
        public long bullet;
    }
    private readonly System.Random random = new();
    private bool disposed = false;
    private readonly byte[] territory;
    private readonly int[] orbMask;
    public readonly Color32[] rendererShield, rendererBullet, rendererTerritory;
    public readonly List<ObjectInfo> renderObjects = new();
    public readonly int width, height;
    private bool renderer = false;
    public readonly Player[] players;
    private readonly List<Bullet> bullets = new();
    private readonly List<TerritoryMarkingOrb> territoryMarkingOrbs = new();
    private readonly List<Scatter> scatters = new();
    private readonly Queue<Command> commandQueue = new();
    public Battle(int width, int height)
    {
        this.width = width;
        this.height = height;
        territory = new byte[width * height];
        orbMask = new int[width * height];
        rendererShield = new Color32[width * height];
        rendererBullet = new Color32[width * height];
        rendererTerritory = new Color32[width * height];
        var radius = Math.Min(width, height) / 16f;
        players = new Player[]
        {
            new(0,"红色", width / 8, height / 8, (float)(Math.PI * 2 * random.NextDouble()), radius,Color.red),
            new(1,"绿色", width / 8, height * 7 / 8, (float)(Math.PI * 2 * random.NextDouble()), radius,Color.green),
            new(2,"黄色", width * 7 / 8, height / 8, (float)(Math.PI * 2 * random.NextDouble()), radius,Color.yellow),
            new(3,"蓝色", width * 7 / 8, height * 7 / 8, (float)(Math.PI * 2 * random.NextDouble()), radius,Color.blue),
            new(4,"青色", width / 2, height / 2, (float)(Math.PI * 2 * random.NextDouble()), radius,Color.cyan)
        };
        for (int i = 0; i < territory.Length; i++)
        {
            var x = i % width;
            var y = i / width;
            var sqrDist = float.MaxValue;
            for (byte j = 0; j < players.Length; j++)
            {
                var dx = players[j].x - x;
                var dy = players[j].y - y;
                var d = dx * dx + dy * dy;
                if (d < sqrDist)
                {
                    sqrDist = d;
                    territory[i] = j;
                }
            }
        }
        Renderer();

        new Thread(Update).Start();
    }
    public void RequestRender()
    {
        renderer = false;
    }
    public bool PreparedRenderer() => renderer;
    private void RendererLine(float x, float y, float angle, byte player)
    {
        var vx = (float)Math.Cos(angle);
        var vy = (float)Math.Sin(angle);
        var step = Math.Abs(vx) > Math.Abs(vy) ? Math.Abs(vx) : Math.Abs(vy);
        vx /= step;
        vy /= step;
        while (true)
        {
            var px = (int)x;
            var py = (int)y;
            if (px >= 0 && px < width && py >= 0 && py < height)
            {
                var index = py * width + px;
                if (territory[index] != player)
                    break;
                rendererShield[index] = players[player].LaserColor;
                x += vx;
                y += vy;
            }
            else break;
        }

    }
    private void Renderer()
    {
        for (int i = 0; i < territory.Length; i++)
            rendererTerritory[i] = players[territory[i]].territoryColor;
        Array.Clear(rendererBullet, 0, rendererBullet.Length);
        Array.Clear(rendererShield, 0, rendererShield.Length);
        renderObjects.Clear();
        foreach (var bullet in bullets)
        {
            var index = (int)bullet.y * width + (int)bullet.x;
            rendererBullet[index] = new Color32(255, 255, 255, 192);
        }
        foreach (var star in scatters)
            renderObjects.Add(new ObjectInfo(star.x / width, star.y / height, star.bullet));
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
                        var r = (dx * dx + dy * dy) / (radius * radius);
                        if (r < 1)
                        {
                            var index = py * width + px;
                            var color = players[orb.player].shieldColor;
                            r = MathF.Pow(r, 4);
                            color = Color32.Lerp(color, new Color32(0, 0, 0, 255), r);
                            rendererShield[index] = color;
                        }
                    }
                }
            renderObjects.Add(new ObjectInfo(orb.x / width, orb.y / height, orb.bullet));
        }
        foreach (var player in players)
            if (player.shield > 0)
            {
                if (player.laser > 0)
                {
                    var laserAngle = player.angle * 0.05f;
                    for (int index = 0; index < 4; index++)
                    {
                        laserAngle += MathF.PI * .5f;
                        RendererLine(player.x, player.y, laserAngle, player.id);
                        var x = Mathf.Sin(laserAngle);
                        var y = -Mathf.Cos(laserAngle);
                        for (int i = 0; i < player.radius; i++)
                        {
                            RendererLine(player.x + x * i, player.y + y * i, laserAngle, player.id);
                            RendererLine(player.x - x * i, player.y - y * i, laserAngle, player.id);
                        }
                    }
                }
                for (int x = 0; x < player.radius * 2; x++)
                    for (int y = 0; y < player.radius * 2; y++)
                    {
                        var px = (int)(player.x - player.radius + x);
                        var py = (int)(player.y - player.radius + y);
                        if (px >= 0 && px < width && py >= 0 && py < height)
                        {
                            var dx = x - player.radius;
                            var dy = y - player.radius;
                            var r = (dx * dx + dy * dy) / (player.radius * player.radius);
                            if (r < 1)
                            {
                                var index = py * width + px;
                                rendererShield[index] = new Color32(255, 255, 255, (byte)(128 * r));
                            }
                        }
                    }
            }


        renderer = true;
    }
    public void OnCmd(Command cmd)
    {
        lock (this)
        {
            commandQueue.Enqueue(cmd);
        }
    }

    private void OnPlayerDead(Player player)
    {
        var activePlayers = 0;
        foreach (var item in players)
            if (item.shield > 0) activePlayers++;
        if (activePlayers <= 1)
            Dispose();
    }

    private void GenPlayerButtle(Player player, float scattering, long damage)
    {
        var bullet = new Bullet(player.x, player.y, player.angle + (float)random.NextDouble() * scattering * Mathf.PI * 2, (float)random.NextDouble() * 0.25f + 0.75f, player.id, damage);
        bullets.Add(bullet);
    }
    private void GenTMOrb(Player player, long bullet)
    {
        var orb = new TerritoryMarkingOrb(player, bullet, 1f);
        territoryMarkingOrbs.Add(orb);
    }

    private long CircleWipe(float cx, float cy, float radius, byte player, long bullet)
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
    private long LineWipe(float x, float y, float angle, byte player, long bullet, long penetration)
    {
        var vx = (float)Math.Cos(angle);
        var vy = (float)Math.Sin(angle);
        var step = Math.Abs(vx) > Math.Abs(vy) ? Math.Abs(vx) : Math.Abs(vy);
        vx /= step;
        vy /= step;
        while (bullet > 0 && penetration > 0)
        {
            var px = (int)x;
            var py = (int)y;
            if (px >= 0 && px < width && py >= 0 && py < height)
            {
                var index = py * width + px;
                if (territory[index] != player)
                {
                    bullet--;
                    penetration--;
                    territory[index] = player;
                }
                x += vx;
                y += vy;
            }
            else break;
        }
        return bullet;
    }
    private void PlayerLogic()
    {
        foreach (var player in players)
            if (player.shield > 0)
            {
                for (int i = 0; i < 107 && player.bullet > 0; i++, player.bullet--)
                    GenPlayerButtle(player, 0.06f, 1);
                for (int i = 0; i < 10; i++)
                    GenPlayerButtle(player, 1, 1);
                player.angle += (float)Math.PI * 0.01f;

                player.shield = CircleWipe(player.x, player.y, player.radius, player.id, player.shield);
                if (player.shield == 0) OnPlayerDead(player);
                if (player.laser > 0)
                {
                    var laserAngle = player.angle * 0.05f;
                    for (int index = 0; index < 4; index++)
                    {
                        laserAngle += MathF.PI * .5f;
                        player.laser = LineWipe(player.x, player.y, laserAngle, player.id, player.laser, 1);
                        var x = Mathf.Sin(laserAngle);
                        var y = -Mathf.Cos(laserAngle);
                        for (int i = 0; i < player.radius; i++)
                        {
                            player.laser = LineWipe(player.x + x * i, player.y + y * i, laserAngle, player.id, player.laser, 1);
                            player.laser = LineWipe(player.x - x * i, player.y - y * i, laserAngle, player.id, player.laser, 1);
                        }
                    }
                }
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

            var index = (int)bullet.y * width + (int)bullet.x;
            if (index < 0 || index >= territory.Length)
            {
                Debug($"无效的索引,子弹坐标:({bullet.x},{bullet.y})");
                bullets[i] = bullets[^1];
                bullets.RemoveAt(bullets.Count - 1);
                i--;
            }
            else
            {
                if (territory[index] != bullet.player)
                {
                    territory[index] = bullet.player;
                    bullet.damage--;
                }
                if (orbMask[index] > 0)
                {
                    var orbIndex = orbMask[index] - 1;
                    var orb = territoryMarkingOrbs[orbIndex];
                    if (orb.bullet > 0)
                    {
                        if (orb.player != bullet.player)
                        {
                            if (orb.bullet > bullet.damage)
                            {
                                orb.bullet -= bullet.damage;
                                bullet.damage = 0;
                            }
                            else
                            {
                                bullet.damage -= orb.bullet;
                                orb.bullet = 0;
                            }
                        }
                        else
                        {
                            orb.bullet += bullet.damage;
                            bullet.damage = 0;
                        }
                        territoryMarkingOrbs[orbIndex] = orb;
                    }
                }
                if (bullet.damage <= 0)
                {
                    bullets[i] = bullets[^1];
                    bullets.RemoveAt(bullets.Count - 1);
                    i--;
                }
                else bullets[i] = bullet;
            }
        }
    }
    private void ResetTMOrbMask()
    {
        Array.Clear(orbMask, 0, orbMask.Length);
        for (int i = 0; i < territoryMarkingOrbs.Count; i++)
        {
            var orb = territoryMarkingOrbs[i];
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
                            orbMask[index] = i + 1;
                        }
                    }
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
            orb.bullet = CircleWipe(orb.x, orb.y, radius, orb.player, orb.bullet);
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
    private void ScatterLogic()
    {
        for (int i = 0; i < scatters.Count; i++)
        {
            var scatter = scatters[i];

            scatter.x += scatter.vx;
            scatter.y += scatter.vy;
            if (scatter.x < 0)
            {
                scatter.x = -scatter.x;
                scatter.vx = -scatter.vx;
            }
            else if (scatter.x >= width)
            {
                scatter.x = 2 * (width - 1) - scatter.x;
                scatter.vx = -scatter.vx;
            }
            if (scatter.y < 0)
            {
                scatter.y = -scatter.y;
                scatter.vy = -scatter.vy;
            }
            else if (scatter.y >= height)
            {
                scatter.y = 2 * (height - 1) - scatter.y;
                scatter.vy = -scatter.vy;
            }

            for (int idx = 0; idx < 30; idx++)
                for (int index = 0; index < 5 && scatter.bullet > 0; index++, scatter.bullet--)
                {
                    var angle = scatter.angle + index * Mathf.PI * 2 * .2f;
                    var bullet = new Bullet(scatter.x, scatter.y, angle + (float)random.NextDouble() * .1f * Mathf.PI * 2, (float)random.NextDouble() * 0.5f + 1.5f, scatter.player, 1);
                    bullets.Add(bullet);
                }
            {
                var index = (int)scatter.y * width + (int)scatter.x;
                if (orbMask[index] > 0)
                {
                    var orbIndex = orbMask[index] - 1;
                    var orb = territoryMarkingOrbs[orbIndex];
                    if (orb.bullet > 0)
                    {
                        if (orb.player != scatter.player)
                        {
                            if (orb.bullet > scatter.bullet)
                            {
                                orb.bullet -= scatter.bullet;
                                scatter.bullet = 0;
                            }
                            else
                            {
                                scatter.bullet -= orb.bullet;
                                orb.bullet = 0;
                            }
                        }
                        territoryMarkingOrbs[orbIndex] = orb;
                    }
                }
            }
            foreach (var player in players)
            {
                if (player.shield > 0 && scatter.player != player.id)
                {
                    var d = player.radius;
                    var dx = scatter.x - player.x;
                    var dy = scatter.y - player.y;
                    if (dx * dx + dy * dy < d * d)
                    {
                        d = (float)Math.Sqrt(dx * dx + dy * dy);
                        dx /= d;
                        dy /= d;

                        var dot = scatter.vx * dx + scatter.vy * dy;
                        var nvx = dot * dx;
                        var nvy = dot * dy;
                        scatter.vx -= nvx * 2;
                        scatter.vy -= nvy * 2;

                        var lose = Math.Min(scatter.bullet, player.shield) / 2;
                        scatter.bullet -= lose;
                        player.shield -= lose;

                        scatter.x = player.x + dx * player.radius;
                        scatter.y = player.y + dy * player.radius;
                    }
                }
            }
            if (scatter.bullet <= 0)
            {
                scatters[i] = scatters[^1];
                scatters.RemoveAt(scatters.Count - 1);
                i--;
            }
            else
            {
                scatter.angle += .01f * Mathf.PI;
                scatters[i] = scatter;
            }
        }
    }
    private void ExeCmd()
    {
        lock (this)
        {
            while (commandQueue.Count > 0)
            {
                var cmd = commandQueue.Dequeue();
                if (cmd.player < players.Length)
                {
                    var player = players[cmd.player];
                    if (player.shield > 0)
                        switch (cmd.type)
                        {
                            case AbilityType.HP:
                                player.shield += cmd.value;
                                break;
                            case AbilityType.Strafe:
                                player.bullet += cmd.value;
                                break;
                            case AbilityType.Ball:
                                GenTMOrb(player, cmd.value);
                                break;
                            case AbilityType.Snipe:
                                {
                                    var value = (long)Math.Sqrt(cmd.value);
                                    var x = Mathf.Sin(player.angle);
                                    var y = -Mathf.Cos(player.angle);
                                    for (int i = 0; i < value; i++)
                                    {
                                        var d = (float)(random.NextDouble() - 0.5) * player.radius;
                                        var bullet = new Bullet(player.x + x * d, player.y + y * d, player.angle, (float)random.NextDouble() * 0.25f + 0.75f, player.id, value);
                                        bullets.Add(bullet);
                                    }
                                }
                                break;
                            case AbilityType.Scatter:
                                var star = new Scatter
                                {
                                    x = player.x,
                                    y = player.y,
                                    angle = player.angle,
                                    vx = (float)Math.Cos(player.angle) * .25f,
                                    vy = (float)Math.Sin(player.angle) * .25f,
                                    player = player.id,
                                    bullet = cmd.value
                                };
                                scatters.Add(star);
                                break;
                            case AbilityType.Laser:
                                players[cmd.player].laser += cmd.value;
                                break;
                        }
                }
            }
        }
    }
    private void BattleLogic()
    {
        ResetTMOrbMask();
        ExeCmd();
        PlayerLogic();
        BulletLogic();
        ScatterLogic();
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
            if (millis > 0)
                Thread.Sleep((int)millis);

            GameManager.LogicFrameTime = sw.ElapsedMilliseconds;
            GameManager.battleBulletCount = bullets.Count;
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
