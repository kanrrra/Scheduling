using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Scheduling.Qualifications;

namespace Scheduling
{
    class Task
    {
        public TaskType type;
        public DateTime startTime;
        public DateTime endTime;
        private RefereeQualification refereeQualification;
        private int minimumAge;
        private string note;

        public Player person;

        public Task(string note, TaskType type, DateTime start, DateTime end, int minimumAge, RefereeQualification refereeQualification)
        {
            this.note = note;
            this.type = type;
            this.startTime = start;
            this.endTime = end;
            this.minimumAge = minimumAge;
            this.refereeQualification = refereeQualification;
        }

        public override string ToString()
        {
            string name = "Scorekeeping";
            if (type == TaskType.Linesman)
            {
                name = "Linesman";
            }
            else if (type == TaskType.BarKeeper)
            {
                name = "barkeeper";
            } else if(type == TaskType.Referee)
            {
                name = "Referee";
            }

            return name.PadRight(15) + "\t" + note.PadRight(11) + " " + startTime + "-" + endTime.TimeOfDay;
        }

        public DateTime getAgeQualification()
        {
            return startTime.Date.AddYears(-minimumAge);
        }

        public RefereeQualification GetRefereeQualification()
        {
            return refereeQualification;
        }
    }

    enum TaskType
    {
        Referee,
        ScoreKeeping,
        Linesman,
        BarKeeper
    }
}
