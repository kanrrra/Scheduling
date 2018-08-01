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
        public Team team { get; private set; }

        private DateTime startTime;
        private DateTime endTime;


        public Match(string teamName, DateTime startTime)
        {
            this.teamName = teamName;
            this.startTime = startTime;
            endTime = startTime.AddHours(2);
        }

        public void SetTeam(Team t)
        {
            team = t;
        }

        public DateTime GetRefereeStartTime()
        {
            return startTime.AddMinutes(-30);
        }

        public DateTime GetPlayerStartTime()
        {
            return startTime.AddMinutes(-(30 + team.additionalPreMatchTime));
        }

        public DateTime GetEndTime()
        {
            return startTime.AddMinutes(team.matchDurationMinutes);
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
