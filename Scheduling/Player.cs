using System;
using System.Collections.Generic;
using System.Linq;
using static Scheduling.Qualifications;

namespace Scheduling
{

    class Player {
        public readonly string name;
        public readonly string teamName;

        private readonly RefereeQualification refereeQualification;
        private readonly AgeQualification ageQualification;

        private Team team;
        public List<Task> tasks = new List<Task>();


        public Player(string name, string teamName, string refereeQualificationText)
        {
            this.name = name;
            this.teamName = teamName;

            this.refereeQualification = textLabelToReferee(refereeQualificationText);

            if(teamName.ToLower().Contains("ds") || teamName.ToLower().Contains("hs")){
                this.ageQualification = AgeQualification.Adult;
            } else {
                this.ageQualification = AgeQualification.None;
            }

        }

        public void setTeam(Team t)
        {
            team = t;
        }

        public void addTask(Task t)
        {
            tasks.Add(t);
        }

        public void removeTask(Task t)
        {
            tasks.Remove(t);
        }

        public double getCurrentCost()
        {
            double cost = tasks.Sum(t => getCost(t));

            return cost;
        }

        public bool hasTaskOnDate(DateTime date)
        {
            return tasks.Any(t => t.startTime.Date == date.Date);
        }

        public bool hasTaskOnTime(DateTime startTime, DateTime endTime)
        {
            return tasks.Any(t => hasTimeOverlap(t.startTime, t.endTime, startTime, endTime));
        }

        public bool hasOtherTaskOnTime(Task excludedTask, DateTime startTime, DateTime endTime)
        {
            return tasks.Any(t => t != excludedTask && hasTimeOverlap(t.startTime, t.endTime, startTime, endTime));
        }

        public bool hasMatchOnTime(DateTime startTime, DateTime endTime)
        {
            return team.matches.Any(m => hasTimeOverlap(m.startTime, m.endTime, startTime, endTime));
        }

        public bool isBusyOnTime(DateTime startTime, DateTime endTime)
        {
            return hasTaskOnTime(startTime, endTime) || hasMatchOnTime(startTime, endTime);
        }

        public bool isQualified(Task t)
        {
            return (ageQualification >= t.getAgeQualification() && refereeQualification >= t.GetRefereeQualification());
        }

        public double getCost(Task t)
        {
            if (!isQualified(t)) return -1;

            double duration = t.endTime.Subtract(t.startTime).TotalHours;


            foreach (Match m in team.matches)
            {
                if(t.startTime.Date == m.startTime.Date)
                {
                    //after
                    if(t.startTime >= m.endTime)
                    {
                        double waitTime = t.startTime.Subtract(m.endTime).TotalHours;
                        return duration + Math.Min(0.5 * waitTime, 1);
                    } else if(t.endTime <= m.startTime)
                    {
                        //before
                        double waitTime = m.startTime.Subtract(t.endTime).TotalHours;
                        return duration + Math.Min(0.5 * waitTime, 1);
                    } else
                    {
                        return -1;
                        //during
                    }
                }
            }

            //no match on this day
            return 2.0 + duration;
        }

        private bool hasTimeOverlap(DateTime startTimeA, DateTime endTimeA, DateTime startTimeB, DateTime endTimeB)
        {
            return startTimeA < endTimeB && startTimeB < endTimeA;
        }

        public override string ToString()
        {
            string id = name + " " + ageQualification + ":" + refereeQualification + ": " + getCurrentCost() + "\n";
            foreach(Task t in tasks)
            {
                id += "T: " + t + " " + getCost(t) + "\n";
            }

            return id;
        }

        internal bool isMoreQualified(Player alternativePlayer, Task task)
        {
            if (task.type == TaskType.Referee)
            {
                return refereeQualification > alternativePlayer.refereeQualification;
            }
            else
            {
                return ageQualification > alternativePlayer.ageQualification;
            }
        }
    }
}
