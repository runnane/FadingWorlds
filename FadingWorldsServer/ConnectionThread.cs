using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FadingWorldsServer.GameObjects;
using FadingWorldsServer.GameObjects.Living;
using fwlib;

namespace FadingWorldsServer{
	internal enum ConnectionState {
		NotConnected,
		Connected,
		LoggedIn
	}

	public class ConnectionThread : IDisposable {
		#region Private members

		private TcpClient _client;
		private StreamReader _clientReader;
		private StreamWriter _clientWriter;
		private DateTime _lastPacketRecieved;
		private DateTime _lastActivity;
		private DateTime _loginTime = DateTime.Now;
		public Player LoggedInUser;

		private bool _isClosed = true;

		private readonly char[] _delimSpace = {' '};
		private readonly char[] _delimPipe = {'|'};

		private bool _lineOccupied;
		private string _clientVersion = string.Empty;
		private readonly int _threadID;
		private readonly string _randomConID = string.Empty;

		private ConnectionState _state;
		public Dictionary<string, string> Variables = new Dictionary<string, string>();

		#endregion

		#region Constructors

		public ConnectionThread(TcpClient c) {
			_state = ConnectionState.NotConnected;
			_randomConID = new Random().Next(1000000, 9999999).ToString();
			ConsoleWrite("New connection from " + c.Client.RemoteEndPoint);

			_client = c;
			_isClosed = false;
			_clientReader = new StreamReader(_client.GetStream(), Encoding.GetEncoding(28591));
			_clientWriter = new StreamWriter(_client.GetStream(), Encoding.GetEncoding(28591));


			_threadID = Thread.CurrentThread.GetHashCode();
		}

		#endregion

		#region Getters and Setters

		/// <summary>
		/// Gets version client is running
		/// </summary>
		public string ClientVersion {
			get { return _clientVersion; }
		}

		/// <summary>
		/// Gets current thread ID
		/// </summary>
		public int ThreadID {
			get { return _threadID; }
		}

		/// <summary>
		/// Gets if user is logged in
		/// </summary>
		public bool IsLoggedIn {
			get { return LoggedInUser != null; }
		}

		/// <summary>
		/// Gets a string with time logged in
		/// </summary>
		public string TimeLoggedIn {
			get { return _loginTime.ToString(); }
		}

		/// <summary>
		/// Gets the random generated connection id
		/// </summary>
		public string ConnectionID {
			get { return _randomConID; }
		}

		/// <summary>
		/// Gets number of seconds since last packed was recieved from client
		/// </summary>
		public int SecondsSinceLastPacket {
			get {
				TimeSpan t = (DateTime.Now - _lastPacketRecieved);
				return (int) t.TotalSeconds;
			}
		}

		/// <summary>
		/// Gets seconds connection has been idle (no active usage besides connectionchecks)
		/// </summary>
		public int SecondsIdle {
			get {
				TimeSpan t = (DateTime.Now - _lastActivity);
				return (int) t.TotalSeconds;
			}
		}

		/// <summary>
		/// Gets a string with duration connection has been active
		/// </summary>
		public string OnlineTime {
			get {
				TimeSpan t = (DateTime.Now - _loginTime);
				return t.Days + "d " + t.Hours + "h " + t.Minutes + "m " + t.Seconds + "s";
			}
		}

		#endregion

		public void Close() {
			ConsoleWrite("Starting to close connection");
			SendCommand("ms|system|Connection is being closed.");

			if (LoggedInUser != null)
			{
				FadingWorldsServer.Instance.RemoveEntity(LoggedInUser);
			}
			_client.Client.Close();
			_client.Close();
			ConnectionThread a = this;
			FadingWorldsServer.Instance.TCPPool.Remove(ref a);
			if (!_isClosed && IsLoggedIn && LoggedInUser != null)
			{
				_isClosed = true;
				FadingWorldsServer.Instance.TCPPool.SendMessageToAll("us|logout|" + LoggedInUser.Id);
				SendUserList();
			}
			LoggedInUser = null;
		}

		public void ConsoleWrite(string s) {
			Console.WriteLine("[" + _threadID + "|" + _randomConID + "|" + LoggedInUser + "]> " + s);
		}

