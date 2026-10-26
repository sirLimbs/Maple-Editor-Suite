using System;
using System.Net;
using System.Net.Sockets;
using MapleLib.Helpers;
using MapleLib.MapleCryptoLib;

namespace MapleLib.PacketLib;

public class Monitor
{
	public delegate void PacketReceivedHandler(PacketReader packet);

	public delegate void ClientDisconnectedHandler(Monitor monitor);

	private readonly Socket _socket;

	private MapleCrypto _RIV;

	private MapleCrypto _SIV;

	public MapleCrypto RIV
	{
		get
		{
			return _RIV;
		}
		set
		{
			_RIV = value;
		}
	}

	public MapleCrypto SIV
	{
		get
		{
			return _SIV;
		}
		set
		{
			_SIV = value;
		}
	}

	public Socket Socket => _socket;

	public event PacketReceivedHandler OnPacketReceived;

	public event ClientDisconnectedHandler OnClientDisconnected;

	public Monitor()
	{
		_socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
	}

	public void StartMonitoring(IPAddress IP)
	{
		_socket.Bind(new IPEndPoint(IP, 0));
		_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AcceptConnection, optionValue: true);
		byte[] optionInValue = new byte[4] { 1, 0, 0, 0 };
		byte[] optionOutValue = null;
		_socket.IOControl(IOControlCode.ReceiveAll, optionInValue, optionOutValue);
		WaitForData();
	}

	public void WaitForData()
	{
		WaitForData(new SocketInfo(_socket, short.MaxValue));
	}

	private void WaitForData(SocketInfo socketInfo)
	{
		try
		{
			_socket.BeginReceive(socketInfo.DataBuffer, socketInfo.Index, socketInfo.DataBuffer.Length - socketInfo.Index, SocketFlags.None, OnDataReceived, socketInfo);
		}
		catch (Exception ex)
		{
			ErrorLogger.Log(ErrorLevel.Critical, "[Error] Session.WaitForData: " + ex);
		}
	}

	private void OnDataReceived(IAsyncResult iar)
	{
		SocketInfo socketInfo = (SocketInfo)iar.AsyncState;
		try
		{
			int num = socketInfo.Socket.EndReceive(iar);
			if (num == 0)
			{
				if (OnClientDisconnected != null)
				{
					OnClientDisconnected(this);
				}
				return;
			}
			socketInfo.Index += num;
			byte[] array = new byte[num];
			Buffer.BlockCopy(socketInfo.DataBuffer, 0, array, 0, num);
			if (OnPacketReceived != null)
			{
				OnPacketReceived(new PacketReader(array));
			}
			WaitForData();
		}
		catch (ObjectDisposedException)
		{
			ErrorLogger.Log(ErrorLevel.Critical, "[Error] Session.OnDataReceived: Socket has been closed");
		}
		catch (SocketException ex2)
		{
			if (ex2.ErrorCode != 10054)
			{
				ErrorLogger.Log(ErrorLevel.Critical, "[Error] Session.OnDataReceived: " + ex2);
			}
		}
		catch (Exception ex3)
		{
			ErrorLogger.Log(ErrorLevel.Critical, "[Error] Session.OnDataReceived: " + ex3);
		}
	}
}
