using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FadingWorldsServer.GameObjects.Living;

namespace FadingWorldsServer.ServerObjects
{
	public class UserDB
	{
		public List<Player> Users;

		public UserDB() {
			Users = new List<Player>();
			Users.Add(new Player { Username = "psax", Password = "password", Email = "psax@runnane.com" });
			Users.Add(new Player { Username = "psax1", Password = "password", Email = "psax@runnane.com" });
			Users.Add(new Player { Username = "psax2", Password = "password", Email = "psax@runnane.com" });
			Users.Add(new Player { Username = "psax3", Password = "password", Email = "psax@runnane.com" });
			Users.Add(new Player { Username = "jacob", Password = "password", Email = "psax@runnane.com" });
			Users.Add(new Player { Username = "core", Password = "password", Email = "psax@runnane.com" });
			Users.Add(new Player { Username = "morten", Password = "password", Email = "psax@runnane.com" });
			Users.Add(new Player { Username = "daniel", Password = "password", Email = "psax@runnane.com" });
			Users.Add(new Player { Username = "dani", Password = "password", Email = "psax@runnane.com" });
			Users.Add(new Player { Username = "roar", Password = "password", Email = "psax@runnane.com" });
			Users.Add(new Player { Username = "henning", Password = "password", Email = "psax@runnane.com" });
			Users.Add(new Player { Username = "rune", Password = "password", Email = "psax@runnane.com" });

		}
	}
}
