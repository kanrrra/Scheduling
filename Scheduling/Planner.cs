using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic;
using static Scheduling.Qualifications;

namespace Scheduling
{
    class Planner
    {
        List<Match> matches;
        public List<Task> tasks = new List<Task>();
        List<Player> players;
        Dictionary<string, Team> teams = new Dictionary<string, Team>();

        HashSet<string> volunteers;

        public Planner(List<Match> matchesHolder, List<Player> playersHolder, List<Team> teamsHolder, List<BarShift> barShifts, List<DateException> dateExceptions, HashSet<string> volunteersHolder)
        {
            this.players = playersHolder;
            this.matches = matchesHolder;
            this.volunteers = volunteersHolder;

            //create team lookup
            foreach (Team t in teamsHolder)
            {
                teams.Add(t.name, t);
            }

            foreach (DateException de in dateExceptions)
            {
                if (!teams.ContainsKey(de.teamName))
                {
                    Console.Out.WriteLine($"Team {de.teamName} not specified!");
                    Console.In.ReadLine();
                    System.Environment.Exit(1);
                }
                teams[de.teamName].addUnavailableDate(de.date);
            }

            foreach (Player p in players)
            {
                foreach (var team in p.teamNames)
                {
                    if (!teams.ContainsKey(team))
                    {
                        Console.Out.WriteLine($"Team {team} doesnt exist (player {p.name})! Exiting...");
                        Console.In.ReadLine();
                        System.Environment.Exit(1);
                    }
                    p.addTeam(teams[team]);
                }
            }

            bool anyVolunteerNotFound = false;

            //set team objects
            foreach (Match m in matches)
            {
                m.SetTeam(teams[m.teamName]);
                m.team.addMatch(m);

                // for duplicate matches (Taurus vs Taurus)
                if (!m.GenerateTasks())
                {
                    continue;
                }

                //add referee task
                if (m.requiresReferee())
                {
                    AgeGroup minimumAgeGroup = textToAgeGroupRef(m.team.name.ToLower());

                    if (minimumAgeGroup < AgeGroup.Senior)
                    {
                        // youth at least 2 levels higher
                        minimumAgeGroup = (AgeGroup)Math.Min((int)AgeGroup.Senior, (int)minimumAgeGroup + 2);
                    }

                    Task t;
                    if (m.refName.Length > 0)
                    {
                        var volunteerPlayer = findPlayer(m.refName);

                        if (volunteerPlayer != null)
                        {
                            m.refName = "";

                            t = new Task(m.team.name, TaskType.Referee, m.GetRefereeStartTime(), m.GetEndTime(), 0, m.team.minimumRefereeQualification, minimumAgeGroup, true);
                            m.AddTask(t);
                            tasks.Add(t);

                            volunteerPlayer.addTask(t);
                        }
                        else if (!findVolunteer(m.refName))
                        {
                            Console.Out.WriteLine($"Volunteer ref not found as player: {m.refName} \t at {m.GetProgramStartTime()}");
                            anyVolunteerNotFound = true;

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
                    //national teams need adults
                    AgeGroup minimumAgeGroup = m.team.minimumRefereeQualification == RefereeQualification.National ? AgeGroup.MA : AgeGroup.Mini;

                    Task t;
                    if (i == 0 && m.scoreName.Length > 0)
                    {
                        var volunteerPlayer = findPlayer(m.scoreName);

                        if (volunteerPlayer != null)
                        {
                            m.scoreName = "";

                            t = new Task(m.team.name, TaskType.ScoreKeeping, m.GetRefereeStartTime(), m.GetEndTime(), 0, Qualifications.RefereeQualification.None, minimumAgeGroup, true);
                            m.AddTask(t);
                            tasks.Add(t);

                            volunteerPlayer.addTask(t);
                        }
                        else if (!findVolunteer(m.scoreName))
                        {
                            Console.Out.WriteLine($"Volunteer extra not found as player: {m.scoreName} \t at {m.GetProgramStartTime()}");
                            anyVolunteerNotFound = true;
                        }

                    }
                    else
                    {
                        t = new Task(m.team.name, TaskType.ScoreKeeping, m.GetRefereeStartTime(), m.GetEndTime(), 0, Qualifications.RefereeQualification.None, minimumAgeGroup);

                        tasks.Add(t);
                        m.AddTask(t);
                    }

                }

            }


            foreach (BarShift bs in barShifts)
            {
                Task prevTask = null;
                for (int i = 0; i < bs.personel.Length; i++)
                {
                    string playerName = bs.personel[i];

                    //no preset person
                    if (playerName.Length < 1)
                    {
                        var shiftTask = new Task("", TaskType.BarKeeper, bs.startTime, bs.endTime, 16, Qualifications.RefereeQualification.None, AgeGroup.Mini);
                        tasks.Add(shiftTask);

                        if (prevTask == null)
                        {
                            prevTask = shiftTask;
                        }
                        else
                        {
                            prevTask.SetLinkedTask(shiftTask);
                            shiftTask.SetLinkedTask(prevTask);
                        }
                    }
                    else
                    {
                        var volunteerPlayer = findPlayer(playerName);

                        if (volunteerPlayer != null)
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
                        }
                        else if (!findVolunteer(playerName))
                        {
                            Console.Out.WriteLine($"Bar volunteer not found as player: {playerName}");
                            anyVolunteerNotFound = true;
                        }
                    }
                }
            }

            if (anyVolunteerNotFound)
            {
                System.Environment.Exit(1);
            }

            tasks = tasks.OrderByDescending(t => t.GetRefereeQualification()).ThenByDescending(t => t.getAgeQualification()).ToList();
        }

        private Player findPlayer(string playerName)
        {
            var volunteerPlayer = players.Find(p => p.name.ToLower() == playerName.ToLower());

            if (volunteerPlayer == null)
            {
                volunteerPlayer = players.Find(p => p.ApproxNameMatch(playerName.ToLower()));
            }

            return volunteerPlayer;
        }

        private bool findVolunteer(string name)
        {

            if (volunteers.Contains(name.Split(' ')[0]))
            {
                return true;
            }

            return volunteers.Contains(name);
        }

        /**
         * Fill the list of bar shifts with information from tasks
         * 
         * 
         */
        public void fillBarShifts(List<BarShift> shifts)
        {
            foreach (BarShift bs in shifts)
            {
                int expectedTaskCount = bs.personel.Where(p => p.Length < 1).ToList().Count;

                List<Task> relevantShifts = tasks.Where(t => t.type == TaskType.BarKeeper && t.startTime == bs.startTime && t.endTime == bs.endTime).ToList();

                if (relevantShifts.Count != expectedTaskCount)
                {
                    throw new Exception("Unexpected task cout: " + relevantShifts.Count + " instead of " + expectedTaskCount);
                }

                for (int i = 0; i < relevantShifts.Count; i++)
                {
                    var p = relevantShifts.ElementAt(i).person;
                    int emptyIdx = Array.FindIndex(bs.personel, person => person.Trim().Length < 1);
                    bs.personel[emptyIdx] = $"{p.name} ({p.ShortTeamName()})";
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

                if (getSos() - sos > 0.001)
                {
                    throw new Exception("searchTask, whatcha doing fucking up my score");
                }

                twoOpt();
                double newSos = reportScore("" + i + ": ");

                if (newSos - sos > 0.001)
                {
                    throw new Exception("twoOpt, whatcha doing fucking up my score");
                }

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
                        if (p2.hasMatchOnTime(task1.schedulingStartTime, task1.endTime)) continue;

                        double p1Task1Cost = p1.getGainRemoveTask(task1);
                        double p2Task1Cost = p2.getCostNewTask(task1);

                        //p2 trade task
                        foreach (Task task2 in p2.tasks)
                        {
                            if (task2.presetTask) continue;
                            if (!p1.isQualified(task2)) continue;
                            if (!p1.canPerformTaskOnDay(task2.startTime)) continue;
                            if (p1.hasMatchOnTime(task2.schedulingStartTime, task2.endTime)) continue;

                            if (p1.hasOtherTaskOnTime(task1, task2.startTime, task2.endTime)) continue;
                            if (p2.hasOtherTaskOnTime(task2, task1.startTime, task1.endTime)) continue;

                            // if tasks are in a different year then swapping may result in too many tasks in the half year
                            //if (task1.startTime.Year != task2.startTime.Year)
                            //{
                            //    if (p1.getCurrentHalfSeasonTaskCount(task2.startTime.Year) >= p1.getMaxAllowedTasks(task2.startTime)) continue;
                            //    if (p2.getCurrentHalfSeasonTaskCount(task1.startTime.Year) >= p2.getMaxAllowedTasks(task1.startTime)) continue;
                            //}
                            //TODO(Geerten) this doesnt matter today but it should be fixed before next season


                            double p1Task2Cost = p1.getCostNewTask(task2);
                            double p2Task2Cost = p2.getGainRemoveTask(task2);

                            double newScoreP1 = currentScoreP1 - p1Task1Cost + p1Task2Cost;
                            double newScoreP2 = currentScoreP2 - p2Task2Cost + p2Task1Cost;

                            //after < before
                            if ((Math.Pow(newScoreP1, 2) + Math.Pow(newScoreP2, 2)) < (Math.Pow(currentScoreP1, 2) + Math.Pow(currentScoreP2, 2)))
                            {
                                //Console.Out.WriteLine("Switching " + task1 + " <> " + task2 + "\nbetween: " + p1 + "" + p2);

                                double uglyTotalBefore = Math.Pow(p1.getCurrentCost(), 2) + Math.Pow(p2.getCurrentCost(), 2);
                                if (task1.linkedTask != null)
                                {
                                    uglyTotalBefore += Math.Pow(task1.linkedTask.person.getCurrentCost(), 2);
                                }
                                if (task2.linkedTask != null)
                                {
                                    uglyTotalBefore += Math.Pow(task2.linkedTask.person.getCurrentCost(), 2);
                                }

                                //switch
                                p1.removeTask(task1);
                                p2.removeTask(task2);

                                p1.addTask(task2);
                                p2.addTask(task1);

                                //prev score win is not taking into account the lost score of the linked task player
                                double uglyTotalAfter = Math.Pow(p1.getCurrentCost(), 2) + Math.Pow(p2.getCurrentCost(), 2);
                                if (task1.linkedTask != null)
                                {
                                    uglyTotalAfter += Math.Pow(task1.linkedTask.person.getCurrentCost(), 2);
                                }
                                if (task2.linkedTask != null)
                                {
                                    uglyTotalAfter += Math.Pow(task2.linkedTask.person.getCurrentCost(), 2);
                                }

                                if (uglyTotalAfter > uglyTotalBefore)
                                {
                                    //revert
                                    p1.removeTask(task2);
                                    p2.removeTask(task1);

                                    p1.addTask(task1);
                                    p2.addTask(task2);
                                }


                                //Console.Out.WriteLine("Before: " + currentScoreP1 + "  " + currentScoreP2);
                                //Console.Out.WriteLine("After : " + p1.getCurrentCost() + "  " + p2.getCurrentCost());

                                return;
                            }

                        }


                    }
                }
            }
        }

