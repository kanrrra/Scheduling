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

        public bool presetTask;

        public AgeGroup minimumAgeGroup;
        public Player person;

        public Task linkedTask = null;

        public string Note { get => note; private set => note = value; }

        public Task(string note, TaskType type, DateTime start, DateTime end, int minimumAge, RefereeQualification refereeQualification, AgeGroup minimumAgeGroup, bool presetTask = false)
        {
            this.Note = note;
            this.type = type;
            this.startTime = start;
            this.endTime = end;
            this.minimumAge = minimumAge;
            this.refereeQualification = refereeQualification;
            this.presetTask = presetTask;

            this.minimumAgeGroup = minimumAgeGroup;
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

            return ((presetTask ? "p " : "") + name).PadRight(15) + "\t" + Note.PadRight(11) + " " + startTime + "-" + endTime.TimeOfDay;
        }

        public string ToCSV()
        {
            return startTime.ToShortDateString() + "," + startTime.ToShortTimeString() + "," + Note + "," + type + "," + person.name;
        }

        public void SetLinkedTask(Task t)
        {
            linkedTask = t;
        }

        public Task GetLinkedTask()
        {
            return linkedTask;
        }

        public DateTime getAgeQualification()
        {
            return startTime.Date.AddYears(-minimumAge);
        }

        public RefereeQualification GetRefereeQualification()
        {
            return refereeQualification;
        }

        public bool LinkedTaskScheduledToSameTeam(List<Team> t)
        {
            if (linkedTask == null) return false;
            if (linkedTask.person == null) return false;

            if (linkedTask.person.teams.Intersect(t).ToList().Count > 0) return true;

            return false;
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
