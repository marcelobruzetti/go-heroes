mergeInto(LibraryManager.library, {
  GoHeroesWebSocketConnect: function (urlPointer) {
    var url = UTF8ToString(urlPointer);
    if (window.goHeroesWebSocket) window.goHeroesWebSocket.close();

    var socket = new WebSocket(url);
    window.goHeroesWebSocket = socket;
    socket.onopen = function () {
      if (window.goHeroesWebSocket === socket) SendMessage("Game Client", "OnWebSocketOpen");
    };
    socket.onmessage = function (event) {
      if (window.goHeroesWebSocket === socket && typeof event.data === "string") {
        SendMessage("Game Client", "OnWebSocketMessage", event.data);
      }
    };
    socket.onerror = function () {
      if (window.goHeroesWebSocket === socket) {
        SendMessage("Game Client", "OnWebSocketError", "The browser could not connect to the game server.");
      }
    };
    socket.onclose = function () {
      if (window.goHeroesWebSocket === socket) {
        window.goHeroesWebSocket = null;
        SendMessage("Game Client", "OnWebSocketClose");
      }
    };
  },

  GoHeroesWebSocketSend: function (messagePointer) {
    var socket = window.goHeroesWebSocket;
    if (socket && socket.readyState === WebSocket.OPEN) socket.send(UTF8ToString(messagePointer));
  },

  GoHeroesWebSocketClose: function () {
    var socket = window.goHeroesWebSocket;
    if (socket) socket.close();
  }
});
