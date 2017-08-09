﻿using Intersect.Server.Classes.Networking.Websockets;
using WebSocketSharp.Server;

namespace Intersect.Server.Classes.Networking
{
    public static class WebSocketNetwork
    {
        private static WebSocketSharp.Server.WebSocketServer _listener;

        public static void Init(int port)
        {
            _listener = new WebSocketSharp.Server.WebSocketServer(port);
            _listener.AddWebSocketService<SharpServerService>("/Intersect");
            _listener.Start();
        }

        public static void Stop()
        {
        }
    }

    public class SharpServerService : WebSocketBehavior
    {
        public SharpServerService() : base()
        {
            IgnoreExtensions = true;
            Protocol = "binary";
        }

        protected override void OnOpen()
        {
            var connection = new WebSocketConnection(Context, this);
        }
    }
}