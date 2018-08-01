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
        private readonly DateTime dateOfBirth;

        private Team team;
        public List<Task> tasks = new List<Task>();


        public Player(string name, string teamName, string refereeQualificationText, DateTime dateOfBirth)
        {
            this.name = name;
            this.teamName = teamName;

            this.refereeQualification = textLabelToReferee(refereeQualificationText);
            this.dateOfBirth = dateOfBirth;
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
            return team.matches.Any(m => hasTimeOverlap(m.GetPlayerStartTime(), m.GetEndTime(), startTime, endTime));
        }

        public Match getMatchOnDay(DateTime day)
        {
            return team.matches.Find(m => day.Date == m.GetPlayerStartTime().Date);
        }

        public bool isBusyOnTime(DateTime startTime, DateTime endTime)
        {
            return hasTaskOnTime(startTime, endTime) || hasMatchOnTime(startTime, endTime);
        }

        public bool canPerformTaskOnDay(DateTime dateTime)
        {
            return team.allowSchedulingOnNonMatchDay || team.matches.Any(m => dateTime.Date == m.GetPlayerStartTime().Date);
        }

        public bool isQualified(Task t)
        {
            return (dateOfBirth <= t.getAgeQualification() && refereeQualification >= t.GetRefereeQualification());
        }

        public double getCost(Task t)
        {
            if (!isQualified(t)) return -1;

            double duration = t.endTime.Subtract(t.startTime).TotalHours;


            foreach (Match m in team.matches)
            {
                if(t.startTime.Date == m.GetPlayerStartTime().Date)
                {
                    //after
                    if(t.startTime >= m.GetEndTime())
                    {
                        double waitTime = t.startTime.Subtract(m.GetEndTime()).TotalHours;
                        return duration + Math.Sqrt(waitTime);
                    } else if(t.endTime <= m.GetPlayerStartTime())
                    {
                        //before
                        double waitTime = m.GetPlayerStartTime().Subtract(t.endTime).TotalHours;
                        return duration + Math.Sqrt(waitTime);
                    } else
                    {
                        return -1;
                        //during
                    }
                }
            }

            //no match on this day
            if (team.allowSchedulingOnNonMatchDay)
            {
                return 1.0 + duration;
            }
            return -1;
        }

        private bool hasTimeOverlap(DateTime startTimeA, DateTime endTimeA, DateTime startTimeB, DateTime endTimeB)
        {
            return startTimeA < endTimeB && startTimeB < endTimeA;
        }

        public override string ToString()
        {
            string id = name + " " + dateOfBirth + ":" + refereeQualification + ": " + getCurrentCost() + "\n";
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
                return false;//return ageQualification > alternativePlayer.ageQualification;
            }
        }
    }
}
