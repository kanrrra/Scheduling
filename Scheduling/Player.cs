using System;
using System.Collections.Generic;
using System.Linq;
using static Scheduling.Qualifications;

namespace Scheduling
{

    class Player {
        public readonly string name;
        public readonly List<string> teamNames = new List<string>();

        private readonly RefereeQualification refereeQualification;
        private readonly DateTime dateOfBirth;

        public List<Team> teams = new List<Team>();
        public List<Task> tasks = new List<Task>();

        double SAME_TEAM_BARSHIFT_MULTIPLIER = 0.8;
        private double accruedCost;


        public Player(string name, string teamName, string refereeQualificationText, DateTime dateOfBirth, double accruedCost)
        {
            this.name = name;

            string[] tokens = teamName.Split('.');
            foreach(string tn in tokens)
            {
                teamNames.Add(tn.Trim());
            }


            this.refereeQualification = textLabelToReferee(refereeQualificationText);
            this.dateOfBirth = dateOfBirth;
            this.accruedCost = accruedCost;
        }

        public void addTeam(Team t)
        {
            teams.Add(t);
        }

        public void addTask(Task t)
        {
            tasks.Add(t);
            t.person = this;
        }

        public void removeTask(Task t)
        {
            tasks.Remove(t);
        }

        public double getCurrentCost()
        {
            double cost = tasks.Sum(t => getCostCurrentTask(t));

            return cost + accruedCost;
        }

        public int hasTaskOnDate(DateTime date)
        {
            return tasks.Count(t => t.startTime.Date == date.Date);
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
            return teams.Any(t => t.matches.Any(m => hasTimeOverlap(m.GetPlayerStartTime(), m.GetEndTime(), startTime, endTime)));
        }

        public List<Match> getMatchOnDay(DateTime day)
        {
            return teams.Where(t => t.matches.Any(m => day.Date == m.GetPlayerStartTime().Date)).Select(t =>  t.matches.Find(m => day.Date == m.GetPlayerStartTime().Date)).ToList();
        }

        public bool isBusyOnTime(DateTime startTime, DateTime endTime)
        {
            return hasTaskOnTime(startTime, endTime) || hasMatchOnTime(startTime, endTime);
        }

        public bool canPerformTaskOnDay(DateTime dateTime)
        {
            return !teams.Any(t => t.unavailableDates.Contains(dateTime.Date)) && (teams[0].allowSchedulingOnNonMatchDay || teams.Any(t => t.matches.Any(m => dateTime.Date == m.GetPlayerStartTime().Date)));
        }

        public bool isQualified(Task t)
        {
            return (dateOfBirth <= t.getAgeQualification() && refereeQualification >= t.GetRefereeQualification());
        }

        public double getGainRemoveTask(Task t)
        {
            double timeCost = getTimeCost(t);
            if (timeCost < 0)
            {
                throw new Exception();
            }
            if (t.LinkedTaskScheduledToSameTeam(teams))
            {
                timeCost *= SAME_TEAM_BARSHIFT_MULTIPLIER;
            }

            //pref not multiple matches on same day
            int tasksOnDate = hasTaskOnDate(t.startTime.Date);
            if (tasksOnDate > 1)
            {
                //if you have more than 1 task on a day, every task costs half an hour extra
                return timeCost + tasksOnDate * 0.5;
            }

            return timeCost;
        }

        public double getCostNewTask(Task t)
        {
            if (!isQualified(t)) return -1;

            double timeCost = getTimeCost(t);
            if (timeCost < 0)
            {
                throw new Exception();
            }
            if (t.LinkedTaskScheduledToSameTeam(teams))
            {
                timeCost *= SAME_TEAM_BARSHIFT_MULTIPLIER;
            }

            //pref not multiple matches on same day
            int tasksOnDate = hasTaskOnDate(t.startTime.Date);
            if(tasksOnDate > 0)
            {
                //if you have more than 1 task on a day, every task costs half an hour extra
                return timeCost + (tasksOnDate + 1) * 0.5;
            }

            return timeCost;
        }

        public double getCostCurrentTask(Task t)
        {
            if (!tasks.Contains(t))
            {
                throw new Exception();
            }

            double timeCost = getTimeCost(t);
            if (t.LinkedTaskScheduledToSameTeam(teams))
            {
                timeCost *= SAME_TEAM_BARSHIFT_MULTIPLIER;
            }

            if (timeCost < 0)
            {
                throw new Exception();
            }

            int tasksOnDate = hasTaskOnDate(t.startTime.Date);
            if (tasksOnDate > 1)
            {
                //if you have more than 1 task on a day, every task costs half an hour extra
                return timeCost + 0.5;
            }

            return timeCost;

        }

        private double getTimeCost(Task t)
        {
            double duration = t.endTime.Subtract(t.startTime).TotalHours;

            double minWaitTime = double.MaxValue;
            foreach (Team team in teams)
            {
                Match m = team.matches.Find(match => match.GetPlayerStartTime().Date == t.startTime.Date);
                if(m != null)
                {
                    double waitTime = double.MaxValue;

                    //after
                    if (t.startTime >= m.GetEndTime())
                    {
                        waitTime = t.startTime.Subtract(m.GetEndTime()).TotalHours;
                    }
                    else if (t.endTime <= m.GetPlayerStartTime())
                    {
                        //before
                        waitTime = m.GetPlayerStartTime().Subtract(t.endTime).TotalHours;
                    }
                    else
                    {
                        throw new Exception("Task during match!");
                        //during
                    }

                    if(waitTime != double.MaxValue)
                    {
                        if(waitTime < minWaitTime)
                        {
                            minWaitTime = waitTime;
                        }
                        break;
                    }

                }
            }

            if(minWaitTime != double.MaxValue)
            {
                return duration + Math.Min(1.5, Math.Sqrt(minWaitTime));
            }

            //no match on this day
            if (teams[0].allowSchedulingOnNonMatchDay)
            {
                return 1.0 + duration;
            }
            throw new Exception("No match on this day and team.allowSchedulingOnNonMatchDay is not set");
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
                id += "T: " + t + " " + getCostCurrentTask(t) + "\n";
            }

            return id;
        }

        public string ToCSV()
        {
            return name + "," + teamNames.Aggregate((a, b) => a + "." + b) + "," + refereeQualification + "," + dateOfBirth.ToShortDateString() + "," + getCurrentCost();
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
