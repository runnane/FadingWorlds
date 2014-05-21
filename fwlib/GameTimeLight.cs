using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fwlib
{
    public class GameTimeLight
    {
        public readonly DateTime GameStartedTime;
        public DateTime LastUpdatedTime;

        //public readonly long GameStartedTimeTicks;
        //public long LastUpdatedTimeTicks;

        public GameTimeLight()
        {
            GameStartedTime = DateTime.Now;
            LastUpdatedTime = DateTime.Now;
            //GameStartedTimeTicks = DateTime.Now.Ticks;
            //LastUpdatedTimeTicks = DateTime.Now.Ticks;
        }

        public TimeSpan TotalGameTime
        {
            get { return (DateTime.Now - GameStartedTime); }
        }

        public TimeSpan ElapsedGameTime
        {
            get { return DateTime.Now - LastUpdatedTime; }
        }

        //public long Ticks {
        //  get { return DateTime.Now.Ticks - LastUpdatedTimeTicks; }
        //}

        public void Updated()
        {
            LastUpdatedTime = DateTime.Now;
            //LastUpdatedTimeTicks = DateTime.Now.Ticks;
        }

        public string Status()
        {
            return " // " + LastUpdatedTime + " // " + GameStartedTime + " // " + (DateTime.Now - LastUpdatedTime);
        }
    }
}
