using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ElRaccoone.WebSockets
{

	public class SendPacket
	{
		public byte[] message;
		public TaskCompletionSource<byte[]> taskSource;
	}

	/// The WebSocket Connection class creates a WebSocket connection and handles
	/// all in and out going messages.
	public class WSConnection
	{

		/// Fixed chunk size for incomming messages.
		private int receiveChunkSize = 1024;

		/// Fixed check size for outgoing messages.
		private int sendChunkSize = 1024;

		/// Client WebSocket instance.
		private ClientWebSocket clientWebSocket;

		/// WebSocket server Uri.
		private Uri uri;

		/// Assigns the event listener raised when the WebSockets are connected.
		public void OnConnected(Action action) => this.hasOnConnected = (this.onConnect = action) != null;

		/// Defines whether the OnConnected event is assigned.
		private bool hasOnConnected;

		/// Callback Action invoked when the connection is estabalished.
		private Action onConnect;

		/// Assigns the event listener raised when the WebSockets are being disconnected.
		public void OnDisconnected(Action action) => this.hasOnDisconnected = (this.onDisconnected = action) != null;

		/// Defines whether the OnDisconnected event is assigned.
		private bool hasOnDisconnected;

		/// Callback Action invoked when the connection is ended.
		private Action onDisconnected;

		/// Assigns the event listener raised when the WebSockets do error.
		public void OnError(Action<Exception> action) => this.hasOnError = (this.onError = action) != null;

		/// Defines whether the OnError event is assigned.
		private bool hasOnError;

		/// Callback Action invoked when the connection did error.
		private Action<Exception> onError;

		/// Assigns the event listener raised when the WebSockets receive a message.
		public void OnMessage(Action<byte[]> action) => this.hasOnMessage = (this.onMessage = action) != null;

		/// Defines whether the OnMessage event is assigned.
		private bool hasOnMessage;

		/// Callback Action invoked when the connection receives a message.
		private Action<byte[]> onMessage;

		/// Defines whether the message queue is dispatching.
		private bool isMessageQueueDispatching = false;

		/// A queue containing all the messages that are queued to be sendt.
		private List<SendPacket> sendMessageQueue = new List<SendPacket>();

		/// A list of all WebSocket Connection instances.
		public static List<WSConnection> instances = new List<WSConnection>();

		/// Defines wheter the WebSocket is connected.
		protected bool isConnected { private set; get; }

		public virtual WebSocketCloseStatus? CloseStatus => this.clientWebSocket.CloseStatus;
		public virtual string CloseStatusDescription => this.clientWebSocket.CloseStatusDescription;
		public virtual ClientWebSocketOptions Options => this.clientWebSocket.Options;
		public virtual WebSocketState State => this.clientWebSocket.State;
		public virtual string SubProtocol => this.clientWebSocket.SubProtocol;

		/// Instanciates a new WebSocket connection.
		public WSConnection(string uri = null, string subProtocol = "Tls")
		{
			this.clientWebSocket = new ClientWebSocket();
			this.clientWebSocket.Options.AddSubProtocol(subProtocol);
			if (uri != null)
			{
				this.uri = new Uri(uri);
			}
			/// Adds the instance to the instaces array.
			WSConnection.instances.Add(this);
		}

		public virtual string Uri
		{
			get
			{
				return this.uri.OriginalString;
			}
			set
			{
				this.uri = new Uri(value);
			}
		}

		public virtual Task Connect(string uri)
		{
			this.uri = new Uri(uri);
			return this.Connect();
		}
		/// Connects to the WebSocket.
		public virtual async Task Connect()
		{
			//this.clientWebSocket = new ClientWebSocket();
			try
			{
				/// Tries connecting to the sockets async. No cancellation token will be
				/// provided. When connected, the onConnect event listener will be raised.
				await this.clientWebSocket.ConnectAsync(this.uri, CancellationToken.None);
				this.isConnected = true;
				if (this.hasOnConnected == true)
					this.onConnect();
				/// Starts awaiting web socket messages.
				this.ReceiveWebSocketMessage();
			}
			catch (Exception exception)
			{
				/// Whebnthe connection fails, the onError event listenr will be raised.
				if (this.hasOnError == true)
				{
					this.onError(exception);
					if (exception.InnerException != null)
						this.onError(exception.InnerException);
				}
			}
		}

		/// Sends a message using the dispatch queue. Messages will always be send
		/// in the order their added.
		public virtual Task<byte[]> SendMessage(byte[] message)
		{
			/// When the web sockets are not connected, we'll stop sending.
			if (this.isConnected == false)
			{
				return Task.FromException<byte[]>(new Exception("websocket not connected yed"));
			}
			var taskSource = new TaskCompletionSource<byte[]>();
			/// Adds the message to the message queue.
			this.sendMessageQueue.Add(new SendPacket()
			{
				message = message,
				taskSource = taskSource,
			});
			/// Starts the dispatch message queue if it's not already.
			if (this.isMessageQueueDispatching == false)
			{
				this.DispatchMessageQueue();
			}
			return taskSource.Task;
		}

		/// Sends a message using the dispatch queue. Messages will always be send
		/// in the order their added.
		public virtual void SendMessage(string message)
		{
			var _bytesToSend = new ArraySegment<byte>(
				Encoding.UTF8.GetBytes(message));
			this.SendMessage(_bytesToSend.Array);
		}

		/// Dispatches the message queueu, this sends all the messages in the queue
		/// using a async method. Messages will be sendt in the order they're added.
		private async void DispatchMessageQueue()
		{
			this.isMessageQueueDispatching = true;
			while (true)
			{
				/// Sends the oldest message from the queueu and removed it afterwards.
				// var _bytesToSend = new ArraySegment<byte>(
				// Encoding.UTF8.GetBytes(this.sendMessageQueue[0]));
				var packet = this.sendMessageQueue[0];
				var sendBytes = packet.message;
				bool sendOk = false;
				try
				{
					await this.clientWebSocket.SendAsync(
					  sendBytes, WebSocketMessageType.Binary, true, CancellationToken.None);
					sendOk = true;
				}
				catch (Exception e)
				{
					sendOk = false;
					packet.taskSource.SetException(e);
				}
				if (sendOk)
				{
					packet.taskSource.SetResult(sendBytes);
				}
				this.sendMessageQueue.RemoveAt(0);
				/// When the message queue is empty, we'll return out of the loop and
				/// set the flag to flase, ready for another message to be queued.
				if (this.sendMessageQueue.Count == 0)
				{
					this.isMessageQueueDispatching = false;
					return;
				}
			}
		}

		/// Receives WebSocket messages, keeps waiting async until messages are being
		/// received. When parsed and did not contain a close type, the method will
		/// be invoked again to wait for the next message.
		private async void ReceiveWebSocketMessage()
		{
			var _buffer = new ArraySegment<byte>(new byte[this.receiveChunkSize]);
			var _bytes = new List<byte>();
			var _result = null as WebSocketReceiveResult;
			/// Awaits a message to be received async. When receiving it will loop
			/// until the message of the end of the message has been received.
			do
			{
				_result = await this.clientWebSocket.ReceiveAsync(_buffer, CancellationToken.None);
				/// When a message is received it will streaned to the bytes buffer.
				for (int i = 0; i < _result.Count; i++)
					_bytes.Add(_buffer.Array[i]);
				/// If the WebSocket dis connects while receiving a messages, the client
				/// will be disposed and the receiving will be stopped.
				if (this.isConnected == false)
				{
					this.clientWebSocket.Dispose();
					return;
				}
			} while (!_result.EndOfMessage);
			/// When the message did receive a close event, the web socket will be set
			/// to disconnected, and the OnDisconnected event will be raised.
			if (_result.MessageType == WebSocketMessageType.Close)
			{
					this.isConnected = false;
				this.notifyDisconnected();
			}
			/// When the message is of any other type, it will be parsed into a string
			/// and the onMessage event will be raised.
			else
			{
				if (this.hasOnMessage == true)
                {
                    try
                    {
						// this.onMessage(Encoding.UTF8.GetString(_bytes.ToArray(), 0, _bytes.Count));
						this.onMessage(_bytes.ToArray());
					}catch (Exception ex)
                    {
						Debug.LogError(ex);
                    }
				}
				if(this.isConnected == true)
                {
					this.ReceiveWebSocketMessage();
				}
			}
		}

		/// Sets the receive and send chuck sizes. Keep in mind these should be to
		/// the power of two for the best results.
		public void SetChunkSize(int receive, int send)
		{
			this.receiveChunkSize = receive;
			this.sendChunkSize = send;
		}

		protected TaskCompletionSource<bool> disconnectTask = null;
		protected void notifyDisconnected()
        {
			if (this.hasOnDisconnected == true)
			{
                if (this.disconnectTask != null)
                {
					var dt = this.disconnectTask;
					this.disconnectTask = null;
					dt.TrySetResult(true);
                }
				this.onDisconnected();
			}
        }


		/// Disconnects from the WebSockets.
		public async Task Disconnect()
		{
			this.disconnectTask = new TaskCompletionSource<bool>();
			var dt = disconnectTask.Task;
			if (this.isConnected == true)
            {
			this.isConnected = false;
				this.clientWebSocket.Dispose();
                if (this.State == WebSocketState.Closed)
                {
					this.notifyDisconnected();
				}
			}
			await dt;
		}
	}
}
