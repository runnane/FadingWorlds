using System;
using System.IO;
using System.Reflection;
using System.Threading;
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
		Running,
        StoppingGameWindow
	}

	public partial class Loader : Form {
		internal FadingWorldsGameWindow TheGame;
		private Thread _socketThread;
		private Thread _gameThread;
		internal ConnectionLoop ConnectionLoop = null;
		public GameState State { get; set; }
	    public bool ConnectionAlive = true;

        public string Version { get; set; }

		private string _username;
		private string _password;
		private string _hostname;


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

	    internal delegate void ThreadSafeSetDelegate(string input, TextBoxBase tb);
        internal void ThreadSafeSet(string input, TextBoxBase tb)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ThreadSafeSetDelegate(ThreadSafeSet), input, tb);
            }
            else
            {
                tb.Text = input;
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
			_hostname = txtHost.Text;
			_username = txtUsername.Text;
			_password = txtPassword.Text;

			// Starting tcp con
            Message("StartConnect() Creating new thread");
			_socketThread = new Thread(InitiateConnectionThread) {Name = "ConnectionThread"};
			_socketThread.Start();

		}

		public void InitiateConnectionThread() {
			try {
                Message("InitiateConnectionThread() Thread started, connecting.");
				ConnectionLoop = new ConnectionLoop(this);
				ConnectionLoop.StartConnect(_hostname, 4100, _username, _password);
				ConnectionLoop.Disconnect();
				//ConnectionLoop = null;
                Message("InitiateConnectionThread() stopped after Run()");
			}
			catch (ThreadAbortException ex) {
                Console.WriteLine(ex.ToString());
				throw ex;
			}
			catch (Exception ex) {
				Console.WriteLine(ex.ToString());
				if (ConnectionLoop != null) {
					ConnectionLoop.Disconnect();
					ConnectionLoop = null;
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
			_gameThread = new Thread(InitiateGameThread) {Name = "GameThread"};
			_gameThread.Start(s);
			State = GameState.Starting;
		}


	    public void InitiateGameThread(object o)
	    {
            Message("InitiateGameThread() starting"); 
            TheGame = new FadingWorldsGameWindow(this, (string[])o);
	        TheGame.Run();
	        Message("InitiateGameThread() stopped after Run()");
	    }

	    private void Loader_FormClosed(object sender, FormClosedEventArgs e) {
		}

		private void Loader_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (ConnectionLoop != null && ConnectionLoop.IsConnected)
			{
				ConnectionLoop.Disconnect();
			}
			if (ConnectionLoop != null)
			{
				//
			}
			if (_gameThread != null)
			{
                _gameThread.Abort();
				//gameThread.Join();
			}
			if (_socketThread != null)
			{
                _socketThread.Abort();
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
				if(ConnectionLoop != null && ConnectionLoop.IsConnected) {
                    ConnectionLoop.SendPayload(new NetworkPayload { Type = PayloadType.Quit});
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
            Message("Loader.Disconnect() called"); 
            State = GameState.StoppingGameWindow;
            ConnectionLoop.Disconnect();
	        ConnectionLoop = null;
            State = GameState.InLoader;
            Message("Loader.Disconnect() completed");
	    }
	}
}