        public double getSos()
        {
            /*foreach(Player p in players)
            {
                Console.Out.WriteLine(p.getCurrentCost());
            }*/


            return players.Select(p => Math.Pow(p.getCurrentCost(), 2)).Sum();
        }

        public double reportScore(string prefix)
        {
            double meanScore = players.Average(p => p.getCurrentCost());
            double stdDev = Math.Sqrt(players.Select(p => Math.Pow(p.getCurrentCost() - meanScore, 2)).Average());
            double sos = getSos();

            Console.Out.WriteLine(prefix + "mean: " + meanScore + " stdDev: " + stdDev + " sos: " + sos);

            return sos;
        }

        // move task from one player to another if it reduces the total cost
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

                    double barScoreLossByOtherBarkeeper = 0;
                    if (task.type == TaskType.BarKeeper)
                    {
                        Task linkedTask = task.GetLinkedTask();

                        //No linked task (meaning the other task is covered by a non player)
                        if (linkedTask != null)
                        {
                            Player otherBarPerson = linkedTask.person;

                            //same team
                            if (otherBarPerson.teams.Intersect(p.teams).Any())
                            {
                                barScoreLossByOtherBarkeeper = Math.Pow(otherBarPerson.getCurrentCost() + otherBarPerson.getScoreLossByDifferentBuddy(task.GetLinkedTask()), 2) - Math.Pow(otherBarPerson.getCurrentCost(), 2);
                            }
                        }
                    }
                    scoreGainByRemoval -= barScoreLossByOtherBarkeeper;

