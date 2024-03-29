﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using fwlib;

namespace FadingWorldsServer{
	public class TcpConnectionPool {
		public List<ConnectionThread> Connections { get; set; }

		private DateTime _started = DateTime.Now;

		public TcpConnectionPool() {
			Connections = new List<ConnectionThread>();
		}

		public string GetTimeStarted() {
			return _started.ToString();
		}

		public string GetUptime() {
			TimeSpan t = (DateTime.Now - _started);
			return t.Days + "d " + t.Hours + "h " + t.Minutes + "m " + t.Seconds + "s";
		}

		public void Add(ref ConnectionThread c) {
			Connections.Add(c);
			Console.WriteLine("Added a thread to the pool - now count: " + Connections.Count);
		}

		public void Remove(ref ConnectionThread c) {
			Connections.Remove(c);
			Console.WriteLine("Removed a thread from the pool - now count: " + Connections.Count);
		}

		public void CheckPool(object source, ElapsedEventArgs e) {
			//Console.WriteLine("Connections open: " + tcpPool.Count);
			foreach (ConnectionThread c in Connections.ToList()) {
				if (c.SecondsSinceLastPacket > 80) {
					c.ConsoleWrite("Closing connection, over 80 sec since last packet");
					c.Close();
				}
				else if (c.SecondsSinceLastPacket > 60) {
					c.ConsoleWrite("Sending ping, seconds since packet: " + c.SecondsSinceLastPacket);
				    c.SendPayload(new NetworkPayload {Type = PayloadType.System, Command = PayloadCommand.Ping});
				}
			}
		}

		public void SendPayloadToAll(NetworkPayload payload) {
			foreach (ConnectionThread c in Connections) {
				if (c.IsLoggedIn)
                    c.SendPayload(payload);
			}
		}


		public void DisconnectUser(string str) {
			Console.WriteLine("POOL: disconnecting [" + str + "]");
			foreach (ConnectionThread c in Connections) {
				if (c.LoggedInUser.Username.Equals(str)) {
					c.Close();
					return;
				}
			}
		}

		public string GenerateUserList() {
			string str = "";
			foreach (ConnectionThread c in Connections) {
				if (c.LoggedInUser != null && c.LoggedInUser.Username.Length > 0) {
					str += "#" + c.LoggedInUser.MakeDump();
				}
			}
			return str;
		}


		public int GetOnlineUserCount() {
			return Connections.Count;
		}

		public bool IsUserLoggedIn(string username) {
			return Connections.Any(c => c.IsLoggedIn && c.LoggedInUser.Username.Equals(username));
		}

		public ConnectionThread GetConnectionFromUser(string username) {
			return Connections.FirstOrDefault(c => c.LoggedInUser.Username.Equals(username));
		}

		internal void SendPayloadToUser(string username, NetworkPayload p) {
			foreach (ConnectionThread c in Connections) {
				if (c.LoggedInUser.Username.Equals(username)) {
					c.SendPayload(p);
					return;
				}
			}
		}

        internal void SendPayloadToAllButUser(string username, NetworkPayload p)
        {
			foreach (ConnectionThread c in Connections) {
				if (c != null && c.LoggedInUser != null) {
					if (!c.LoggedInUser.Username.Equals(username)) {
                        c.SendPayload(p);
					}
				}
			}
		}
	}
}