		public void StartListen() {
			ConsoleWrite(" Starting listening, waiting for command");
			string input = "";
			_lastActivity = DateTime.Now;
			_state = ConnectionState.Connected;

			while (_client.Client.Connected && _client.GetStream().CanRead) {
				// Wait for command
				try {
					input = _clientReader.ReadLine();
					ConsoleWrite(" <- [" + input + "]");
				}
				catch (IOException) {}
				catch (ObjectDisposedException) {
					break;
				}
				catch (Exception ex) {
					Console.WriteLine(ex.ToString());
					break;
				}
				if (input == null) {
					ConsoleWrite("Connection closed at remote end");
					break;
				}
				_lastPacketRecieved = DateTime.Now;
				if (input.Trim().Length > 0 && _client.Client.Connected && _client.GetStream().CanRead) {
					try {
						if (!ParseIncomingCommand(input)) {
							ConsoleWrite("ParseIncomingCommand() returned false :(");
							break;
						}
					}
					catch (Exception ex) {
						ConsoleWrite("Exception!!" + ex);
						SendCommand("ms|system|Your command caused the server to throw an exception (" + ex.Message + ")");
						if (!IsLoggedIn) {
							break;
						}
					}
				}
			} //while
			Close();
		}

		private bool ParseIncomingCommand(string input) {
			string[] parameters = {};
			string[] primaryParts = input.Split(_delimPipe, 2);
			if (primaryParts.Length > 1) {
				parameters = primaryParts[1].Split(_delimPipe);
			}
			return HandleCode(primaryParts[0].Trim().ToLower(), parameters);
		}

		private bool HandleCode(string opCode, string[] parm) {
			if ((_state != ConnectionState.LoggedIn || !IsLoggedIn) && opCode != "au") {
				ConsoleWrite("ERROR: AUTH not competed. Closing connection");
				return false;
			}
			_lastActivity = DateTime.Now;

			switch (opCode) {
				case "mv":
					if (parm.Count() == 3) {
						string who = parm[0]; // should be "self". TODO: fix security
						int col = int.Parse(parm[1]);
						int row = int.Parse(parm[2]);
						LoggedInUser.MoveTo(new Position2D(col, row));
					}
					else {
						ConsoleWrite("Error on command mv. Unknown parameter count: " + parm.Count());
					}
					break;
				case "ms":
					// Message
					_lastActivity = DateTime.Now;
					if (parm.Any()) {
						if (parm[0].Trim().Length > 0) {
							FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ms|" + LoggedInUser + "|" + parm[0].Trim());
						}
						else {
							// Empty message
						}
					}
					else {
						// no parameters??
					}

					break;
				case "au":
					return ParseAuthCommand(parm);
				case "da":
					if (parm.Any()) {
						string subCode = parm[0];
						switch (subCode) {
							case "getmap":
								SendCommand("da|map|" + FadingWorldsServer.Instance.TheGrid.MakeDump());
								break;
							case "maploaded":
								SendCommand("da|pleaseloadgame");
								break;
							case "gameloaded":
								FadingWorldsServer.Instance.TCPPool.SendMessageToAllButUser(LoggedInUser.Username,
								                                                            "us|login|" + LoggedInUser.MakeDump());

								SendCommand("da|initplayer|" + LoggedInUser.MakeDump());
								foreach (Entity entity in FadingWorldsServer.Instance.GameObjects) {
									SendCommand("da|initentity|" + entity.MakeDump());
								}
								SendUserList();
								FadingWorldsServer.Instance.TheGrid.GetBlockAt(LoggedInUser.Position).Entities.Add(LoggedInUser);
								break;
						}
					}

					break;
				case "qt": // Pure quit message
					ConsoleWrite("Got quit command, so returning false on request.");
					return false;
				case "sy":
					if (parm.Any()) {
						string subCode = parm[0];
						if (subCode == "po") {
							ConsoleWrite("Replied to ping");
						}
					}
					break;
				case "at":
					if (parm.Length == 3) {
						string mobId = parm[0];
						int mobX = int.Parse(parm[1]);
						int mobY = int.Parse(parm[2]);
						ConsoleWrite("Player " + LoggedInUser.Id + " doing an attack move on " + mobId);
						LoggedInUser.TryAttack(mobId);
						//MoveResult x = LoggedInUser.MoveTo(new Position2D(mobX, mobY));

						//if (subCode == "po")
						//{
						//  ConsoleWrite("Replied to ping");
						//}
					}
					break;
				case "pm":
					if (parm.Count() == 2) {
						string target = parm[0];
						string message = parm[1];
						SendCommand("ms|system|<<pm to " + LoggedInUser + ":" + message);
						FadingWorldsServer.Instance.TCPPool.SendMessageToUser(target, "system|>>pm from " + LoggedInUser + ":" + message);
					}
					else {
						SendCommand("ms|system|i'm afraid you need more than that");
					}
					break;

				default:
					ConsoleWrite("ParseGuiCommand():  Sent unknown opCode: " + opCode);
					SendCommand("ms|system|unknown-cmd");
					break;
			}
			// Default to success
			return true;
		}

		//private bool ParseCommand(string input) {
		//  if (input.StartsWith("sy|po")) {
		//    // Ping reply
		//    ConsoleWrite("Replied to ping");
		//  }
		//  else if (input.StartsWith("pm")) {
		//  return true;
		//}

