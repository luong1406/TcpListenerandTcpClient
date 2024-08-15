using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sever
{
    public class ConnectionStatistics
    {
        public int IncomingConnections { get; set; }
        public int OutgoingConnections { get; set; }
        public long TotalIncomingData { get; set; }
        public long TotalOutgoingData { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration => DateTime.Now - StartTime;

        public ConnectionStatistics()
        {
            StartTime = DateTime.Now;
        }
    }
}
