using System;
using System.Net.Sockets;
using MapleLib.Helpers;
using MapleLib.MapleCryptoLib;

namespace MapleLib.PacketLib;

public class Session
{
	public delegate void PacketReceivedHandler(PacketReader packet, bool mIsInit);

	public delegate void ClientDisconnectedHandler(Session session);

	public delegate void InitPacketReceived(short version, byte serverIdentifier);

	private readonly Socket _socket;

	private SessionType _type;

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

	public SessionType Type => _type;

	public event PacketReceivedHandler OnPacketReceived;

	public event ClientDisconnectedHandler OnClientDisconnected;

	public event InitPacketReceived OnInitPacketReceived;

	public Session(Socket socket, SessionType type)
	{
		_socket = socket;
		_type = type;
	}

	public void WaitForData()
	{
		WaitForData(new SocketInfo(_socket, 4));
	}

	public void WaitForDataNoEncryption()
	{
		WaitForData(new SocketInfo(_socket, 2, noEncryption: true));
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
			if (socketInfo.Index == socketInfo.DataBuffer.Length)
			{
				switch (socketInfo.State)
				{
				case SocketInfo.StateEnum.Header:
				{
					if (socketInfo.NoEncryption)
					{
						PacketReader packetReader2 = new PacketReader(socketInfo.DataBuffer);
						short num3 = packetReader2.ReadShort();
						socketInfo.State = SocketInfo.StateEnum.Content;
						socketInfo.DataBuffer = new byte[num3];
						socketInfo.Index = 0;
						WaitForData(socketInfo);
						break;
					}
					PacketReader packetReader3 = new PacketReader(socketInfo.DataBuffer);
					byte[] array = packetReader3.ToArray();
					int num4 = packetReader3.ReadInt();
					short num5 = (short)MapleCrypto.GetPacketLength(num4);
					if (_type == SessionType.SERVER_TO_CLIENT && !_RIV.CheckPacketToServer(BitConverter.GetBytes(num4)))
					{
						ErrorLogger.Log(ErrorLevel.Critical, "[Error] Packet check failed. Disconnecting client.");
					}
					socketInfo.State = SocketInfo.StateEnum.Content;
					socketInfo.DataBuffer = new byte[num5];
					socketInfo.Index = 0;
					WaitForData(socketInfo);
					break;
				}
				case SocketInfo.StateEnum.Content:
				{
					byte[] dataBuffer = socketInfo.DataBuffer;
					if (socketInfo.NoEncryption)
					{
						socketInfo.NoEncryption = false;
						PacketReader packetReader = new PacketReader(dataBuffer);
						short num2 = packetReader.ReadShort();
						string text = packetReader.ReadMapleString();
						_SIV = new MapleCrypto(packetReader.ReadBytes(4), num2);
						_RIV = new MapleCrypto(packetReader.ReadBytes(4), num2);
						byte serverIdentifier = packetReader.ReadByte();
						if (_type == SessionType.CLIENT_TO_SERVER)
						{
							OnInitPacketReceived(num2, serverIdentifier);
						}
						OnPacketReceived(new PacketReader(dataBuffer), mIsInit: true);
						WaitForData();
					}
					else
					{
						_RIV.Crypt(dataBuffer);
						MapleCustomEncryption.Decrypt(dataBuffer);
						if (dataBuffer.Length != 0 && OnPacketReceived != null)
						{
							OnPacketReceived(new PacketReader(dataBuffer), mIsInit: false);
						}
						WaitForData();
					}
					break;
				}
				}
			}
			else
			{
				ErrorLogger.Log(ErrorLevel.Critical, "[Warning] Not enough data");
				WaitForData(socketInfo);
			}
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

	public void SendInitialPacket(int pVersion, string pPatchLoc, byte[] pRIV, byte[] pSIV, byte pServerType)
	{
		PacketWriter packetWriter = new PacketWriter();
		packetWriter.WriteShort((pPatchLoc == "") ? 13 : 14);
		packetWriter.WriteShort(pVersion);
		packetWriter.WriteMapleString(pPatchLoc);
		packetWriter.WriteBytes(pRIV);
		packetWriter.WriteBytes(pSIV);
		packetWriter.WriteByte(pServerType);
		SendRawPacket(packetWriter);
	}

	public void SendPacket(PacketWriter packet)
	{
		SendPacket(packet.ToArray());
	}

	public void SendPacket(byte[] input)
	{
		byte[] array = new byte[input.Length + 4];
		byte[] src = ((_type == SessionType.SERVER_TO_CLIENT) ? _SIV.GetHeaderToClient(input.Length) : _SIV.GetHeaderToServer(input.Length));
		MapleCustomEncryption.Encrypt(input);
		_SIV.Crypt(input);
		Buffer.BlockCopy(src, 0, array, 0, 4);
		Buffer.BlockCopy(input, 0, array, 4, input.Length);
		SendRawPacket(array);
	}

	public void SendRawPacket(PacketWriter pPacket)
	{
		SendRawPacket(pPacket.ToArray());
	}

	public void SendRawPacket(byte[] buffer)
	{
		_socket.Send(buffer);
	}
}
