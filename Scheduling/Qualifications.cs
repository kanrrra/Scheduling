using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduling
{
    class Qualifications
    {

        public enum RefereeQualification
        {
            None,
            VS1,//spelregeltoets, tot 3de klasse
            VS2,//tot 1e klasse
            VS3,
            VS4,
            National
        }

        internal static RefereeQualification textLabelToReferee(string text)
        {
            switch (text.ToLower())
            {
                case "vs1":
                    return RefereeQualification.VS1;
                case "vs2":
                    return RefereeQualification.VS2;
                default:
                    return RefereeQualification.None;
            }
        }

        public enum AgeGroup
        {
            Mini,
            MC,
            JC,
            MB,
            JB,
            MA,
            JA,
            Senior,
            Recreative
        }

        internal static AgeGroup textToAgeGroupRef(string teamName)
        {
            string clubName = "taurus";

            var letter = teamName.Substring(teamName.IndexOf(clubName) + clubName.Length + 1, 2);
            switch (letter)
            {
                case "ma":
                    return AgeGroup.MA;
                case "ja":
                    return AgeGroup.JA;
                case "mb":
                    return AgeGroup.MB;
                case "jb":
                    return AgeGroup.JB;
                case "mc":
                    return AgeGroup.MC;
                case "jc":
                    return AgeGroup.JC;
                case "hs":
                case "ds":
                    return AgeGroup.Senior;
                case "hr":
                case "dr":
                case "xr":
                    return AgeGroup.Recreative;
                default:
                    if (letter.ElementAt(0) == 'n')
                    {
                        return AgeGroup.Mini;
                    } else
                    {
                        throw new Exception("Unknown age group: " + letter);
                    }

            }
        }

        internal static RefereeQualification textTeamToReferee(string level)
        {
            if (level.Contains("divisie"))
            {
                return RefereeQualification.National;
            }

            switch (level.ToLower())
            {
                case "promotieklasse":
                    return RefereeQualification.National;
                case "1e klasse":
                    return RefereeQualification.VS2;
                case "2e klasse":
                    return RefereeQualification.VS2;
                case "topklasse"://jeugd
                    return RefereeQualification.VS2;
                case "3e klasse":
                    return RefereeQualification.VS1;
                case "4e klasse":
                    return RefereeQualification.VS1;
                case "jeugd":
                    return RefereeQualification.VS1;
                case "mini":
                    return RefereeQualification.None;
                default:
                    Console.Out.WriteLine("Unknown level found: " + level);
                    return RefereeQualification.VS1;
            }
        }
    }
}
