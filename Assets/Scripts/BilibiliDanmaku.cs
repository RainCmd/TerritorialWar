using UnityEngine;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;

public class BilibiliDanmaku : MonoBehaviour
{
    public int roomId = 23058; // 测试用直播间ID（B站官方测试房）
    private ClientWebSocket webSocket;
    private CancellationTokenSource cts;
    private const int HEARTBEAT_INTERVAL = 30000; // 30秒心跳

    void Start()
    {
        Connect();
    }

    async void Connect()
    {
        cts = new CancellationTokenSource();
        webSocket = new ClientWebSocket();

        try
        {
            // 1. 连接弹幕服务器
            await webSocket.ConnectAsync(
                new Uri("wss://broadcastlv.chat.bilibili.com:2245/sub"),
                cts.Token
            );
            Debug.Log("连接成功，发送认证包...");

            // 2. 发送认证包
            await SendAuthPacket();

            // 3. 启动心跳和接收循环
            _ = StartHeartbeat();
            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"连接失败: {e.Message}");
            ScheduleReconnect();
        }
    }

    // 发送认证包（严格大端字节序）
    async Task SendAuthPacket()
    {
        var authJson = JsonConvert.SerializeObject(new
        {
            roomid = roomId,
            uid = 10000,
            protover = 1, // 不压缩模式
            platform = "web",
            clientver = "1.14.3",
            type = 2
        });

        byte[] body = Encoding.UTF8.GetBytes(authJson);
        byte[] packet = EncodePacket(body, 7); // 7=认证包
        await webSocket.SendAsync(
            new ArraySegment<byte>(packet),
            WebSocketMessageType.Binary,
            true,
            cts.Token
        );
    }

    // 心跳循环
    async Task StartHeartbeat()
    {
        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                byte[] pingData = EncodePacket(Encoding.UTF8.GetBytes("ping"), 2); // 2=心跳包
                await webSocket.SendAsync(
                    new ArraySegment<byte>(pingData),
                    WebSocketMessageType.Binary,
                    true,
                    cts.Token
                );
                Debug.Log("发送心跳包");
            }
            catch (Exception e)
            {
                Debug.LogError($"心跳失败: {e.Message}");
                break;
            }
            await Task.Delay(HEARTBEAT_INTERVAL, cts.Token);
        }
    }

    // 接收数据循环
    async Task ReceiveLoop()
    {
        byte[] buffer = new byte[1024 * 32];
        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cts.Token
                );

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    ParseBinaryData(buffer, result.Count);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing",
                        cts.Token
                    );
                    break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"接收错误: {e.Message}");
                break;
            }
        }

        ScheduleReconnect();
    }

    // 解析二进制数据包（大端字节序处理）
    void ParseBinaryData(byte[] data, int length)
    {
        int offset = 0;
        while (offset < length)
        {
            // 解析协议头（大端字节序）
            int packetLen = BitConverter.ToInt32(ReverseBytes(data, offset, 4), 0);
            offset += 4;

            short headerLen = BitConverter.ToInt16(ReverseBytes(data, offset, 2), 0);
            offset += 2;

            short protoVer = BitConverter.ToInt16(ReverseBytes(data, offset, 2), 0);
            offset += 2;

            int operation = BitConverter.ToInt32(ReverseBytes(data, offset, 4), 0);
            offset += 4;

            int seqId = BitConverter.ToInt32(ReverseBytes(data, offset, 4), 0);
            offset += 4;

            // 提取消息体
            byte[] body = new byte[packetLen - headerLen];
            Array.Copy(data, offset, body, 0, body.Length);
            offset += body.Length;

            // 处理消息
            HandleOperation(operation, body, protoVer);
        }
    }

    // 处理不同类型的消息
    void HandleOperation(int operation, byte[] body, short protoVer)
    {
        switch (operation)
        {
            case 5: // 弹幕/礼物等消息
                string json = Encoding.UTF8.GetString(body);
                Debug.Log($"收到消息: {json}");
                // 这里可以解析具体弹幕内容
                break;
            case 3: // 心跳回应
                Debug.Log("收到服务器心跳回应");
                break;
            case 8: // 认证成功
                Debug.Log("认证成功！");
                break;
            default:
                Debug.Log($"未知操作码: {operation}");
                break;
        }
    }

    // 编码数据包（大端字节序）
    byte[] EncodePacket(byte[] body, int operation)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            // 协议头字段
            int packetLen = 16 + body.Length; // 总长度=头(16)+体
            short headerLen = 16;
            short protoVer = 1;
            int seqId = 1;

            // 写入大端字节序的协议头
            ms.Write(Int32ToBigEndian(packetLen), 0, 4);
            ms.Write(Int16ToBigEndian(headerLen), 0, 2);
            ms.Write(Int16ToBigEndian(protoVer), 0, 2);
            ms.Write(Int32ToBigEndian(operation), 0, 4);
            ms.Write(Int32ToBigEndian(seqId), 0, 4);

            // 写入消息体
            ms.Write(body, 0, body.Length);

            return ms.ToArray();
        }
    }

    // 工具方法：转换为大端字节序
    private byte[] Int32ToBigEndian(int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    private byte[] Int16ToBigEndian(short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    // 工具方法：反转字节（用于解析）
    private byte[] ReverseBytes(byte[] data, int offset, int length)
    {
        byte[] temp = new byte[length];
        Array.Copy(data, offset, temp, 0, length);
        Array.Reverse(temp);
        return temp;
    }

    // 重连调度
    void ScheduleReconnect()
    {
        Debug.Log("5秒后尝试重连...");
        Invoke(nameof(Reconnect), 5f);
    }

    void Reconnect()
    {
        if (webSocket != null)
        {
            webSocket.Dispose();
        }
        cts?.Cancel();
        Connect();
    }

    void OnDestroy()
    {
        cts?.Cancel();
        webSocket?.Dispose();
        CancelInvoke();
    }
}
