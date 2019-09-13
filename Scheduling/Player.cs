using System;
using System.Collections.Generic;
using System.Linq;
using static Scheduling.Qualifications;

namespace Scheduling
{

    class Player {
        public readonly string name;
        public readonly List<string> teamNames = new List<string>();

        private readonly AgeGroup ageGroup;
        private readonly RefereeQualification refereeQualification;
        private readonly DateTime dateOfBirth;

        public List<Team> teams = new List<Team>();
        public List<Task> tasks = new List<Task>();

        double SAME_TEAM_BARSHIFT_MULTIPLIER = 0.9;
        double EXTRA_COST_SAME_DAY_TASK = 0.5;
        double NON_MATCH_DAY_BONUS = 0.2;
        private double accruedCost;

        public Player(string name, string teamName, string refereeQualificationText, DateTime dateOfBirth, double accruedCost)
        {
            this.name = name;

            string[] tokens = teamName.Split('.');
            foreach(string tn in tokens)
            {
                teamNames.Add(tn.Trim());
            }

            this.ageGroup = textToAgeGroupRef(teamNames[0].ToLower());

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

            if(tasks.Count > getMaxTasks())
            {
                throw new Exception("HELP");
            }
        }

        public void removeTask(Task t)
        {
            tasks.Remove(t);
            t.person = null;
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
            //if bar after 16:30 -> no old ppl
            if (t.type == TaskType.BarKeeper && t.endTime.TimeOfDay > new TimeSpan(16, 30, 0))        //no slow people during peak time
            {
                int age = t.startTime.Year - dateOfBirth.Year;
                if(ageGroup == AgeGroup.Recreative)
                {
                    return false;
                }

                if(dateOfBirth.AddYears(18) > t.startTime && t.startTime.TimeOfDay < new TimeSpan(20, 0, 0))
                {
                    return false;
                }
            }


            if (dateOfBirth > t.getAgeQualification())         //age
            {
                return false;
            }

            //VS2 only referee VS2
            if (refereeQualification == RefereeQualification.VS2 || refereeQualification == RefereeQualification.VS2_A) { 
                if(t.type != TaskType.Referee)
                {
                    return false;
                }

                //adult vs2 only ref vs2
                //edited to vs2 only ref vs2 or adults
                //if (ageGroup == AgeGroup.Senior && (t.GetRefereeQualification() < RefereeQualification.VS2_A))
                if (ageGroup == AgeGroup.Senior 
                    && t.GetRefereeQualification() < RefereeQualification.VS2_A 
                    && t.minimumAgeGroup < AgeGroup.Senior)
                {
                    return false;
                }
            }

            //check ref qualification for ref tasks
            if (t.type == TaskType.Referee && !IsQualifiedReferee(t.GetRefereeQualification()))
            {
                return false;
            }

            return isAgeGroupQualified(t);
        }

        private bool isAgeGroupQualified(Task t)
        {
            //seniors dont do minis
            if(t.type == TaskType.Referee && ageGroup >= AgeGroup.Senior && t.minimumAgeGroup == AgeGroup.Mini)
            {
                return false;
            }

            //recreatives only do bar
            if(t.type != TaskType.BarKeeper && ageGroup == AgeGroup.Recreative)
            {
                return false;
            }

            //kids with vs2+
            if (ageGroup < AgeGroup.Senior && refereeQualification > RefereeQualification.VS1)
            {
                if (t.type == TaskType.Referee)
                {
                    //16+ can ref 3e class and below
                    if(dateOfBirth.AddYears(16) < t.startTime)
                    {
                        return t.GetRefereeQualification() <= RefereeQualification.VS1;
                    }

                    return ageGroup >= t.minimumAgeGroup;
                }
            }

            //is at least 2 age groups higher
            return (ageGroup == AgeGroup.Senior || ageGroup > 1 + t.minimumAgeGroup);
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
                return timeCost + tasksOnDate * EXTRA_COST_SAME_DAY_TASK;
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
                return timeCost + (tasksOnDate + 1) * EXTRA_COST_SAME_DAY_TASK;
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
                return timeCost + EXTRA_COST_SAME_DAY_TASK;
            }

            return timeCost;

        }

        public double getScoreLossByDifferentBuddy(Task task)
        {
            double currentScoreTask = getCostCurrentTask(task);
            double newScoreTask = currentScoreTask / SAME_TEAM_BARSHIFT_MULTIPLIER;

            return newScoreTask - currentScoreTask;
        }

        public int getMaxTasks()
        {
            if(ageGroup == AgeGroup.Recreative)
            {
                if (tasks.Count > 0)
                {
                    if (tasks[0].presetTask)
                    {
                        return tasks.Count;
                    }
                }

                return 1;
            }

            return int.MaxValue;
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

            double waitTimeBonus = 0;
            if(minWaitTime != double.MaxValue)
            {
                waitTimeBonus = Math.Min(1, minWaitTime / 4);
            }

            //no match on this day
            double nonMatchDayBonus = 0;
            if (teams[0].allowSchedulingOnNonMatchDay)
            {
                nonMatchDayBonus = NON_MATCH_DAY_BONUS;
            }

            double timeCost = duration + waitTimeBonus + nonMatchDayBonus; ;
            
            if (ageGroup == AgeGroup.Recreative && t.type == TaskType.ScoreKeeping) {
                timeCost *= 1.1;
            }

            return timeCost;
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

        public string ShortTeamName()
        {
            string prefix = "Taurus ";

            if (teamNames[0].Length <= prefix.Length) return teamNames[0];

            return teamNames[0].Substring(teamNames[0].IndexOf(prefix) + prefix.Length);
        }

        public bool IsQualifiedReferee(RefereeQualification rq)
        {
            return refereeQualification >= rq;
        }

        internal bool isMoreQualified(Player alternativePlayer, Task task)
        {
            return refereeQualification > alternativePlayer.refereeQualification;
        }

        internal bool isLessQualified(Player alternativePlayer, Task task)
        {
            return refereeQualification < alternativePlayer.refereeQualification;
        }
    }
}
