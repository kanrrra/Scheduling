using System;
using System.Collections.Generic;
using System.Linq;

namespace Scheduling
{
    class Planner
    {
        List<Match> matches;
        List<Task> tasks = new List<Task>();
        List<Player> players;
        Dictionary<string, Team> teams = new Dictionary<string, Team>();

        public Planner(List<Match> matchesHolder, List<Player> playersHolder, List<Team> teamsHolder, List<BarShift> barShifts)
        {
            this.players = playersHolder;
            this.matches = matchesHolder;

            //create team lookup
            foreach (Team t in teamsHolder)
            {
                teams.Add(t.name, t);
            }



            //set team objects
            foreach (Match m in matches)
            {
                m.team = teams[m.teamName];
                m.team.addMatch(m);

                //add referee task
                if (m.requiresReferee())
                {
                    tasks.Add(new Task(m.team.name, TaskType.Referee, m.startTime, m.startTime.AddHours(2), Qualifications.AgeQualification.Adult, m.team.minimumRefereeQualification));
                }

                for (int i = 0; i < m.flagsRequired(); i++)
                {
                    tasks.Add(new Task(m.team.name, TaskType.Linesman, m.startTime, m.startTime.AddHours(2), Qualifications.AgeQualification.Adult, Qualifications.RefereeQualification.None));
                }
                for (int i = 0; i < m.additionalsRequired(); i++)
                {
                    tasks.Add(new Task(m.team.name, TaskType.ScoreKeeping, m.startTime, m.startTime.AddHours(2), Qualifications.AgeQualification.None, Qualifications.RefereeQualification.None));
                }

            }

            foreach(Player p in players)
            {
                p.setTeam(teams[p.teamName]);
            }

            foreach(BarShift bs in barShifts)
            {
                tasks.Add(new Task("", TaskType.BarKeeper, bs.startTime, bs.endTime, Qualifications.AgeQualification.Adult, Qualifications.RefereeQualification.None));
            }

            tasks = tasks.OrderByDescending(t => t.GetRefereeQualification()).ThenByDescending(t => t.getAgeQualification()).ToList();

        }

        //do stuff
        public void generateSchema()
        {
            generateInitialSchema();

            double sos = reportScore();
            
            for (int i = 0; i < 25; i++)
            {
                searchTask();
                twoOpt();
                double newSos = reportScore();

                if (sos == newSos)
                {
                    break;
                }
                else
                {
                    sos = newSos;
                }
            }

            reportScore();

            Console.Out.WriteLine("=====================================================");


            players = players.OrderBy(p => p.teamName).ThenByDescending(p => p.getCurrentCost()).ToList();

            string teamname = "";
            foreach (Player p in players)
            {
                if(p.teamName != teamname)
                {
                    teamname = p.teamName;
                    Console.Out.WriteLine("==================");
                    Console.Out.WriteLine(teamname);
                }

                Console.Out.WriteLine(p.name);
                foreach(Task t in p.tasks)
                {
                    Console.Out.WriteLine(t);
                }
                Console.Out.WriteLine("");
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
                        if (!p2.isQualified(task1)) continue;
                        if (p2.hasMatchOnTime(task1.startTime, task1.endTime)) continue;

                        double p1Task1Cost = p1.getCost(task1);
                        double p2Task1Cost = p2.getCost(task1);

                        //p2 trade task
                        foreach (Task task2 in p2.tasks)
                        {
                            if (!p1.isQualified(task2)) continue;
                            if (p1.hasMatchOnTime(task2.startTime, task2.endTime)) continue;
                            
                            if (p1.hasOtherTaskOnTime(task1, task2.startTime, task2.endTime)) continue;
                            if (p2.hasOtherTaskOnTime(task2, task1.startTime, task1.endTime)) continue;



                            double p1Task2Cost = p1.getCost(task2);
                            double p2Task2Cost = p2.getCost(task2);

                            double newScoreP1 = currentScoreP1 - p1Task1Cost + p1Task2Cost;
                            double newScoreP2 = currentScoreP2 - p2Task2Cost + p2Task1Cost;

                            

                            if (Math.Pow(newScoreP1, 2) + Math.Pow(newScoreP2, 2) < Math.Pow(currentScoreP1, 2) + Math.Pow(currentScoreP2, 2))
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


        public double reportScore()
        {
            double meanScore = players.Average(p => p.getCurrentCost());
            double stdDev = players.Select(p => Math.Pow(p.getCurrentCost() - meanScore, 2)).Sum();
            double sos = players.Select(p => Math.Pow(p.getCurrentCost(), 2)).Sum();

            Console.Out.WriteLine("mean: " + meanScore + " stdDev: " + stdDev + " sos: " + sos);

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
                    double scoreImprovment = Math.Pow(p.getCurrentCost(), 2) - Math.Pow(p.getCurrentCost() - p.getCost(task), 2);

                    foreach (Player playerUnderConsideration in players)
                    {
                        if (p == playerUnderConsideration) continue;
                        if (!playerUnderConsideration.isQualified(task)) continue;
                        if (playerUnderConsideration.isBusyOnTime(task.startTime, task.endTime)) continue;

                        double newCost = playerUnderConsideration.getCost(task);
                        if (newCost < 0)
                        {
                            continue;
                        }

                        //new cost - current cost
                        double scoreCost = Math.Pow(playerUnderConsideration.getCurrentCost() + playerUnderConsideration.getCost(task), 2) - Math.Pow(playerUnderConsideration.getCurrentCost(), 2);
                        double score = scoreImprovment - scoreCost;

                        if (score > currentLargestImprovment || (score == currentLargestImprovment && playerUnderConsideration.isMoreQualified(alternativePlayer, task) ))
                        {
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


                    p.removeTask(bestMoveSuggestion);
                    alternativePlayer.addTask(bestMoveSuggestion);

                }

            }
        }

        public void generateInitialSchema()
        {
            foreach(Task t in tasks)
            {
                double minCost = double.MaxValue;
                Player minCostPlayer = null;

                foreach (Player p in players)
                {
                    if (!p.hasTaskOnDate(t.startTime))
                    {
                        double currentCost = p.getCost(t);
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