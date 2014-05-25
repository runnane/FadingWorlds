using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FadingWorldsServer.GameObjects;
using FadingWorldsServer.GameObjects.Living;
using fwlib;
using ProtoBuf;

namespace FadingWorldsServer{
    public class ConnectionThread : IDisposable {
		#region Private members

		private readonly TcpClient _client;
        private readonly FadingWorldsServer _server;
		private DateTime _lastPacketRecieved;
		private DateTime _lastActivity;
		private DateTime _loginTime = DateTime.Now;
		public Player LoggedInUser;

		private bool _isClosed = true;

        private bool _lineOccupied;
		private string _clientVersion = string.Empty;
		private readonly int _threadId;
		private readonly string _randomConId = string.Empty;

		private ConnectionState _state;
		public Dictionary<string, string> Variables = new Dictionary<string, string>();

		#endregion

		#region Constructors

		public ConnectionThread(TcpClient c, FadingWorldsServer p) {
			_state = ConnectionState.NotConnected;
			_randomConId = new Random().Next(1000000, 9999999).ToString();
			ConsoleWrite("New connection from " + c.Client.RemoteEndPoint);

		    _server = p;
			_client = c;
			_isClosed = false;
			_threadId = Thread.CurrentThread.GetHashCode();
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
		public int ThreadId {
			get { return _threadId; }
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
		public string ConnectionId {
			get { return _randomConId; }
		}

		/// <summary>
		/// Gets number of seconds since last packed was recieved from client
		/// </summary>
		public int SecondsSinceLastPacket {
			get {
				var t = (DateTime.Now - _lastPacketRecieved);
				return (int) t.TotalSeconds;
			}
		}

		/// <summary>
		/// Gets seconds connection has been idle (no active usage besides connectionchecks)
		/// </summary>
		public int SecondsIdle {
			get {
				var t = (DateTime.Now - _lastActivity);
				return (int) t.TotalSeconds;
			}
		}

		/// <summary>
		/// Gets a string with duration connection has been active
		/// </summary>
		public string OnlineTime {
			get {
				var t = (DateTime.Now - _loginTime);
				return t.Days + "d " + t.Hours + "h " + t.Minutes + "m " + t.Seconds + "s";
			}
		}

		#endregion

		public void Close() {
			ConsoleWrite("Starting to close connection");
            SendPayload(new NetworkPayload
            {
                Type = PayloadType.Message,
                Params = { "system", "Connection is being closed." }
            });
			

			if (LoggedInUser != null)
			{
				FadingWorldsServer.Instance.RemoveEntity(LoggedInUser);
			}
			_client.Client.Close();
			_client.Close();
			var a = this;
			FadingWorldsServer.Instance.TCPPool.Remove(ref a);
			if (!_isClosed && IsLoggedIn && LoggedInUser != null)
			{
				_isClosed = true;
                FadingWorldsServer.Instance.TCPPool.SendPayloadToAll(new NetworkPayload
                {
                    Type = PayloadType.User,
                    Command = PayloadCommand.Logout,
                    Params = { LoggedInUser.Id }
                });
              
				
				SendUserList();
			}
			LoggedInUser = null;
		}

		public void ConsoleWrite(string s) {
			Console.WriteLine("[" + _threadId + "|" + _randomConId + "|" + LoggedInUser + "]> " + s);
		}

		public void StartListen() {
			ConsoleWrite(" Starting listening, waiting for command");
			_lastActivity = DateTime.Now;
			_state = ConnectionState.Connected;
		    var payload = new NetworkPayload();
			while (_client.Client.Connected && _client.GetStream().CanRead) {
				// Wait for command
				try {
                    payload = Serializer.DeserializeWithLengthPrefix<NetworkPayload>(_client.GetStream(), PrefixStyle.Base128);
				}
				catch (IOException) {}
				catch (ObjectDisposedException) {
					break;
				}
				catch (Exception ex) {
					Console.WriteLine(ex.ToString());
					break;
				}
                if (payload == null || payload.Type == PayloadType.Unset)
                {
                    ConsoleWrite("Empty or unset payload, skipping.");
                   // continue;
                    break;
                }
                ConsoleWrite(" [in ] <- [" + payload + "]");
                _lastPacketRecieved = DateTime.Now;
			    if (!_client.Client.Connected || !_client.GetStream().CanRead) continue;

			    try
			    {
			        if (HandlePayload(payload)) continue;

			        ConsoleWrite("HandlePayload() returned false :(");
			        break;
			    }
			    catch (Exception ex)
			    {
			        ConsoleWrite("Exception!!" + ex);
			        SendPayload(new NetworkPayload
			        {
			            Type = PayloadType.Message,
			            Params = { "system,","Your command caused the server to throw an exception (" + ex.Message + ")" }
			        });
			        
			        if (!IsLoggedIn)
			        {
			            break;
			        }
			    }
			} //while
			Close();
		}

        private bool HandlePayload(NetworkPayload payload)
	    {
	        if ((_state != ConnectionState.LoggedIn || !IsLoggedIn) && payload.Type != PayloadType.Auth)
	        {
	            ConsoleWrite("ERROR: AUTH not competed. Closing connection");
	            return false;
	        }
	        _lastActivity = DateTime.Now;

	        switch (payload.Type)
	        {
	            case PayloadType.Move:
	                if (payload.Params.Count() == 3)
	                {
	                    string who = payload.Params[0]; // should be "self". TODO: fix security
	                    int col = int.Parse(payload.Params[1]);
	                    int row = int.Parse(payload.Params[2]);
	                    LoggedInUser.MoveTo(new Position2D(col, row));
	                }
	                else
	                {
	                    ConsoleWrite("Error on command mv. Unknown parameter count: " + payload.Params.Count());
	                }
	                break;
	            case PayloadType.Message:
	                // Message
	                _lastActivity = DateTime.Now;
	                if (payload.Params.Any())
	                {
	                    if (payload.Params[0].Trim().Length > 0)
	                    {
	                        FadingWorldsServer.Instance.TCPPool.SendPayloadToAll(new NetworkPayload
	                        {
	                            Type = PayloadType.Message,
	                            Params = {LoggedInUser.ToString(), payload.Params[0]}
	                        });
	                        //FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ms|" + LoggedInUser + "|" + payload.Params[0].Trim());
	                    }
	                    else
	                    {
	                        // Empty message
	                    }
	                }
	                else
	                {
	                    // no parameters??
	                }

	                break;
	            case PayloadType.Auth:
	                if (!payload.Params.Any())
	                {
	                    ConsoleWrite("Missing parametres to auth command");
	                    return false;
	                }
                   
	                switch (payload.Command)
	                {
	                    case  PayloadCommand.Helo:
                            if (payload.Params.Count == 1)
	                        {
                                string version = payload.Params[0];
	                            _clientVersion = version;

	                            //check for outdated version
	                            //if (version == "1.0.0.0")
	                            //{
	                            //    SendCommand("au|oldversion");
	                            //    return false;
	                            //}
                                ConsoleWrite("Client connecting with version " + _clientVersion);
	                            SendPayload(new NetworkPayload()
	                            {
	                                Type = PayloadType.Auth,
	                                Command = PayloadCommand.AuthPlease,
                                    Params = { _server.Version }
	                            });
	                           // SendCommand("au|authplease");
	                            return true;
	                        }
	                        else
	                        {
	                            ConsoleWrite("Missing version to auth helo command");
	                            return false;
	                        }

	                    case PayloadCommand.Login:
                            if (payload.Params.Count == 2)
	                        {
                                string username = payload.Params[0];
                                string password = payload.Params[1];
	                            //string ip = _client.Client.RemoteEndPoint.ToString().Split(':')[0];

	                            // Check if user exists
	                            var users =
	                                FadingWorldsServer.Instance.UserDB.Users.Where(
	                                    e => e.Username.ToLower().Equals(username.ToLower()));
	                            if (!users.Any())
	                            {
	                                ConsoleWrite("Unknown user: " + username);
                                    SendPayload(new NetworkPayload()
                                    {
                                        Type = PayloadType.Auth,
                                        Command = PayloadCommand.Fail
                                    });
	                                return false;
	                            }

	                            var login = users.Single();

	                            // Check if password is correct
	                            if (login.Password.Equals(password))
	                            {
	                                // Log out if logged in already
	                                if (FadingWorldsServer.Instance.TCPPool.IsUserLoggedIn(username))
	                                {

                                        FadingWorldsServer.Instance.TCPPool.SendPayloadToUser(username, new NetworkPayload()
                                        {
                                            Type = PayloadType.System,
                                            Command = PayloadCommand.LoggedInDifferentLocation
                                        });

                                        //FadingWorldsServer.Instance.TCPPool.SendMessageToUser(username,
                                        //    "sy|logged-in-on-another-location");
	                                    FadingWorldsServer.Instance.TCPPool.DisconnectUser(username);
	                                }

	                                if (login.Health <= 0)
	                                {
	                                    login.Reset();
	                                }

	                                LoggedInUser = login;
	                                _state = ConnectionState.LoggedIn;
	                                ConsoleWrite("Username: " + LoggedInUser.Username);
                                    SendPayload(new NetworkPayload()
                                    {
                                        Type = PayloadType.Auth,
                                        Command = PayloadCommand.Success,
                                        Params = { ConnectionId }
                                    });
	                               // SendCommand("au|ok|" + ConnectionID);
	                                if (LoggedInUser.Position.IsInvalid)
	                                {
	                                    LoggedInUser.Position =
	                                        FadingWorldsServer.Instance.TheGrid.FindRandomEmptyGrassBlock();
	                                }


	                                return true;
	                            }
	                            else
	                            {
	                                ConsoleWrite("Invalid password!");
                                    SendPayload(new NetworkPayload()
                                    {
                                        Type = PayloadType.Auth,
                                        Command = PayloadCommand.Fail,
                                        
                                    });
	                                //SendCommand("au|failed");
	                                return false;
	                            }
	                        }
	                        else
	                        {
	                            ConsoleWrite("wrong number of args");
                                SendPayload(new NetworkPayload()
                                {
                                    Type = PayloadType.Auth,
                                    Command = PayloadCommand.Fail,

                                });
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
                            SendPayload(new NetworkPayload()
                            {
                                Type = PayloadType.Auth,
                                Command = PayloadCommand.Fail,

                            });
	                        return false;
	                }
	            case PayloadType.Data:

	                switch (payload.Command)
	                {
	                    case PayloadCommand.GetMap:
	                        SendPayload(new NetworkPayload()
	                        {
	                            Type = PayloadType.Data,
	                            Command = PayloadCommand.Map,
	                            Params =
	                            {
	                                FadingWorldsServer.Instance.TheGrid.Width+"",
	                                FadingWorldsServer.Instance.TheGrid.Height+"",
	                                FadingWorldsServer.Instance.TheGrid.DumpMapBlocks()
	                            }

	                        });
	                        break;
	                    case PayloadCommand.MapLoaded:
                            SendPayload(new NetworkPayload()
                            {
                                Type = PayloadType.Data,
                                Command = PayloadCommand.PleaseLoadGame,
                             

                            });
	                       // SendCommand("da|pleaseloadgame");
	                        break;
	                    case PayloadCommand.GameLoaded:
                            //FadingWorldsServer.Instance.TCPPool.SendMessageToAllButUser(LoggedInUser.Username,
                            //    "us|login|" + LoggedInUser.MakeDump());
                            FadingWorldsServer.Instance.TCPPool.SendPayloadToAllButUser(LoggedInUser.Username, new NetworkPayload()
                            {
                                Type = PayloadType.User,
                                Command = PayloadCommand.Login,
                                Params = { LoggedInUser.MakeDump() }
                            });
	                       // SendCommand("da|initplayer|" + LoggedInUser.MakeDump());
                            SendPayload(new NetworkPayload()
                            {
                                Type = PayloadType.Data,
                                Command = PayloadCommand.InitPlayer,
                                Params = { LoggedInUser.MakeDump() }


                            });
	                        foreach (Entity entity in FadingWorldsServer.Instance.GameObjects)
	                        {
                                SendPayload(new NetworkPayload()
                                {
                                    Type = PayloadType.Data,
                                    Command = PayloadCommand.InitEntity,
                                    Params = { entity.MakeDump()}


                                });
	                           // SendCommand("da|initentity|" + entity.MakeDump());
	                        }
	                        SendUserList();
	                        FadingWorldsServer.Instance.TheGrid.GetBlockAt(LoggedInUser.Position)
	                            .Entities.Add(LoggedInUser);
	                        break;
	                }


	                break;
	            case PayloadType.Quit: // Pure quit message
	                ConsoleWrite("Got quit command, so returning false on request.");
	                return false;
	            case PayloadType.System:
	                if (payload.Command == PayloadCommand.Pong)
	                {
	                    ConsoleWrite("Replied to ping");
	                }
	                break;
	            case PayloadType.Attack:
	                if (payload.Params.Count == 3)
	                {
	                    string mobId = payload.Params[0];
	                    int mobX = int.Parse(payload.Params[1]);
	                    int mobY = int.Parse(payload.Params[2]);
	                    ConsoleWrite("Player " + LoggedInUser.Id + " doing an attack move on " + mobId);
	                    LoggedInUser.TryAttack(mobId);
	                    //MoveResult x = LoggedInUser.MoveTo(new Position2D(mobX, mobY));

	                    //if (subCode == "po")
	                    //{
	                    //  ConsoleWrite("Replied to ping");
	                    //}
	                }
	                break;
	            case PayloadType.PrivateMessage:
	                if (payload.Params.Count() == 2)
	                {
	                    string target = payload.Params[0];
	                    string message = payload.Params[1];
                        SendPayload(new NetworkPayload()
                        {
                            Type = PayloadType.Message,
                            Params = { "system", "<<pm to " + LoggedInUser + ":" + message }
                        });
	                   // SendCommand("ms|system|<<pm to " + LoggedInUser + ":" + message);
                        FadingWorldsServer.Instance.TCPPool.SendPayloadToUser(target, new NetworkPayload()
                        {
                            Type = PayloadType.Message,
                            Params = { "system", ">>pm from " + LoggedInUser + ":" + message }
                        });
                        //FadingWorldsServer.Instance.TCPPool.SendMessageToUser(target,
                        //    "system|>>pm from " + LoggedInUser + ":" + message);
	                }
	                else
	                {
                        SendPayload(new NetworkPayload()
                        {
                            Type = PayloadType.Message,
                            Params = { "system", "i'm afraid you need more than that" }
                        });
	                    //SendCommand("ms|system|i'm afraid you need more than that");
	                }
	                break;

	            default:
	                ConsoleWrite("ParseGuiCommand():  Sent unknown opCode: " + payload.Type);
                    SendPayload(new NetworkPayload()
                    {
                        Type = PayloadType.Message,
                       
                        Params = { "system", "Unknown Command" }


                    });
	                //SendCommand("ms|system|unknown-cmd");
	                break;
	        }
	        // Default to success
	        return true;
	    }




	    public void SendUserList() {
            FadingWorldsServer.Instance.TCPPool.SendPayloadToAll(new NetworkPayload
            {
                Type = PayloadType.UserList,
                Params = { FadingWorldsServer.Instance.TCPPool.GenerateUserList() }
            });
			//FadingWorldsServer.Instance.TCPPool.SendMessageToAll("ul|" + FadingWorldsServer.Instance.TCPPool.GenerateUserList());
		}

	    public void SendPayload(NetworkPayload payload)
	    {
            try
            {
                if (!_lineOccupied && !_isClosed && _client.Connected)
                {
                    while (_lineOccupied)
                    {
                        Thread.Sleep(50);
                    }
                    _lineOccupied = true;
                    Serializer.SerializeWithLengthPrefix(_client.GetStream(), payload, PrefixStyle.Base128);
                    _client.GetStream().Flush();
                    ConsoleWrite(" [out] -> [" + payload + "]");
                    _lineOccupied = false;
                }
            }
            catch (Exception ex)
            {
                ConsoleWrite("[-]: SendPayload() exception: " + ex);
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
            //try {
            //    SendCommand("ms|system|DISPOSING CONNECTION!");
            //    if (_clientWriter != null) {
            //        _clientWriter.Close();
            //        _clientWriter.Dispose();
            //    }
            //    if (_clientReader != null) {
            //        _clientReader.Close();
            //        _clientReader.Dispose();
            //    }
            //    _clientWriter = null;
            //    _clientReader = null;

            //    if (_client != null) {
            //        if (_client.Client != null) {
            //            _client.Client.Close();
            //            _client.Client = null;
            //        }
            //        _client.Close();
            //    }

            //    _client = null;
            //}
            //catch (Exception ex) {
            //    Console.WriteLine(ex.ToString());
            //}
		}

		#endregion
	}
}