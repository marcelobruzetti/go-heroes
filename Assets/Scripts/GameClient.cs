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

    if (stream == null)
    {
      tcpClient = new TcpClient();

      await tcpClient.ConnectAsync(host, port);
      stream = tcpClient.GetStream();

      _ = ReadLoop();

      Debug.Log("Connected to server");
    }

    await SendMessage(new ClientMessage
    {
      type = "queue"
    });
    Debug.Log("Queued for a match");

    // Debug.Log("Connected and queued for a match");
    // _ = ReadLoop();
  }

  // public async void QueueAgain()
  // {
  //   if (stream == null)
  //   {
  //     Debug.LogWarning("Cannot queue before connecting");
  //     return;
  //   }

  //   await SendMessage(new ClientMessage
  //   {
  //     type = "queue"
  //   });
  // }

  private async Task ReadLoop()
  {
    using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);

    while (true)
    {
      string line = await reader.ReadLineAsync();

      if (line == null)
      {
        Debug.Log("Server closed the connection");
        receivedEvents.Enqueue(new ServerEvent
        {
          type = "disconnected"
        });
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
      if (serverEvent.type == "disconnected")
      {
        stream?.Close();
        stream = null;

        tcpClient?.Close();
        tcpClient = null;
      }

      Debug.Log($"Server event: {serverEvent.type}");
      ServerEventReceived?.Invoke(serverEvent);
    }
  }

  private async Task SendMessage(ClientMessage message)
  {
    string json = JsonUtility.ToJson(message) + "\n";
    byte[] bytes = Encoding.UTF8.GetBytes(json);

    await stream.WriteAsync(bytes, 0, bytes.Length);
    await stream.FlushAsync();
  }

  public async void SendAction(string action)
  {
    if (stream == null)
    {
      Debug.LogWarning("Cannot send an action before connecting");
      return;
    }

    await SendMessage(new ClientMessage
    {
      type = "action",
      action = action
    });
  }
}