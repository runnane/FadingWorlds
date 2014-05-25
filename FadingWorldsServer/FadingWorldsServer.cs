using System;
using System.Dynamic;
using System.IO;
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
	    private const int InitialCountOfMobs = 10;

		public static FadingWorldsServer Instance { get; set; }

		public string Version { get; set; }

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
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FadingWorldsServer." + "version.txt"))
            {
                if (stream != null)
                    using (var reader = new StreamReader(stream))
                    {
                        Version = reader.ReadToEnd();
                    }
            }

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
				GameObjects = new EntityCollection();
				for (var i = 0; i < InitialCountOfMobs; i++) {
					var monster1 = new Skeleton();
					var randPos = TheGrid.FindRandomEmptyGrassBlock();
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

        public void TCPConnectionHandler(object connection)
        {

			try {
                var ct = new ConnectionThread(((ThreadStartParameters)connection).Client, ((ThreadStartParameters)connection).Server);
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
			TcpListener serverListenerTcp = null;

			try {
				serverListenerTcp = new TcpListener(IPAddress.Any, TCPLocalPort);
				serverListenerTcp.Start();
				Console.WriteLine("Client system on tcp://0.0.0.0:" + TCPLocalPort + " started OK (ThreadID=" +
				                  _tcpThread.ManagedThreadId + ")");
				while (true) {
					var connection = serverListenerTcp.AcceptTcpClient();
					//var t = new Thread(new ParameterizedThreadStart(TCPConnectionHandler))
                    var t = new Thread(TCPConnectionHandler)
					{
					    Name = "Client ConnectionThread [" + connection.Client.RemoteEndPoint + "]"
					};
                    t.Start(new ThreadStartParameters { Client = connection, Server = this });
				}
			}
			catch (Exception e) {
				Console.WriteLine(e.ToString());
			}
			finally {
				if (serverListenerTcp != null) serverListenerTcp.Stop();
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
			var randPos = TheGrid.FindRandomEmptyGrassBlock();
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

    public class ThreadStartParameters
    {
        public TcpClient Client { get; set; }
        public FadingWorldsServer Server { get; set; }
    }
}