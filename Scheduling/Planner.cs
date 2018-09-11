using System;
using System.Collections.Generic;
using System.Linq;
using static Scheduling.Qualifications;

namespace Scheduling
{
    class Planner
    {
        List<Match> matches;
        public List<Task> tasks = new List<Task>();
        List<Player> players;
        Dictionary<string, Team> teams = new Dictionary<string, Team>();
        
        public Planner(List<Match> matchesHolder, List<Player> playersHolder, List<Team> teamsHolder, List<BarShift> barShifts, List<DateException> dateExceptions)
        {
            this.players = playersHolder;
            this.matches = matchesHolder;

            //create team lookup
            foreach (Team t in teamsHolder)
            {
                teams.Add(t.name, t);
            }

            foreach(DateException de in dateExceptions)
            {
                teams[de.teamName].addExceptionDate(de.date);
            }

            foreach (Player p in players)
            {
                foreach (var team in p.teamNames)
                {
                    p.addTeam(teams[team]);
                }
            }

            //set team objects
            foreach (Match m in matches)
            {
                m.SetTeam(teams[m.teamName]);
                m.team.addMatch(m);

                //add referee task
                if (m.requiresReferee())
                {
                    AgeGroup minimumAgeGroup = textToAgeGroupRef(m.team.name.ToLower());
                    
                    Task t;
                    if (m.refName.Length > 0)
                    {
                        var volunteerPlayer = findPlayer(m.refName);

                        if(volunteerPlayer != null)
                        {
                            m.refName = "";

                            t = new Task(m.team.name, TaskType.Referee, m.GetRefereeStartTime(), m.GetEndTime(), 0, m.team.minimumRefereeQualification, minimumAgeGroup, true);
                            m.AddTask(t);
                            volunteerPlayer.addTask(t);
                        } else
                        {
                            Console.Out.WriteLine("Volunteer ref not found as player: " + m.refName);
                        }
                        
                    }
                    else
                    {
                        t = new Task(m.team.name, TaskType.Referee, m.GetRefereeStartTime(), m.GetEndTime(), 0, m.team.minimumRefereeQualification, minimumAgeGroup);
                        tasks.Add(t);
                        m.AddTask(t);
                    }
                }

                for (int i = 0; i < m.flagsRequired(); i++)
                {
                    Task t = new Task(m.team.name, TaskType.Linesman, m.GetRefereeStartTime(), m.GetEndTime(), 16, Qualifications.RefereeQualification.None, AgeGroup.Senior);
                    tasks.Add(t);
                    m.AddTask(t);
                }
                for (int i = 0; i < m.additionalsRequired(); i++)
                {
                    AgeGroup minimumAgeGroup = m.team.minimumRefereeQualification == RefereeQualification.National ? AgeGroup.Senior : AgeGroup.Mini;

                    Task t = new Task(m.team.name, TaskType.ScoreKeeping, m.GetRefereeStartTime(), m.GetEndTime(), 0, Qualifications.RefereeQualification.None, minimumAgeGroup);
                    tasks.Add(t);
                    m.AddTask(t);
                }

            }


            foreach(BarShift bs in barShifts)
            {
                Task prevTask = null;
                for(int i = 0; i < bs.personel.Length; i++)
                {
                    string playerName = bs.personel[i];

                    if (playerName.Length < 1)
                    {
                        var shiftTask = new Task("", TaskType.BarKeeper, bs.startTime, bs.endTime, 16, Qualifications.RefereeQualification.None, AgeGroup.Mini);
                        tasks.Add(shiftTask);

                        if(prevTask == null)
                        {
                            prevTask = shiftTask;
                        } else
                        {
                            prevTask.SetLinkedTask(shiftTask);
                            shiftTask.SetLinkedTask(prevTask);
                        }
                    } else
                    {
                        var volunteerPlayer = findPlayer(playerName);

                        if(volunteerPlayer != null)
                        {
                            bs.personel[i] = "";

                            var shiftTask = new Task("", TaskType.BarKeeper, bs.startTime, bs.endTime, 16, Qualifications.RefereeQualification.None, AgeGroup.Mini, true);
                            tasks.Add(shiftTask);
                            volunteerPlayer.addTask(shiftTask);

                            if (prevTask == null)
                            {
                                prevTask = shiftTask;
                            }
                            else
                            {
                                prevTask.SetLinkedTask(shiftTask);
                                shiftTask.SetLinkedTask(prevTask);
                            }
                        } else
                        {
                            Console.Out.WriteLine("Volunteer  not found as player: " + playerName);
                        }
                    }
                }
            }

            tasks = tasks.OrderByDescending(t => t.GetRefereeQualification()).ThenByDescending(t => t.getAgeQualification()).ToList();

        }

