using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using fwlib;

namespace FadingWorldsClient
{
	public enum GameState {
		InLoader,
		Starting,
		LoggingIn,
		LoggedIn,
		WaitingForMap,
		Running
	}

	public partial class Loader : Form {
		internal FadingWorldsApp TheGame;
		private Thread socketThread = null;
		private Thread gameThread = null;
		internal ConnectionLoop connectionLoop = null;
		public GameState State { get; set; }


        public string Version { get; set; }

		private string username;
		private string password;
		private string hostname;


		public Loader() {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FadingWorldsClient." + "version.txt"))
            {
                if (stream != null)
                    using (var reader = new StreamReader(stream))
                    {
                        Version = reader.ReadToEnd();
                    }
            }

			InitializeComponent();
			State = GameState.InLoader;
		    
		}

		internal delegate void SetVisibleDelegate(bool p);

		internal void SetVisible(bool p) {
			if (InvokeRequired) {
				BeginInvoke(new SetVisibleDelegate(SetVisible), p);
			}else {
				Visible = p;
			}
		}


	    private void Loader_Load(object sender, EventArgs e)
	    {
	        Text = "Fading Worlds Loader [" + Version + "]";
	    }

		private void btnLogin_Click(object sender, EventArgs e) {
			StartConnect();
			State = GameState.LoggingIn;
		}

		public void ClearUserList() {
			//throw new NotImplementedException();
		}

		public void AddUserToList(string s) {
			//throw new NotImplementedException();
		}

		public void StopReconnectSequence() {
			//throw new NotImplementedException();
		}

		public void SetLoggedIn(bool b) {
			//	throw new NotImplementedException();
		}

		public void StartReconnectSequence() {
			//throw new NotImplementedException();
		}

		public void StartConnect() {
			//// Starting UDP:
			//udpListenerThread.Start();
			hostname = txtHost.Text;
			username = txtUsername.Text;
			password = txtPassword.Text;


			// Starting tcp con
            Message("StartConnect() Creating new thread");
			socketThread = new Thread(InitiateConnectionThread) {Name = "ConnectionThread"};
			socketThread.Start();

		}

		public void InitiateConnectionThread() {
			try {
                Message("InitiateConnectionThread() Thread started, connecting.");
				connectionLoop = new ConnectionLoop(this);
				connectionLoop.StartConnect(hostname, 4100, username, password);
				connectionLoop.Disconnect();
				connectionLoop = null;

			}
			catch (ThreadAbortException ex) {
				throw ex;
			}
			catch (Exception ex) {
				Console.WriteLine(ex.ToString());
				if (connectionLoop != null) {
					connectionLoop.Disconnect();
					connectionLoop = null;
				}
				// TODO: FixMe
			}
            Message("InitiateConnectionThread() at end");
		}


		internal void SpawnGame(int blockWidth, int blockHeight, string mapData) {
			var s = new string[3];
			s[0] = blockWidth.ToString();
			s[1] = blockHeight.ToString();
			s[2] = mapData;

            Message("SpawnGame() Creating new thread");
			gameThread = new Thread(InitiateGameThread) {Name = "GameThread"};
			gameThread.Start(s);
			State = GameState.Starting;
		}
											

		public void InitiateGameThread(object o) {
            try
            {
            TheGame = new FadingWorldsApp(this, (string[])o);
            TheGame.Run();
            }
            catch (ThreadAbortException ex)
            {
                throw ex;
            }
            //catch (Exception ex)
            //{
            //    Message(ex.ToString());
            //    // TODO: FixMe
            //}
            Message("Completed");
		}

		private void Loader_FormClosed(object sender, FormClosedEventArgs e) {
		}

		private void Loader_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (connectionLoop != null && connectionLoop.IsConnected)
			{
				connectionLoop.Disconnect();
			}
			if (connectionLoop != null)
			{
				//
			}
			if (gameThread != null)
			{
                gameThread.Abort();
				//gameThread.Join();
			}
			if (socketThread != null)
			{
                socketThread.Abort();
				//socketThread.Join();
			}
			//	Close();
		}

		internal delegate void ExitDelegate();

		internal void Exit()
		{
			if (InvokeRequired)
			{
				BeginInvoke(new ExitDelegate(Exit));
			}else {
				if(connectionLoop != null && connectionLoop.IsConnected) {
                    //connectionLoop.SendCommand("qt");
                    connectionLoop.SendPayload(new NetworkPayload { Type = PayloadType.Quit});
				}
				Close();
			}
		}

		internal delegate void MessageDelegate(string text);

		internal void Message(string text)
		{
			if (InvokeRequired)
			{
				BeginInvoke(new MessageDelegate(Message),text);
			}
			else
			{
				//MessageBox.Show(text);
			    txtOutput.Text += "\r\n" + text;
                txtOutput.SelectionStart = txtOutput.Text.Length;
                txtOutput.ScrollToCaret();
                txtOutput.Refresh();
			}
		}

		private void label1_Click(object sender, EventArgs e)
		{

		}

		public void SetMap(int blockWidth, int blockHeight, string mapData) {
			TheGame.MakeGrid(blockWidth, blockHeight, mapData);
		}


	    public void Disconnect()
	    {
	        connectionLoop.Disconnect();
            socketThread.Abort();
            gameThread.Abort();
	    }
	}
}