		private bool ParseAuthCommand(string[] parms) {
			if (!parms.Any()) {
				ConsoleWrite("Missing parametres to auth command");
				return false;
			}
			string command = parms[0];
			switch (command) {
				case "helo":
					if (parms.Length == 2) {
						string version = parms[1];
						_clientVersion = version;

						//check for outdated version
                        //if (version == "1.0.0.0")
                        //{
                        //    SendCommand("au|oldversion");
                        //    return false;
                        //}
						SendCommand("au|authplease");
						return true;
					}
					else {
						ConsoleWrite("Missing version to auth helo command");
						return false;
					}

				case "login":
					if (parms.Length == 3) {
						string username = parms[1];
						string password = parms[2];
						//string ip = _client.Client.RemoteEndPoint.ToString().Split(':')[0];

						// Check if user exists
						var users = FadingWorldsServer.Instance.UserDB.Users.Where(e => e.Username.ToLower().Equals(username.ToLower()));
						if (!users.Any()) {
							ConsoleWrite("Unknown user: " + username);
							SendCommand("au|failed");
							return false;
						}

						var login = users.Single();

						// Check if password is correct
						if (login.Password.Equals(password)) {
							// Log out if logged in already
							if (FadingWorldsServer.Instance.TCPPool.IsUserLoggedIn(username)) {
								FadingWorldsServer.Instance.TCPPool.SendMessageToUser(username, "sy|logged-in-on-another-location");
								FadingWorldsServer.Instance.TCPPool.DisconnectUser(username);
							}

							if (login.Health <= 0) {
								login.Reset();
							}

							LoggedInUser = login;
							_state = ConnectionState.LoggedIn;
							ConsoleWrite("Username: " + LoggedInUser.Username);
							SendCommand("au|ok|" + ConnectionID);
							if (LoggedInUser.Position.IsInvalid) {
								LoggedInUser.Position = FadingWorldsServer.Instance.TheGrid.FindRandomEmptyGrassBlock();
							}


							return true;
						}
						else {
							ConsoleWrite("Invalid password!");
							SendCommand("au|failed");
							return false;
						}
					}
					else {
						ConsoleWrite("wrong number of args");
						SendCommand("au|failed");
						return false;
						// wrong number of args
					}


					//case "version":
					//  if (parms.Length == 2) {
					//    string version = parms[1];
					//    _clientVersion = version;
					//    ConsoleWrite("	- Version: " + _clientVersion);
					//    SendCommand("au|ok");
					//    return true;
					//    //_parentProcess.PreDB.ResetObject(lgn);
					//  }
					//  else {
					//    ConsoleWrite("wrong number of args");
					//    SendCommand("au|failed");
					//    return false;
					//  }


				default:
					ConsoleWrite("unknown format on auth command ");
					SendCommand("au|failed");
					return false;
			}
		}


		public void SendUserList() {
			FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ul|" + FadingWorldsServer.Instance.TCPPool.GenerateUserList());
		}


		public void SendCommand(string output) {
			try {
				if (!_lineOccupied && !_isClosed && _clientReader.BaseStream.CanWrite && _client.Connected) {
					while (_lineOccupied) {
						Thread.Sleep(50);
					}
					_lineOccupied = true;
					_clientWriter.WriteLine(output);
					_clientWriter.Flush();
					ConsoleWrite(" -> [" + output + "]");
					_lineOccupied = false;
				}
			}
			catch (Exception ex) {
				ConsoleWrite("[-]: sendCommand() exception: " + ex);
			}
		}

		//private static string GetTimespan(int secs) {
		//  if (secs > 0) {
		//    int[] a1 = {31536000, 604800, 86400, 3600, 60, 1};
		//    int[] a2 = {0, 52, 7, 24, 60, 60};
		//    char[] a3 = {'y', 'w', 'd', 'h', 'm', 's'};

		//    string duration = "";
		//    for (int i = 0; i < 6; i++) {
		//      int num = secs/a1[i];
		//      if (a2[i] > 0) num = num%a2[i];
		//      if (num > 0) duration += "" + num + a3[i] + " ";
		//    }
		//    return duration.TrimEnd();
		//  }
		//  return "0s";
		//}

		#region IDisposable Members

		public void Dispose() {
			try {
				SendCommand("ms|system|DISPOSING CONNECTION!");
				if (_clientWriter != null) {
					_clientWriter.Close();
					_clientWriter.Dispose();
				}
				if (_clientReader != null) {
					_clientReader.Close();
					_clientReader.Dispose();
				}
				_clientWriter = null;
				_clientReader = null;

				if (_client != null) {
					if (_client.Client != null) {
						_client.Client.Close();
						_client.Client = null;
					}
					_client.Close();
				}

				_client = null;
			}
			catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
		}

		#endregion
	}
}