        private Player findPlayer(string playerName)
        {
            var volunteerPlayer = players.Find(p => p.name == playerName);

            if(volunteerPlayer == null)
            {
                var nameParts = playerName.Trim().Split(' ');
                if(nameParts.Length > 1) {
                    string lastName = nameParts[nameParts.Length - 1];
                    lastName = lastName.First().ToString().ToUpper() + lastName.Substring(1);
                    string firstName = nameParts.Take(nameParts.Length - 1).Aggregate("", (a, b) => a + " " + b.First().ToString().ToUpper() + b.Substring(1)).Trim();
                    string tempName = lastName + " " + firstName;

                    volunteerPlayer = players.Find(p => p.name == tempName);
                }
            }

            return volunteerPlayer;
        }

        public void fillBarShifts(List<BarShift> shifts)
        {
            foreach(BarShift bs in shifts)
            {
                int expectedTaskCount = bs.personel.Where(p => p.Length < 1).ToList().Count;

                List<Task> relevantShifts = tasks.Where(t => t.type == TaskType.BarKeeper && t.startTime == bs.startTime).ToList();

                if(relevantShifts.Count != expectedTaskCount)
                {
                    throw new Exception("Unexpected task cout: " + relevantShifts.Count + " instead of " + expectedTaskCount);
                }

                for(int i = 0; i < relevantShifts.Count; i++)
                {
                    var p = relevantShifts.ElementAt(i).person;
                    bs.personel[bs.personel.Length - i - 1] = p.ShortTeamName() + ": " + p.name;
                }
            }
        }

        //do stuff
        public void generateSchema()
        {
            generateInitialSchema();

            double sos = reportScore("initial: ");
            
            for (int i = 0; i < 200; i++)
            {
                searchTask();
                twoOpt();
                double newSos = reportScore("" + i + ": ");

                if (sos == newSos)
                {
                    break;
                }
                sos = newSos;
            }
        }



        private void twoOpt()
        {
            //from player
            for (int i = 0; i < players.Count - 1; i++)
            {
                for (int j = i + 1; j < players.Count; j++)
                {
                    Player p1 = players[i];
                    Player p2 = players[j];

                    double currentScoreP1 = p1.getCurrentCost();
                    double currentScoreP2 = p2.getCurrentCost();

                    //p1 trade task
                    foreach (Task task1 in p1.tasks)
                    {
                        if (task1.presetTask) continue;
                        if (!p2.isQualified(task1)) continue;
                        if (!p2.canPerformTaskOnDay(task1.startTime)) continue;
                        if (p2.hasMatchOnTime(task1.startTime, task1.endTime)) continue;

                        double p1Task1Cost = p1.getGainRemoveTask(task1);
                        double p2Task1Cost = p2.getCostNewTask(task1);

                        //p2 trade task
                        foreach (Task task2 in p2.tasks)
                        {
                            if (task2.presetTask) continue;
                            if (!p1.isQualified(task2)) continue;
                            if (!p1.canPerformTaskOnDay(task2.startTime)) continue;
                            if (p1.hasMatchOnTime(task2.startTime, task2.endTime)) continue;
                            
                            if (p1.hasOtherTaskOnTime(task1, task2.startTime, task2.endTime)) continue;
                            if (p2.hasOtherTaskOnTime(task2, task1.startTime, task1.endTime)) continue;



                            double p1Task2Cost = p1.getCostNewTask(task2);
                            double p2Task2Cost = p2.getGainRemoveTask(task2);

                            double newScoreP1 = currentScoreP1 - p1Task1Cost + p1Task2Cost;
                            double newScoreP2 = currentScoreP2 - p2Task2Cost + p2Task1Cost;

                            //after < before
                            if ((Math.Pow(newScoreP1, 2) + Math.Pow(newScoreP2, 2)) < (Math.Pow(currentScoreP1, 2) + Math.Pow(currentScoreP2, 2)))
                            {
                                //Console.Out.WriteLine("Switching " + task1 + " <> " + task2 + "\nbetween: " + p1 + "" + p2);
                                
                                //switch
                                p1.removeTask(task1);
                                p2.removeTask(task2);
                                
                                p1.addTask(task2);
                                p2.addTask(task1);

                                //Console.Out.WriteLine("Before: " + currentScoreP1 + "  " + currentScoreP2);
                                //Console.Out.WriteLine("After : " + p1.getCurrentCost() + "  " + p2.getCurrentCost());

                                return;
                            }

                        }


                    }
                }
            }
        }


