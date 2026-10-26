using System.Net;
using System.Net.Sockets;

namespace MapleLib.PacketLib;

public class Connector
{
	public delegate void ClientConnectedHandler(Session session);

	private readonly Socket _socket;

	public event ClientConnectedHandler OnClientConnected;

	public Connector()
	{
		_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
	}

	public Session Connect(IPEndPoint ep)
	{
		_socket.Connect(ep);
		return CreateSession();
	}

	public Session Connect(IPAddress ip, int port)
	{
		_socket.Connect(ip, port);
		return CreateSession();
	}

	public Session Connect(IPAddress[] ip, int port)
	{
		_socket.Connect(ip, port);
		return CreateSession();
	}

	public Session Connect(string ip, int port)
	{
		_socket.Connect(ip, port);
		return CreateSession();
	}

	private Session CreateSession()
	{
		Session session = new Session(_socket, SessionType.CLIENT_TO_SERVER);
		if (OnClientConnected != null)
		{
			OnClientConnected(session);
		}
		session.WaitForDataNoEncryption();
		return session;
	}
}
