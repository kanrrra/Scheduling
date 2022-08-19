using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Scheduling
{
    internal class Match
    {
        public readonly string teamName;

        public readonly string opponent;

        public readonly string field;

        public Team team { get; private set; }

        public string refName { get; set; }
        public string scoreName { get; set; }

        private DateTime startTime;
        private DateTime realStartTime;

        private bool generateTasks;
        private List<Task> tasks = new List<Task>();

        public Match(string teamName, string opponent, DateTime startTime, string referee, string score, bool generateTasks, string field)
        {
            this.teamName = teamName;
            this.opponent = opponent;
            this.field = field;

            refName = referee;
            scoreName = score;

            refName = Regex.Replace(refName, @"\(.*?\)", "").Trim();
            scoreName = Regex.Replace(scoreName, @"\(.*?\)", "").Trim();

            this.startTime = startTime;
            realStartTime = startTime;

            this.generateTasks = generateTasks;

            //revert timechange for late/short matches, otherwise the duration of 90 minutes results in the followup team not being able to ref/count
            if (this.startTime.Minute == 45)
                this.startTime = this.startTime.AddMinutes(-15);
        }

        public bool GenerateTasks()
        {
            return generateTasks;
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
            return startTime.AddMinutes(-15);
        }

        public DateTime GetPlayerStartTime()
        {
            return startTime.AddMinutes(-(30 + team.additionalPreMatchTime));
        }

        public DateTime GetProgramStartTime()
        {
            return startTime;
        }

        public DateTime GetEndTime()
        {
            return startTime.AddMinutes(team.matchDurationMinutes);
        }

        private string ShortTeamName()
        {
            return teamName.Substring(teamName.IndexOf("Taurus ") + 7);
        }

        public override string ToString()
        {
            string shortTeamName = ShortTeamName();

            return shortTeamName + " " + realStartTime;
        }

        public string ToCSV()
        {
            string s = realStartTime.ToShortDateString() + "," + realStartTime.ToShortTimeString() + "," + teamName + "," + opponent + ",";

            Task referee = tasks.Find(t => t.type == TaskType.Referee);
            if (referee != null)
            {
                if (referee.person != null)
                {
                    s += referee.person.name + " (" + referee.person.ShortTeamName() + ")";
                } else
                {
                    s += "TODO!";
                }
            } else if (refName.Length > 0)
            {
                s += refName;// + " (vol)";
            }
            s += ",";

            Task scoreKeeping = tasks.Find(t => t.type == TaskType.ScoreKeeping);
            if (scoreKeeping != null)
            {
                if (scoreKeeping.person != null)
                {
                    s += scoreKeeping.person.name + " (" + scoreKeeping.person.ShortTeamName() + ")";
                } else
                {
                    s += "TODO!";
                }
            }
            s += $", {field}, Kruisboog";

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


        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is Match))
                return false;

            var other = obj as Match;

            if (teamName != other.teamName ||
                opponent != other.opponent ||
                startTime != other.startTime)
                return false;

            return true;
        }

        public static bool operator ==(Match x, Match y)
        {
            if (object.ReferenceEquals(x, null))
            {
                return object.ReferenceEquals(y, null);
            }

            return x.Equals(y);
        }

        public static bool operator !=(Match x, Match y)
        {
            return !(x == y);
        }
    }
}