        public double reportScore(string prefix)
        {
            double meanScore = players.Average(p => p.getCurrentCost());
            double stdDev = players.Select(p => Math.Pow(p.getCurrentCost() - meanScore, 2)).Sum();
            double sos = players.Select(p => Math.Pow(p.getCurrentCost(), 2)).Sum();

            Console.Out.WriteLine(prefix + "mean: " + meanScore + " stdDev: " + stdDev + " sos: " + sos);

            return sos;
        }

        public void searchTask()
        {
            //from player
            foreach (Player p in players)
            {

                double currentLargestImprovment = double.MinValue;
                Player alternativePlayer = null;
                Task bestMoveSuggestion = null;

                //the players task that is to be given away
                foreach (Task task in p.tasks)
                {
                    if (task.presetTask) continue;

                    //current player cost - new player cost
                    double scoreGainByRemoval = Math.Pow(p.getCurrentCost(), 2) - Math.Pow(p.getCurrentCost() - p.getGainRemoveTask(task), 2);


                    foreach (Player playerUnderConsideration in players)
                    {
                        if (p == playerUnderConsideration) continue;
                        if (!playerUnderConsideration.isQualified(task)) continue;
                        if (playerUnderConsideration.isBusyOnTime(task.startTime, task.endTime)) continue;
                        if (!playerUnderConsideration.canPerformTaskOnDay(task.startTime.Date)) continue;

                        //new cost - current cost
                        double scoreCost = Math.Pow(playerUnderConsideration.getCurrentCost() + playerUnderConsideration.getCostNewTask(task), 2) - Math.Pow(playerUnderConsideration.getCurrentCost(), 2);
                        double score = scoreGainByRemoval - scoreCost;

                        if (score > currentLargestImprovment)// || (score == currentLargestImprovment && playerUnderConsideration.isLessQualified(alternativePlayer, task) ))
                        {
                            //Console.Out.WriteLine("New largest improvement: " + score + " = " + scoreGainByRemoval + " - " + scoreCost);
                            //Console.Out.WriteLine("scoreCost = (" + playerUnderConsideration.getCurrentCost() + " + " + playerUnderConsideration.getCostNewTask(task) + ")^2 - " + playerUnderConsideration.getCurrentCost());

                            currentLargestImprovment = score;
                            alternativePlayer = playerUnderConsideration;
                            bestMoveSuggestion = task;
                        }
                    }
                }

                if (bestMoveSuggestion != null && currentLargestImprovment >= 0)
                {
                    //Console.Out.WriteLine("Switching " + bestMoveSuggestion + " with score: " + currentLargestImprovment);
                    //Console.Out.WriteLine("From " + p);
                    //Console.Out.WriteLine("To " + alternativePlayer);

                    //double costBeforeP1 = p.getCurrentCost();
                    //double costBeforeP2 = alternativePlayer.getCurrentCost();

                    p.removeTask(bestMoveSuggestion);
                    alternativePlayer.addTask(bestMoveSuggestion);

                    //double costAfterP1 = p.getCurrentCost();
                    //double costAfterP2 = alternativePlayer.getCurrentCost();

                    //if(Math.Pow(costAfterP1, 2) + Math.Pow(costAfterP2, 2) > Math.Pow(costBeforeP1, 2) + Math.Pow(costBeforeP2, 2))
                    //{
                    //    Console.Out.WriteLine("Before: " + costBeforeP1 + " + " + costBeforeP2);
                    //    Console.Out.WriteLine("After: " + costAfterP1 + " + " + costAfterP2);

                    //    Console.Out.WriteLine("oops");
                    //}



                }

            }
        }

        public void generateInitialSchema()
        {
            foreach(Task t in tasks)
            {
                if (t.person != null) continue;

                double minCost = double.MaxValue;
                Player minCostPlayer = null;

                foreach (Player p in players)
                {
                    if (!p.hasTaskOnTime(t.startTime, t.endTime) && !p.hasMatchOnTime(t.startTime, t.endTime) && p.canPerformTaskOnDay(t.startTime) /*&& !p.hasTaskOnDate(t.startTime.Date)*/)
                    {
                        double currentCost = p.getCostNewTask(t);

                        if (currentCost >= 0 && currentCost < minCost)
                        {
                            minCost = currentCost;
                            minCostPlayer = p;
                        }
                    }
                }

                if (minCostPlayer == null)
                {
                    Console.Out.WriteLine("ERROR, no available player found for: " + t);
                }
                else
                {
                    //Console.Out.WriteLine("Filled task " + t + " with " + minCostPlayer.name + " with cost " + minCost);

                    minCostPlayer.addTask(t);
                    t.person = minCostPlayer;
                }
            }
        }

    }
}