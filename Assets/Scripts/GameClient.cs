using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

[Preserve]
public class GameClient : MonoBehaviour
{
  private TcpClient tcpClient;
  private NetworkStream stream;
  private readonly ConcurrentQueue<ServerEvent> receivedEvents = new ConcurrentQueue<ServerEvent>();
  public event Action<ServerEvent> ServerEventReceived;

  [SerializeField] private string host = "142.93.65.91";
  [SerializeField] private int port = 8081;
  [SerializeField] private string webSocketUrl;

#if UNITY_WEBGL && !UNITY_EDITOR
  [DllImport("__Internal")] private static extern void GoHeroesWebSocketConnect(string url);
  [DllImport("__Internal")] private static extern void GoHeroesWebSocketSend(string message);
  [DllImport("__Internal")] private static extern void GoHeroesWebSocketClose();

  private bool webSocketConnected;
  private bool queueWhenConnected;
  private bool webSocketClosedByClient;
#endif

  [ContextMenu("Connect and queue")]
  public void QueueForMatchmaking()
  {
#if UNITY_WEBGL && !UNITY_EDITOR
    if (!webSocketConnected)
    {
      queueWhenConnected = true;
      webSocketClosedByClient = false;
      GoHeroesWebSocketConnect(WebSocketEndpoint());
      return;
    }

    SendWebSocketMessage(new ClientMessage { type = "queue" });
#else
    _ = QueueForMatchmakingTcp();
#endif
  }

  private async Task QueueForMatchmakingTcp()
  {
    if (stream == null)
    {
      try
      {
        tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port);
        stream = tcpClient.GetStream();
        _ = ReadLoop();
        Debug.Log("Connected to server");
      }
      catch (SocketException exception)
      {
        Debug.LogWarning($"Could not connect to server: {exception.Message}");
        ClearConnection();
        receivedEvents.Enqueue(new ServerEvent {
          type = "connection_failed",
          message = "Could not connect to the server. Please try again."
        });
        return;
      }
    }

    await SendTcpMessage(new ClientMessage { type = "queue" });
    Debug.Log("Queued for a match");
  }

  private async Task ReadLoop()
  {
    using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
    while (true)
    {
      string line = await reader.ReadLineAsync();
      if (line == null)
      {
        Debug.Log("Server closed the connection");
        receivedEvents.Enqueue(new ServerEvent { type = "disconnected" });
        return;
      }

      EnqueueServerEvent(line);
    }
  }

  private void Update()
  {
    while (receivedEvents.TryDequeue(out ServerEvent serverEvent))
    {
      if (serverEvent.type == "disconnected") ClearConnection();
      Debug.Log($"Server event: {serverEvent.type}");
      ServerEventReceived?.Invoke(serverEvent);
    }
  }

  private void EnqueueServerEvent(string json)
  {
    receivedEvents.Enqueue(JsonUtility.FromJson<ServerEvent>(json));
  }

  private async Task SendTcpMessage(ClientMessage message)
  {
    string json = JsonUtility.ToJson(message) + "\n";
    byte[] bytes = Encoding.UTF8.GetBytes(json);
    await stream.WriteAsync(bytes, 0, bytes.Length);
    await stream.FlushAsync();
  }

  public void SendAction(string action)
  {
#if UNITY_WEBGL && !UNITY_EDITOR
    if (!webSocketConnected)
    {
      Debug.LogWarning("Cannot send an action before connecting");
      return;
    }
    SendWebSocketMessage(new ClientMessage { type = "action", action = action });
#else
    if (stream == null)
    {
      Debug.LogWarning("Cannot send an action before connecting");
      return;
    }
    _ = SendTcpMessage(new ClientMessage { type = "action", action = action });
#endif
  }

#if UNITY_WEBGL && !UNITY_EDITOR
  private string WebSocketEndpoint()
  {
    if (!string.IsNullOrEmpty(webSocketUrl)) return webSocketUrl;

    Uri pageUrl = new Uri(Application.absoluteURL);
    string scheme = pageUrl.Scheme == "https" ? "wss" : "ws";
    return $"{scheme}://{pageUrl.Authority}/ws";
  }

  private void SendWebSocketMessage(ClientMessage message)
  {
    GoHeroesWebSocketSend(JsonUtility.ToJson(message));
  }

  // These methods are called by Assets/Plugins/WebGL/GoHeroesWebSocket.jslib.
  public void OnWebSocketOpen()
  {
    webSocketConnected = true;
    if (!queueWhenConnected) return;
    queueWhenConnected = false;
    SendWebSocketMessage(new ClientMessage { type = "queue" });
  }

  public void OnWebSocketMessage(string json)
  {
    EnqueueServerEvent(json);
  }

  public void OnWebSocketError(string message)
  {
    Debug.LogWarning($"WebSocket error: {message}");
    receivedEvents.Enqueue(new ServerEvent {
      type = "connection_failed",
      message = "Could not connect to the server. Please try again."
    });
  }

  public void OnWebSocketClose()
  {
    bool wasClosedByClient = webSocketClosedByClient;
    webSocketConnected = false;
    queueWhenConnected = false;
    if (!wasClosedByClient) receivedEvents.Enqueue(new ServerEvent { type = "disconnected" });
  }
#endif

  private void ClearConnection()
  {
#if UNITY_WEBGL && !UNITY_EDITOR
    webSocketClosedByClient = true;
    webSocketConnected = false;
    queueWhenConnected = false;
    GoHeroesWebSocketClose();
#else
    stream?.Close();
    stream = null;
    tcpClient?.Close();
    tcpClient = null;
#endif
  }

  private void OnDestroy()
  {
    ClearConnection();
  }
}
