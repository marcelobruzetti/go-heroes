using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System;

public class GameClient : MonoBehaviour
{
  private TcpClient tcpClient;
  private NetworkStream stream;
  private readonly ConcurrentQueue<ServerEvent> receivedEvents =
    new ConcurrentQueue<ServerEvent>();
  public event Action<ServerEvent> ServerEventReceived;

  [SerializeField] private string host = "127.0.0.1";
  [SerializeField] private int port = 3000;

  [ContextMenu("Connect and queue")]
  public async void QueueForMatchmaking()
  {
    tcpClient = new TcpClient();

    await tcpClient.ConnectAsync(host, port);
    stream = tcpClient.GetStream();

    var message = new ClientMessage
    {
      type = "queue"
    };

    string json = JsonUtility.ToJson(message) + "\n";
    byte[] bytes = Encoding.UTF8.GetBytes(json);

    await stream.WriteAsync(bytes, 0, bytes.Length);
    await stream.FlushAsync();

    Debug.Log("Connected and queued for a match");
    _ = ReadLoop();
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
        return;
      }

      ServerEvent serverEvent = JsonUtility.FromJson<ServerEvent>(line);
      receivedEvents.Enqueue(serverEvent);
    }
  }

  private void Update()
  {
    while (receivedEvents.TryDequeue(out ServerEvent serverEvent))
    {
      Debug.Log($"Server event: {serverEvent.type}");
      ServerEventReceived?.Invoke(serverEvent);
    }
  }
}