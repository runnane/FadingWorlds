using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FadingWorldsClient.GameObjects;
using FadingWorldsClient.GameObjects.Blocks;
using FadingWorldsClient.GameObjects.Living;
using fwlib;
using ProtoBuf;

namespace FadingWorldsClient
{
	public class ConnectionLoop : IDisposable {
		private TcpClient _client;
        private string _loggedInUser;

		public Loader TheLoader;

		public bool IsConnected;
		public bool IsShuttingDown;
		public bool IsDisconnecting;
		public bool IsSecure;

		private string _username;
		private string _password;
	
		public ConnectionLoop(Loader loader) {
			TheLoader = loader;
			IsConnected = false;
		}

		public string GetLoggedInUser() {
			return _loggedInUser;
		}

		public void Disconnect() {
			try {
				if (_client != null) {
					_client.Client.Disconnect(true);
					if (_client.Client != null)
						_client.Client.Close();
					_client.Close();
				}
			}
			catch (ThreadAbortException) {
				throw;
			}
			catch (Exception) {
				//Log(ex.ToString());
			}
		}

	    public void Log(string s)
	    {
	        TheLoader.Message(s);
	    }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szRemoteHostName"></param>
		/// <param name="iRemotePort"></param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		public void StartConnect(string szRemoteHostName, int iRemotePort, string username, string password) {
			try {
				_username = username;
				_password = password;

				Log(DateTime.Now.ToLongTimeString() + ": Trying to connect");
				try {
					_client = new TcpClient(szRemoteHostName, iRemotePort);
				}
				catch (Exception ex) {
					Log(DateTime.Now.ToLongTimeString() + ": Connection failed, server not responding (" + ex.Message +
					                  ")");
					return;
				}
				TheLoader.StopReconnectSequence();
			    SendPayload(new NetworkPayload
			    {
			        Type = PayloadType.Auth,
			        Command = PayloadCommand.Helo,
			        Params = {TheLoader.Version}
			    });
               
				IsSecure = false;

				Log(DateTime.Now.ToLongTimeString() + ": Connected!");
				//string input;

				Log("Connected to Fading Worlds Server");
				IsConnected = true;
			    while (!IsShuttingDown && !IsDisconnecting && _client.Connected)
			    {
			        // Wait for command
			        NetworkPayload payload;
			        try
			        {
			            //input = _clientReader.ReadLine();
			            payload = Serializer.DeserializeWithLengthPrefix<NetworkPayload>(_client.GetStream(),
			                PrefixStyle.Base128);
                        //payload.Complete = true;
                        Log("input< " + payload);
			            //Log(payload.ToString());
			        }
			        catch (IOException)
			        {
			            if (!IsShuttingDown)
			                Log(DateTime.Now.ToLongTimeString() + ": Connection violently closed at remote end");
			            break;
			        }
                    //if (payload == null)
                    //{
                    //    if (!IsShuttingDown)
                    //        Log(DateTime.Now.ToLongTimeString() + ": Remote end closed connection");
                    //    break;
                    //}
                    if (payload.Type != PayloadType.Unset)
                    {
                        if (!HandlePayload(payload))
                        {
                            break;
                        }
                    }
			    }
			    if (!IsShuttingDown) {
					Log(DateTime.Now.ToLongTimeString() + ": Disconnected");
					Log("-- Disconnected --");
					TheLoader.SetLoggedIn(false);
					TheLoader.TheGame.Exit();
				}
			}
			catch (ThreadAbortException) {
				throw;
			}
			catch (Exception ex) {
				Log(ex.ToString());
			}
		}

	    
		
		public void SendPayload(NetworkPayload msg)
		{
		    Serializer.SerializeWithLengthPrefix(_client.GetStream(), msg, PrefixStyle.Base128);
            _client.GetStream().Flush();
            Log("Sending: " + msg);
		}

