using System;
using System.Collections.Generic;
using static Scheduling.Qualifications;

namespace Scheduling
{
    class Team
    {
        public List<Match> matches = new List<Match>();
        public string name { get; private set; }
        public int flagsRequired { get; private set; }
        public int additionalPeopleRequired { get; private set; } //tellen etc
        public int additionalPreMatchTime { get; private set; }
        public int matchDurationMinutes { get; private set; }

        public HashSet<DateTime> unavailableDates { get; private set; } = new HashSet<DateTime>();

        public bool allowSchedulingOnNonMatchDay = true;

        public RefereeQualification minimumRefereeQualification;

        public Team(string name, string level, int additionalPeopleRequired, int flagsRequired, int additionalPreMatchTime, int matchDurationMinutes)
        {
            this.name = name;
            this.additionalPeopleRequired = additionalPeopleRequired;
            this.flagsRequired = flagsRequired;

            this.additionalPreMatchTime = additionalPreMatchTime;
            this.matchDurationMinutes = matchDurationMinutes;

            minimumRefereeQualification = Qualifications.textTeamToReferee(level, name);
        }

        public void addExceptionDate(DateTime date)
        {
            unavailableDates.Add(date);
        }

        public void addMatch(Match m)
        {
            matches.Add(m);

            if(m.GetPlayerStartTime().DayOfWeek == System.DayOfWeek.Saturday)
            {
                allowSchedulingOnNonMatchDay = false;
            }
        }

        public override string ToString()
        {
            return "" + name + "\n" +
                "extra pre match time: " + additionalPreMatchTime + "\n" +
                "additional people: " + additionalPeopleRequired + "\n" +
                "flags: " + flagsRequired + "\n" +
                "match duration: " + matchDurationMinutes + "\n" +
                "ref: " + minimumRefereeQualification + "\n" + 
                "non match day: " + allowSchedulingOnNonMatchDay;
        }

        }
}