                    foreach (Player playerUnderConsideration in players)
                    {
                        if (p == playerUnderConsideration) continue;
                        if (!playerUnderConsideration.isQualified(task)) continue;
                        if (playerUnderConsideration.isBusyOnTime(task.schedulingStartTime, task.endTime)) continue;
                        if (!playerUnderConsideration.canPerformTaskOnDay(task.startTime.Date)) continue;

                        //cant fit an aditional task
                        if (playerUnderConsideration.getTaskCount() >= playerUnderConsideration.getMaxAllowedTasks(task.startTime)) continue;


                        //new cost - current cost
                        double scoreCost = Math.Pow(playerUnderConsideration.getCurrentCost() + playerUnderConsideration.getCostNewTask(task), 2) - Math.Pow(playerUnderConsideration.getCurrentCost(), 2);

                        if (task.type == TaskType.BarKeeper)
                        {
                            Task linkedTask = task.GetLinkedTask();

                            if (linkedTask != null)
                            {
                                Player otherBarPerson = linkedTask.person;
                                if (otherBarPerson.teams.Intersect(playerUnderConsideration.teams).Any())
                                {
                                    scoreCost -= barScoreLossByOtherBarkeeper;
                                }
                            }
                        }

                        double score = scoreGainByRemoval - scoreCost;

                        if (score > currentLargestImprovment)// || (score == currentLargestImprovment && playerUnderConsideration.isLessQualified(alternativePlayer, task) ))
                        {
                            //Console.Out.WriteLine("New largest improvement: " + score + " = " + scoreGainByRemoval + " - " + scoreCost);
                            //Console.Out.WriteLine("scoreCost = (" + playerUnderConsideration.getCurrentCost() + " + " + playerUnderConsideration.getCostNewTask(task) + ")^2 - " + playerUnderConsideration.getCurrentCost());
                            //Console.Out.WriteLine("barScoreLossByOtherBarkeeper: " + barScoreLossByOtherBarkeeper);

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

                    double costBeforeP1 = p.getCurrentCost();
                    double costBeforeP2 = alternativePlayer.getCurrentCost();

                    p.removeTask(bestMoveSuggestion);

                    alternativePlayer.addTask(bestMoveSuggestion);

                    //                    double costAfterP1 = p.getCurrentCost();
                    //                    double costAfterP2 = alternativePlayer.getCurrentCost();

                    //if (Math.Pow(costAfterP1, 2) + Math.Pow(costAfterP2, 2) > Math.Pow(costBeforeP1, 2) + Math.Pow(costBeforeP2, 2))
                    //{
                    //    Console.Out.WriteLine("Before: " + costBeforeP1 + " (" + Math.Pow(costBeforeP1, 2) + ") + " + costBeforeP2 + " (" + Math.Pow(costBeforeP2, 2) + "): " + (Math.Pow(costBeforeP1, 2) + Math.Pow(costBeforeP2, 2)));
                    //    Console.Out.WriteLine("After: " + costAfterP1 + " (" + Math.Pow(costAfterP1, 2) + ") + " + costAfterP2 + " (" + Math.Pow(costAfterP2, 2) + "): " + (Math.Pow(costAfterP1, 2) + Math.Pow(costAfterP2, 2)));
                    //
                    //    Console.Out.WriteLine("oops");
                    //}



                }

            }
        }

        public void generateInitialSchema()
        {
            foreach (Task t in tasks)
            {
                if (t.person != null) continue;

                double minCost = double.MaxValue;
                Player minCostPlayer = null;

                foreach (Player p in players)
                {
                    if (!p.isBusyOnTime(t.schedulingStartTime, t.endTime)
                            && p.canPerformTaskOnDay(t.startTime)
                            && p.getTaskCount() < p.getMaxAllowedTasks(t.startTime))
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
                    minCostPlayer.addTask(t);
                    t.person = minCostPlayer;
                }
            }
        }

    }
}