        //public void SendPayload(PayloadType pt, PayloadCommand pc, List<string> parms)
        //{
        //    Serializer.SerializeWithLengthPrefix(_client.GetStream(), new NetworkPayload {Type = pt, Command = pc, Params = parms}, PrefixStyle.Base128);
        //}

		
		private bool HandlePayload(NetworkPayload payload) {
            switch (payload.Type)
            {
				case PayloadType.Auth:
						switch (payload.Command) {
							case PayloadCommand.AuthPlease:
								if (_loggedInUser == null) {
                                    SendPayload(new NetworkPayload
                                    {
                                        Type = PayloadType.Auth,
                                        Command = PayloadCommand.Login,
                                        Params = { _username, _password }
                                    });
									//SendCommand("au|login|" + _username + "|" + _password);
								}
								else {
									Log("OOOPS - server wants us to authenticate, but we already are??? :(");
								}
								break;
							case PayloadCommand.OldVersion:
								Log("OOOPS - server says we are outdated");
								return false;

                            case PayloadCommand.Fail:
								TheLoader.Message("Login failed!");
								return false;

							case PayloadCommand.Success:
								if (payload.Params.Count == 1) {
									string connectionId = payload.Params[0];
									Log("Our connectionid is " + connectionId);
									_loggedInUser = _username;

									TheLoader.State = GameState.WaitingForMap;

									TheLoader.SetLoggedIn(true);
									TheLoader.SpawnGame(0, 0, "");
									//TheLoader.SetVisible(false);

									while (TheLoader.TheGame == null || !TheLoader.TheGame.IsLoaded) {
										Thread.Sleep(100);
									}
                                    SendPayload(new NetworkPayload
                                    {
                                        Type = PayloadType.Data,
                                        Command = PayloadCommand.GetMap
                                    });
									//SendCommand("da|getmap");

									return true;
								}
								else {
									Log("OOOPS - server did not send connectionid");
									return false;
								}


							default:
								throw new Exception("Unknown auth (subCode): '" + payload.Command + "'");
						}
					
					
					break;
				case PayloadType.Data:
                   switch (payload.Command) {
							case PayloadCommand.Map:

								int blockWidth = int.Parse(payload.Params[0]);
                                int blockHeight = int.Parse(payload.Params[1]);
                                string mapData = payload.Params[2];
								//TheLoader.SetLoggedIn(true);
								//TheLoader.SetVisible(false);


								//while (TheLoader.TheGame == null || !TheLoader.TheGame.IsLoaded) {
								//  Thread.Sleep(100);
								//}
								TheLoader.SetMap(blockWidth, blockHeight, mapData);

                                SendPayload(new NetworkPayload
                                {
                                    Type = PayloadType.Data,
                                    Command = PayloadCommand.MapLoaded
                                });

								//SendCommand("da|maploaded");

								return true;


							case PayloadCommand.PleaseLoadGame:
								while (TheLoader.TheGame == null || !TheLoader.TheGame.IsLoaded) {
									Thread.Sleep(100);
								}
                                SendPayload(new NetworkPayload
                                {
                                    Type = PayloadType.Data,
                                    Command = PayloadCommand.GameLoaded
                                });
								//SendCommand("da|gameloaded");

								return true;
							case PayloadCommand.InitPlayer:
                                Dictionary<string, string> confSet = Helper.ConvertDataStringToDictionary(payload.Params[0]);
								foreach (KeyValuePair<string, string> keyValuePair in confSet) {
									string attr = keyValuePair.Key;
									string val = keyValuePair.Value;

									switch (attr) {
										case "type":
											//TheLoader.TheGame.ThePlayer.Health = int.Parse(val);
											break;
										case "id":
											//TheLoader.TheGame.ThePlayer.Health = int.Parse(val);
											break;
										case "dmg":
											TheLoader.TheGame.ThePlayer.Weapon = val;
											break;
										case "hp":
											TheLoader.TheGame.ThePlayer.Health = int.Parse(val);
											break;
										case "maxhp":
											TheLoader.TheGame.ThePlayer.MaxHealth = int.Parse(val);
											break;
										case "ap":
											TheLoader.TheGame.ThePlayer.AttackPower = int.Parse(val);
											break;
										case "ac":
											TheLoader.TheGame.ThePlayer.ArmorClass = int.Parse(val);
											break;
										case "maxmana":
											TheLoader.TheGame.ThePlayer.MaxMana = int.Parse(val);
											break;
										case "mana":
											TheLoader.TheGame.ThePlayer.Mana = int.Parse(val);
											break;
										case "xp":
											TheLoader.TheGame.ThePlayer.ExperiencePoints = int.Parse(val);
											break;
										case "level":
											TheLoader.TheGame.ThePlayer.Level = int.Parse(val);
											break;
										case "nextlevel":
											TheLoader.TheGame.ThePlayer.NextLevelAt = int.Parse(val);
											break;

										default:
											Log("Unknown attr for player: " + attr + " -> " + val + "");
											break;
									}
								}
								if (confSet["x"] != null) {
									var pos = new Position2D(int.Parse(confSet["x"]), int.Parse(confSet["y"]));
									TheLoader.TheGame.ThePlayer.Position = pos;
									TheLoader.TheGame.TheGrid.GetBlockAt(pos).Entities.Add(TheLoader.TheGame.ThePlayer);
									lock(TheLoader.TheGame.GameObjects) {
										TheLoader.TheGame.GameObjects.Add(TheLoader.TheGame.ThePlayer);
									}
								
								}
								else {
									Log("Did not get user coords for player!!");
								}
								return true;
							case PayloadCommand.InitEntity:
                                Dictionary<string, string> confSet2 = Helper.ConvertDataStringToDictionary(payload.Params[0]);
								LivingEntity mob2;
								if (confSet2["type"].Contains("Skeleton"))
								{
									mob2 = new Skeleton(confSet2["id"]);
								}
								else if (confSet2["type"].Contains("Ghost")) {
									mob2 = new Ghost(confSet2["id"]);
								}else {
									return true;
								}
								mob2.Position = new Position2D(int.Parse(confSet2["x"]), int.Parse(confSet2["y"]));
								mob2.Health = int.Parse(confSet2["hp"]);

								TheLoader.TheGame.TheGrid.GetBlockAt(new Position2D(int.Parse(confSet2["x"]), int.Parse(confSet2["y"]))).
									Entities.Add(mob2);
								lock (TheLoader.TheGame.GameObjects) {
									TheLoader.TheGame.GameObjects.Add(mob2);
								}
								return true;


							default:
								throw new Exception("Unknown auth (subCode): '" + payload.Command + "'");
						}
                    //}
					break;
				case PayloadType.Message:
                    if (payload.Params.Count > 1)
                    {
                        string from = payload.Params[0];
                        string message = payload.Params[1];
						if (from.ToUpper().Equals("SYSTEM"))
							Log("" + from + " " + message + "");
						if (from.Equals(_loggedInUser))
							Log("" + from + " " + message + "");
						else
							Log("" + from + " " + message + "");
					}
					else {
						// error from server, to few command
						throw new Exception("unknown ms syntax ");
					}
					break;

				case PayloadType.User:
                    //if (parm.Length > 1) {
                    //    string subCode = parm[0];
                        switch (payload.Command)
                        {
							case PayloadCommand.Login:
                                var confSet = Helper.ConvertDataStringToDictionary(payload.Params[0]);

								if (confSet["id"] == _username) {
									return true;
								}
								var op = TheLoader.TheGame.GameObjects.GetById(confSet["id"]) as OtherPlayer;
								if (op == null) {
									var otherplayer = new OtherPlayer(Textures.Hero, confSet["id"]);
									var playerPos = new Position2D(int.Parse(confSet["x"]), int.Parse(confSet["y"]));
									otherplayer.Desc = confSet["id"];
									otherplayer.Position = playerPos;
									//	otherplayer.Disconnected = true;
									otherplayer.Health = int.Parse(confSet["hp"]);
									otherplayer.MaxHealth = int.Parse(confSet["maxhp"]);
									otherplayer.Level = int.Parse(confSet["level"]);
									otherplayer.ExperiencePoints = int.Parse(confSet["xp"]);
									otherplayer.Mana = int.Parse(confSet["mana"]);


									TheLoader.TheGame.TheGrid.GetBlockAt(playerPos).Entities.Add(otherplayer);
									lock (TheLoader.TheGame.GameObjects) {
										TheLoader.TheGame.GameObjects.Add(otherplayer);
									}
								}
								break;
							case PayloadCommand.Logout:
                                string username = payload.Params[0];
								var otherplayer2 = TheLoader.TheGame.GameObjects.GetById(username) as OtherPlayer;
								if (otherplayer2 != null) {
									otherplayer2.OnDeath();
									TheLoader.TheGame.TheGrid.GetBlockAt(otherplayer2.Position).Entities.Remove(otherplayer2);
									lock (TheLoader.TheGame.GameObjects) {
										TheLoader.TheGame.GameObjects.Remove(username);
									}
								}
								break;
							default:
								throw new Exception("unknown us subCode " + payload.Command);
						}
					//}
                    //else {
                    //    // error from server, to few command
                    //    throw new Exception("unknown us syntax ");
                    //}
					break;

                case PayloadType.UserList:
					while (TheLoader.TheGame == null || TheLoader.TheGame.GameObjects == null) {
						Thread.Sleep(100);
					}

                    foreach (string t in payload.Params)
                    {
						string[] configBlocks = t.Split('#');
						foreach (string configBlock in configBlocks) {
							if (configBlock.Trim().Length > 0) {
								Dictionary<string, string> confSet = Helper.ConvertDataStringToDictionary(configBlock);
								if (confSet["id"] == _username) {
									continue;
								}

								var op = TheLoader.TheGame.GameObjects.GetById(confSet["id"]) as OtherPlayer;
								if (op == null) {
									op = new OtherPlayer(Textures.Hero, confSet["id"]);
									op.Desc = confSet["id"];
									var playerPos = new Position2D(int.Parse(confSet["x"]), int.Parse(confSet["y"]));
									op.Position = playerPos;
									TheLoader.TheGame.TheGrid.GetBlockAt(playerPos).Entities.Add(op);
									op.Health = int.Parse(confSet["hp"]);
									op.MaxHealth = int.Parse(confSet["maxhp"]);
									op.Level = int.Parse(confSet["level"]);
									op.ExperiencePoints = int.Parse(confSet["xp"]);
									op.Mana = int.Parse(confSet["mana"]);

									lock (TheLoader.TheGame.GameObjects) {
										TheLoader.TheGame.GameObjects.Add(op);
									}
								}
								else {
									op.Health = int.Parse(confSet["hp"]);
									op.MaxHealth = int.Parse(confSet["maxhp"]);
									op.Level = int.Parse(confSet["level"]);
									op.ExperiencePoints = int.Parse(confSet["xp"]);
									op.Mana = int.Parse(confSet["mana"]);
									op.MoveTo(new Position2D(int.Parse(confSet["x"]), int.Parse(confSet["y"])));
								}
							}
						}
						//string username = userdata[0];
						//int column = int.Parse(userdata[1]);
						//int row = int.Parse(userdata[2]);


						//OtherPlayer op = TheLoader.TheGame.GameObjects.GetById(username) as OtherPlayer;
						//if (op == null) {
						//  OtherPlayer o = new OtherPlayer(Textures.Hero, username);
						//  o.Desc = username;
						//  Position2D playerPos = new Position2D(column, row);
						//  o.Position = playerPos;
						//  TheLoader.TheGame.TheGrid.GetBlockAt(playerPos).Entities.Add(o);
						//  lock (TheLoader.TheGame.GameObjects) {
						//    TheLoader.TheGame.GameObjects.Add(o);
						//  }
						//}
						//else {
						//  op.MoveTo(new Position2D(column, row));
						//}
					}
					break;
                case PayloadType.System:
                    try
                    {
                        //if (!parm.Any())
                        //{
                        //    // Unknown funny command
                        //}
                        //else
                        //{
                            switch (payload.Command)
                            {
                                //case "start-reconnect":
                                //    Log(DateTime.Now.ToLongTimeString() + ": Starting reconnect sequence");
                                //    TheLoader.StartReconnectSequence();
                                //    return true;
                                //case "whois":
                                //    if (parm.Length >= 3)
                                //        Log("(whois " + parm[1] + ")? " + parm[2]);
                                //    return true;
                                case PayloadCommand.LoggedInDifferentLocation:
                                    TheLoader.TheGame.Exit();
                                    return true;
                                case PayloadCommand.Ping:
                                    SendPayload(new NetworkPayload
                                    {
                                        Type = PayloadType.System,
                                        Command = PayloadCommand.Pong
                                    });
                                    //SendCommand("sy|po");
                                    return true;
                                default:
                                    if (payload.Params.Count > 0)
                                    {
                                        Log("SYSTEM: " + payload.Params[0]);
                                        //Log(DateTime.Now.ToLongTimeString() + ": " + s[1]);
                                    }
                                    return true;
                            }
                        //}
                        return true;
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Log(ex.ToString());
                        return true;
                    }
                case PayloadType.Move:
                    string mobId = payload.Params[0];
                    int mobCol = int.Parse(payload.Params[1]);
                    int mobRow = int.Parse(payload.Params[2]);
					LivingEntity mob = TheLoader.TheGame.GameObjects.GetById(mobId) as LivingEntity;
					if (mob != null) {
						mob.MoveTo(new Position2D(mobCol, mobRow));
						Log(DateTime.Now.ToLongTimeString() + "MOVEMENT: " + mobId + " moved to " + mobCol + "x" + mobRow);
					}
					else {
						Log(DateTime.Now.ToLongTimeString() + "MOVEMENT: " + mobId + " NOT FOUND!!");
					}


					return true;
                case PayloadType.EntityChange:
                    //string[] entitychangelines = payload.Params[0].Split('#');
                    //foreach (string entitychangeline in entitychangelines) {
                    //    string[] entityChange = entitychangeline.Split('/');

                        string entityId = payload.Params[0];
                        string attribute = payload.Params[1];
                        string deltaValue = payload.Params[2];
                        string newValue = payload.Params[3];

						var entity = TheLoader.TheGame.GameObjects.GetById(entityId) as LivingEntity;
						if (entity != null) {
							switch (attribute) {
								case "die":
									Log(DateTime.Now.ToLongTimeString() + " DIED CHANGE");
									lock (TheLoader.TheGame.GameObjects) {
										TheLoader.TheGame.GameObjects.RemoveById(entity.Id);
									}
									TheLoader.TheGame.TheGrid.GetBlockAt(entity.Position).Entities.RemoveById(entity.Id);
									return true;
								case "hp":
									Log(DateTime.Now.ToLongTimeString() + " HP CHANGE: " + entityId + " changed to " + newValue +
									                  " -- delta " + deltaValue);
									entity.Health += int.Parse(deltaValue);
									if (entity.Health != int.Parse(newValue)) {
										//throw new Exception("server and client hp out of sync: server=" + newValue + "/client=" + entity.Health);
										Log(DateTime.Now.ToLongTimeString() + "Health server and client hp out of sync: server=" +
										                  newValue +
										                  "/client=" + entity.Health);
									}
									if (int.Parse(deltaValue) < 0) {
										FadingWorldsApp.Instance.TemporaryObjects.Add(new LimitedEntity(Textures.Explosion, entity.Position, 0.12f));
									}

									break;

								case "maxhp":
									Log(DateTime.Now.ToLongTimeString() + " MaxHealth CHANGE: " + entityId + " changed to " +
									                  newValue +
									                  " -- delta " + deltaValue);
									entity.MaxHealth += int.Parse(deltaValue);
									if (entity.MaxHealth != int.Parse(newValue)) {
										Log(DateTime.Now.ToLongTimeString() + " MaxHealth server and client hp out of sync: server=" +
										                  newValue +
										                  "/client=" + entity.MaxHealth);
									}
									break;
								case "mana":
									Log(DateTime.Now.ToLongTimeString() + " Mana CHANGE: " + entityId + " changed to " + newValue +
									                  " -- delta " + deltaValue);
									entity.Mana += int.Parse(deltaValue);
									if (entity.Mana != int.Parse(newValue)) {
										Log(DateTime.Now.ToLongTimeString() + " Mana server and client hp out of sync: server=" +
										                  newValue +
										                  "/client=" + entity.Mana);
									}
									break;
								case "maxmana":
									Log(DateTime.Now.ToLongTimeString() + " MaxMana CHANGE: " + entityId + " changed to " + newValue +
									                  " -- delta " + deltaValue);
									entity.MaxMana += int.Parse(deltaValue);
									if (entity.MaxMana != int.Parse(newValue)) {
										Log(DateTime.Now.ToLongTimeString() + " MaxMana server and client hp out of sync: server=" +
										                  newValue +
										                  "/client=" + entity.MaxMana);
									}
									break;
								case "xp":
									Log(DateTime.Now.ToLongTimeString() + " ExperiencePoints CHANGE: " + entityId + " changed to " +
									                  newValue +
									                  " -- delta " + deltaValue);
									entity.ExperiencePoints += int.Parse(deltaValue);
									if (entity.ExperiencePoints != int.Parse(newValue)) {
										Log(DateTime.Now.ToLongTimeString() +
										                  " ExperiencePoints server and client hp out of sync: server=" +
										                  newValue +
										                  "/client=" + entity.ExperiencePoints);
									}
									break;
								case "ac":
									Log(DateTime.Now.ToLongTimeString() + " ArmorClass CHANGE: " + entityId + " changed to " +
									                  newValue +
									                  " -- delta " + deltaValue);
									entity.ArmorClass += int.Parse(deltaValue);
									if (entity.ArmorClass != int.Parse(newValue)) {
										Log(DateTime.Now.ToLongTimeString() + " ArmorClass server and client hp out of sync: server=" +
										                  newValue +
										                  "/client=" + entity.ArmorClass);
									}
									break;
								case "ap":
									Log(DateTime.Now.ToLongTimeString() + " AttackPower CHANGE: " + entityId + " changed to " +
									                  newValue +
									                  " -- delta " + deltaValue);
									entity.AttackPower += int.Parse(deltaValue);
									if (entity.AttackPower != int.Parse(newValue)) {
										Log(DateTime.Now.ToLongTimeString() + " AttackPower server and client hp out of sync: server=" +
										                  newValue +
										                  "/client=" + entity.AttackPower);
									}
									break;
								case "level":
									Log(DateTime.Now.ToLongTimeString() + " Level CHANGE: " + entityId + " changed to " + newValue +
									                  " -- delta " + deltaValue);
									entity.Level += int.Parse(deltaValue);
									if (entity.Level != int.Parse(newValue)) {
										Log(DateTime.Now.ToLongTimeString() + " Level server and client hp out of sync: server=" +
										                  newValue +
										                  "/client=" + entity.Level);
									}
									break;
								case "nextlevel":
									Log(DateTime.Now.ToLongTimeString() + "NextLevelAt CHANGE: " + entityId + " changed to " +
									                  newValue +
									                  " -- delta " + deltaValue);
									entity.NextLevelAt += int.Parse(deltaValue);
									if (entity.NextLevelAt != int.Parse(newValue)) {
										Log(DateTime.Now.ToLongTimeString() + " NextLevelAt server and client hp out of sync: server=" +
										                  newValue +
										                  "/client=" + entity.NextLevelAt);
									}
									break;


								default:
									Log(DateTime.Now.ToLongTimeString() + "UNKNOWN CHANGE: " + entityId + " changed " + attribute +
									                  " to " + newValue + " -- delta " + deltaValue);

									break;
							}
						}
						else {
							Log(DateTime.Now.ToLongTimeString() + "ENTITYCHANGE: " + entityId + " NOT FOUND!!");
						}
                    //}


					return true;
				default:
					throw new Exception("Unknown (opCode): '" + payload.Type + "'");
			}

			// Default to true
			return true;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
        //private bool ParseInput(string input) {
        //    string[] parameters = {};
        //    string[] primaryParts = input.Split(_delimPipe, 2);
        //    if (primaryParts.Length > 1) {
        //        parameters = primaryParts[1].Split(_delimPipe);
        //    }
        //    return HandleCode(primaryParts[0].Trim().ToLower(), parameters);

        //    //try {

        //    //}
        //    //catch
        //    //  (ThreadAbortException) {
        //    //  throw;
        //    //}
        //    //catch
        //    //  (Exception
        //    //    ex) {
        //    //  Log(ex.ToString());
        //    //  return true;
        //    //}
        //}


		#region IDisposable Members

		public void Dispose() {
            //if (_sslSecureStream != null) {}
            //_sslSecureStream = null;

            //if (_clientWriter != null) {
            //    _clientWriter.Close();
            //    _clientWriter.Dispose();
            //}
            //if (_clientReader != null) {
            //    _clientReader.Close();
            //    _clientReader.Dispose();
            //}
            //_clientWriter = null;
            //_clientReader = null;

            //if (_client != null) {
            //    //client.
            //    if (_client.Client != null) {
            //        _client.Client.Close();
            //        _client.Client = null;
            //    }
            //    _client.Close();
            //}

            //_client = null;
		}

		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="inputString"></param>
		/// <param name="listOfKeyWords"></param>
		/// <returns></returns>
		public static bool IsStringInList(string inputString, string listOfKeyWords) {
			try {
				if (listOfKeyWords.Length == 0) return false;
				string[] listOfMatchWords = listOfKeyWords.Split(",".ToCharArray());

				foreach (string matchWord in listOfMatchWords) {
					if (matchWord.Length == 0) continue;
					if (matchWord.Length > 1 && matchWord.Substring(0, 1).Equals("*") &&
					    matchWord.Substring(matchWord.Length - 1, 1).Equals("*")) {
						if (inputString.ToUpper().Contains(matchWord.Substring(1, matchWord.Length - 2).ToUpper())) {
							return true;
						}
					}
					else if (matchWord.Substring(0, 1).Equals("*")) {
						if (inputString.ToUpper().EndsWith(matchWord.Substring(1, matchWord.Length - 1).ToUpper())) {
							return true;
						}
					}
					else if (matchWord.Substring(matchWord.Length - 1, 1).Equals("*")) {
						if (inputString.ToUpper().StartsWith(matchWord.Substring(0, matchWord.Length - 2).ToUpper())) {
							return true;
						}
					}
					else {
						if (inputString.ToUpper().Equals(matchWord.ToUpper())) {
							return true;
						}
					}
				}
				return false;
			}
			catch (Exception) {
				return false;
			}
		}
	}
}