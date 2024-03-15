using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrañaManuelServ_SF2a
{
    internal class InfoClient
    {
        private string ip;
        private int port;
        private string time;

        public InfoClient(string ip,int port,string time) 
        {
            this.ip = ip;
            this.port = port;
            this.time = time;
        }

        public string Ip 
        {
            set { ip = value; }
            get { return ip; }
        }

        public int Port 
        {
            set {  port = value; }
            get { return port; }
        }

        public string Time
        {
            set { time = value; }
            get { return time; }
        }

        public override string ToString()
        {
            return Ip+" "+Port+" "+Time;
        }

    }
}
