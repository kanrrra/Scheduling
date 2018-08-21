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
        private DateTime realStartTime;

        private List<Task> tasks = new List<Task>();

        public Match(string teamName, DateTime startTime)
        {
            this.teamName = teamName;

            this.startTime = startTime;
            realStartTime = startTime;

            //revert timechange for late/short matches, otherwise the duration of 90 minutes results in the followup team not being able to ref/count
            if (this.startTime.Minute == 45)
                this.startTime = this.startTime.AddMinutes(-15);
        }

        public void AddTask(Task t)
        {
            tasks.Add(t);
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
            string shortTeamName = teamName.Substring(teamName.IndexOf("Taurus ") + 7);

            return shortTeamName + " " + realStartTime;
        }

        public string ToCSV()
        {
            string s = startTime.ToShortDateString() + "," + startTime.ToShortTimeString() + "," + teamName + ",";
            
            Task referee = tasks.Find(t => t.type == TaskType.Referee);
            if (referee != null)
            {
                s += referee.person.name;
            }
            s += ",";
            
            Task scoreKeeping = tasks.Find(t => t.type == TaskType.ScoreKeeping);
            if(scoreKeeping != null)
            {
                s += scoreKeeping.person.name;
            }

            return s;
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
