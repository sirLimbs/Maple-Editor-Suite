using System;
using System.Net;
using System.Net.Sockets;
using MapleLib.Helpers;

namespace MapleLib.PacketLib;

public class Acceptor
{
	public delegate void ClientConnectedHandler(Session session);

	private readonly Socket _listener;

	public event ClientConnectedHandler OnClientConnected;

	public Acceptor()
	{
		_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
	}

	public void StartListening(int port)
	{
		_listener.Bind(new IPEndPoint(IPAddress.Any, port));
		_listener.Listen(15);
		_listener.BeginAccept(OnClientConnect, null);
	}

	public void StopListening()
	{
		_listener.Disconnect(reuseSocket: true);
	}

	private void OnClientConnect(IAsyncResult iar)
	{
		try
		{
			Socket socket = _listener.EndAccept(iar);
			Session session = new Session(socket, SessionType.SERVER_TO_CLIENT);
			if (OnClientConnected != null)
			{
				OnClientConnected(session);
			}
			session.WaitForData();
			_listener.BeginAccept(OnClientConnect, null);
		}
		catch (ObjectDisposedException)
		{
			ErrorLogger.Log(ErrorLevel.Critical, "[Error] OnClientConnect: Socket closed.");
		}
		catch (Exception ex2)
		{
			ErrorLogger.Log(ErrorLevel.Critical, "[Error] OnClientConnect: " + ex2);
		}
	}
}
