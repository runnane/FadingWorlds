﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using FadingWorldsServer.GameObjects;
using FadingWorldsServer.GameObjects.Living;
using FadingWorldsServer.ServerObjects;
using fwlib;

namespace FadingWorldsServer {
	public class FadingWorldsServer {
		private const int TCPLocalPort = 4100;

		public static FadingWorldsServer Instance { get; set; }

		public string Version {
			get {
			    return "1.0.0.0-mono";
			}
		}

		public TcpConnectionPool TCPPool;
		private readonly Thread _tcpThread;
		public Grid TheGrid;
		public UserDB UserDB;

		public EntityCollection GameObjects;

		private GameTimeLight gt;

		private static void Main() {
			var x = new FadingWorldsServer();
		}

		public FadingWorldsServer() {
			Console.WriteLine("Fading Worlds - server v" + Version + " initializing");
            //try {
				Instance = this;

				// Create UserDB
				UserDB = new UserDB();
				Console.WriteLine("[+] UserDB initialized");

				// Create world
				TheGrid = new Grid(39, 20);
				TheGrid.GenerateWorld();
				Console.WriteLine("[+] World generated and initialized");


				// Add som random startmobs
				var initialCountOfMobs = 10;
				GameObjects = new EntityCollection();
				for (int i = 0; i < initialCountOfMobs; i++) {
					var monster1 = new Skeleton();
					Position2D randPos = TheGrid.FindRandomEmptyGrassBlock();
					monster1.Position = randPos;
					TheGrid.GetBlockAt(randPos).Entities.Add(monster1);
					lock (GameObjects) {
						GameObjects.Add(monster1);
					}
				}
				Console.WriteLine("[+] World entities created and initialized");


				// TCP Network connection
				TCPPool = new TcpConnectionPool();

				// Ping check timer
				var myTimer = new System.Timers.Timer();
				myTimer.Elapsed += TCPPool.CheckPool;
				myTimer.Interval = 10000;
				myTimer.Start();

				// TCP Network connection
				_tcpThread = new Thread(TCPListenerHandler) {Name = "TCPListenerThread"};
				_tcpThread.Start();
				Console.WriteLine("[+] Network connection initialized");

				// Start game ticks
				gt = new GameTimeLight();
				Console.WriteLine("[+] Ticks started");
				while (true) {
					Update();
					//Thread.Sleep(1);
				}
            //}
            //catch (Exception ex) {
            //    if (TCPPool != null)
            //        TCPPool.SendMessageToAll("ms|system|Server fail detected, shutting down");
            //    Console.WriteLine(ex.Message);
            //    Console.WriteLine(ex.StackTrace);
            //    Console.WriteLine(ex.ToString());
            //    Console.ReadLine();
            //}
		}

		private void Update() {
			// Max 10ticks /seck
			if (gt.ElapsedGameTime.Milliseconds < 100) {
				Thread.Sleep(100);
			}
			//Console.WriteLine("Ticked at " + gt.ElapsedGameTime + " - " + GameObjects.Count);
			lock (GameObjects) {
				foreach (var gameObject in GameObjects.ToList()) {
					gameObject.Update(gt);
				}
			}
			foreach (var connectionThread in TCPPool.Connections) {
				if (connectionThread.IsLoggedIn) {
					lock (connectionThread.LoggedInUser) {
						connectionThread.LoggedInUser.Update(gt);
					}
				}
			}
			gt.Updated();
		}

		public void TCPConnectionHandler(Object connection) {
			try {
				var ct = new ConnectionThread((TcpClient) connection);
				TCPPool.Add(ref ct);
				ct.StartListen();
			}
			catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void TCPListenerHandler() {
			TcpListener serverListenerTCP = null;

			try {
				serverListenerTCP = new TcpListener(IPAddress.Any, TCPLocalPort);
				serverListenerTCP.Start();
				Console.WriteLine("Client system on tcp://0.0.0.0:" + TCPLocalPort + " started OK (ThreadID=" +
				                  _tcpThread.ManagedThreadId + ")");
				while (true) {
					var connection = serverListenerTCP.AcceptTcpClient();
					var t = new Thread(TCPConnectionHandler)
					{
					    Name = "Client ConnectionThread [" + connection.Client.RemoteEndPoint + "]"
					};
				    t.Start(connection);
				}
			}
			catch (Exception e) {
				Console.WriteLine(e.ToString());
			}
			finally {
				if (serverListenerTCP != null) serverListenerTCP.Stop();
			}
		}

		public void DisconnectUser(string username) {
			TCPPool.DisconnectUser(username);
		}

		public void RemoveEntity(Entity e) {
			TheGrid.GetBlockAt(e.Position).Entities.RemoveById(e.Id);
			lock (GameObjects) {
				GameObjects.RemoveById(e.Id);
			}
		}

		internal void SpawnRandomEntity() {
			LivingEntity monster1;
			if(Helper.Random(1,10) > 7) {
				monster1 = new Ghost();
			}
			else
			{
				monster1 = new Skeleton();
			}
			Position2D randPos = TheGrid.FindRandomEmptyGrassBlock();
			monster1.Position = randPos;
		    var pld = new NetworkPayload()
		    {
		        Type = PayloadType.Data,
		        Command = PayloadCommand.InitEntity,
		        Params = {monster1.MakeDump()}
		    };

            TCPPool.SendPayloadToAll(pld);
			TheGrid.GetBlockAt(randPos).Entities.Add(monster1);
			lock (GameObjects) {
				GameObjects.Add(monster1);
			}
		}
	}
}