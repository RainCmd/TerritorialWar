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
    public int roomId = 23058; // ������ֱ����ID��Bվ�ٷ����Է���
    private ClientWebSocket webSocket;
    private CancellationTokenSource cts;
    private const int HEARTBEAT_INTERVAL = 30000; // 30������

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
            // 1. ���ӵ�Ļ������
            await webSocket.ConnectAsync(
                new Uri("wss://broadcastlv.chat.bilibili.com:2245/sub"),
                cts.Token
            );
            Debug.Log("���ӳɹ���������֤��...");

            // 2. ������֤��
            await SendAuthPacket();

            // 3. ���������ͽ���ѭ��
            _ = StartHeartbeat();
            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"����ʧ��: {e.Message}");
            ScheduleReconnect();
        }
    }

    // ������֤�����ϸ����ֽ���
    async Task SendAuthPacket()
    {
        var authJson = JsonConvert.SerializeObject(new
        {
            roomid = roomId,
            uid = 10000,
            protover = 1, // ��ѹ��ģʽ
            platform = "web",
            clientver = "1.14.3",
            type = 2
        });

        byte[] body = Encoding.UTF8.GetBytes(authJson);
        byte[] packet = EncodePacket(body, 7); // 7=��֤��
        await webSocket.SendAsync(
            new ArraySegment<byte>(packet),
            WebSocketMessageType.Binary,
            true,
            cts.Token
        );
    }

    // ����ѭ��
    async Task StartHeartbeat()
    {
        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                byte[] pingData = EncodePacket(Encoding.UTF8.GetBytes("ping"), 2); // 2=������
                await webSocket.SendAsync(
                    new ArraySegment<byte>(pingData),
                    WebSocketMessageType.Binary,
                    true,
                    cts.Token
                );
                Debug.Log("����������");
            }
            catch (Exception e)
            {
                Debug.LogError($"����ʧ��: {e.Message}");
                break;
            }
            await Task.Delay(HEARTBEAT_INTERVAL, cts.Token);
        }
    }

    // ��������ѭ��
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
                Debug.LogError($"���մ���: {e.Message}");
                break;
            }
        }

        ScheduleReconnect();
    }

    // �������������ݰ�������ֽ�����
    void ParseBinaryData(byte[] data, int length)
    {
        int offset = 0;
        while (offset < length)
        {
            // ����Э��ͷ������ֽ���
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

            // ��ȡ��Ϣ��
            byte[] body = new byte[packetLen - headerLen];
            Array.Copy(data, offset, body, 0, body.Length);
            offset += body.Length;

            // ������Ϣ
            HandleOperation(operation, body, protoVer);
        }
    }

    // ����ͬ���͵���Ϣ
    void HandleOperation(int operation, byte[] body, short protoVer)
    {
        switch (operation)
        {
            case 5: // ��Ļ/�������Ϣ
                string json = Encoding.UTF8.GetString(body);
                Debug.Log($"�յ���Ϣ: {json}");
                // ������Խ������嵯Ļ����
                break;
            case 3: // ������Ӧ
                Debug.Log("�յ�������������Ӧ");
                break;
            case 8: // ��֤�ɹ�
                Debug.Log("��֤�ɹ���");
                break;
            default:
                Debug.Log($"δ֪������: {operation}");
                break;
        }
    }

    // �������ݰ�������ֽ���
    byte[] EncodePacket(byte[] body, int operation)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            // Э��ͷ�ֶ�
            int packetLen = 16 + body.Length; // �ܳ���=ͷ(16)+��
            short headerLen = 16;
            short protoVer = 1;
            int seqId = 1;

            // д�����ֽ����Э��ͷ
            ms.Write(Int32ToBigEndian(packetLen), 0, 4);
            ms.Write(Int16ToBigEndian(headerLen), 0, 2);
            ms.Write(Int16ToBigEndian(protoVer), 0, 2);
            ms.Write(Int32ToBigEndian(operation), 0, 4);
            ms.Write(Int32ToBigEndian(seqId), 0, 4);

            // д����Ϣ��
            ms.Write(body, 0, body.Length);

            return ms.ToArray();
        }
    }

    // ���߷�����ת��Ϊ����ֽ���
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

    // ���߷�������ת�ֽڣ����ڽ�����
    private byte[] ReverseBytes(byte[] data, int offset, int length)
    {
        byte[] temp = new byte[length];
        Array.Copy(data, offset, temp, 0, length);
        Array.Reverse(temp);
        return temp;
    }

    // ��������
    void ScheduleReconnect()
    {
        Debug.Log("5���������...");
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
