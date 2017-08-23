using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldCupPlan
{
    class Match
    {
        public string Local { get; set; }
        public string Visitor { get; set; }
        public Venue Venue { get; set; }
        public DateTime MatchDateTimeUTC { get; set; }
        public string MatchName { get; set; }

    }
}
