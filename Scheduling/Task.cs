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
        private AgeQualification ageQualification;
        private RefereeQualification refereeQualification;
        private string note;

        public Player person;

        public Task(string note, TaskType type, DateTime start, DateTime end, AgeQualification ageQualification, RefereeQualification refereeQualification)
        {
            this.note = note;
            this.type = type;
            this.startTime = start;
            this.endTime = end;
            this.ageQualification = ageQualification;
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

            return name + " " + note + " " + startTime;
        }

        public AgeQualification getAgeQualification()
        {
            return ageQualification;
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
