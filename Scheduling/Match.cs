using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduling
{
    class Match
    {
        public readonly string teamName;
        public Team team;

        public DateTime startTime;
        public DateTime endTime;


        public Match(string teamName, DateTime startTime)
        {
            this.teamName = teamName;
            this.startTime = startTime;
            endTime = startTime.AddHours(2);
        }

        public override string ToString()
        {
            return teamName + " " + startTime;
        }

        public bool requiresReferee()
        {
            return team.minimumRefereeQualification != Qualifications.RefereeQualification.National;
        }

        public int flagsRequired()
        {
            return team.flagsRequired;
        }

        public int additionalsRequired()
        {
            return team.additionalPeopleRequired;
        }
    }
}
