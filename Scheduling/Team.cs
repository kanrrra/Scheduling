using System.Collections.Generic;
using static Scheduling.Qualifications;

namespace Scheduling
{
    class Team
    {
        public List<Match> matches = new List<Match>();
        public string name;
        public int flagsRequired;
        public int additionalPeopleRequired; //tellen etc

        public RefereeQualification minimumRefereeQualification;

        public Team(string name, string level, int additionalPeopleRequired, int flagsRequired)
        {
            this.name = name;
            this.additionalPeopleRequired = additionalPeopleRequired;
            this.flagsRequired = flagsRequired;

            minimumRefereeQualification = Qualifications.textTeamToReferee(level);
        }

        public void addMatch(Match m)
        {
            matches.Add(m);
        }

    }
}