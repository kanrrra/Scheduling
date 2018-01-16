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

        public enum AgeQualification
        {
            None,
            Adult
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
                default:
                    Console.Out.WriteLine("Unknown level found: " + level);
                    return RefereeQualification.VS1;
            }
        }

        internal static AgeQualification textToAgeQualification(string text)
        {
            if (text.ToLower() == "adult")
            {
                return AgeQualification.Adult;
            } else
            {
                return AgeQualification.None;
            }
        }
    }
}
