using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduling
{
    class Program
    {
        static string matchPath = "C:/data/matches.csv";
        static string playersPath = "C:/data/players.csv";
        static string teamsPath = "C:/data/teams.csv";
        static string barPath = "C:/data/bar.csv";

        static void Main(string[] args)
        {
            Reader r = new Reader();

            List<Match> matches = r.readProgram(matchPath);
            List<Player> players = r.readPlayers(playersPath);
            List<Team> teams = r.readTeams(teamsPath);
            List<BarShift> bar = r.readBarShifts(barPath);
            List<DateException> dateExceptions = r.readExceptionsFromProgram(matchPath);

            Planner planner = new Planner(matches, players, teams, bar, dateExceptions);

            /*
            foreach (Team t in teams){
                Console.Out.WriteLine(t);

                Console.Out.WriteLine("Matches: ");
                foreach(Match m in t.matches)
                {
                    Console.Out.WriteLine("\t" + m);
                }
                Console.Out.WriteLine("");

                Console.Out.WriteLine("Days busy: ");
                foreach(DateTime dt in t.unavailableDates)
                {
                    Console.Out.WriteLine("\t" + dt);
                }
                Console.Out.WriteLine("");
            }
            */


            planner.generateSchema();
            var tasks = planner.tasks.OrderBy(t => t.startTime).ThenBy(t => t.Note).ToList();

            //write bar schedule to csv
            planner.fillBarShifts(bar);


            //Console.Out.WriteLine("=====================================================");
            Console.Out.WriteLine("highest cost: " + players.Max(p => p.getCurrentCost()));

            Dictionary<string, int[]> summary = new Dictionary<string, int[]>();
            string teamname = "";
            foreach (Player p in players)
            {
                if (p.teamNames[0] != teamname)
                {
                    teamname = p.teamNames[0];
                    Console.Out.WriteLine("==================");
                    Console.Out.WriteLine(teamname);
                }

                if (!summary.ContainsKey(teamname))
                {
                    summary[teamname] = new int[] { 0, 0, 0, 0 };
                }

                summary[teamname][0]++;
                summary[teamname][1] += p.tasks.Count;
                summary[teamname][2] += p.IsQualifiedReferee(Qualifications.RefereeQualification.VS1) ? 1 : 0;
                summary[teamname][3] += p.Exemption ? 1 : 0;


                printPlayerTasks(p);
            }

            Console.Out.WriteLine("========================================================");

            foreach (var item in summary) {
                Console.Out.WriteLine($"Team {item.Key} has {item.Value[1] / (double)item.Value[0]:F1} tasks/player and {item.Value[2] / (double)item.Value[0]:F1} refs/player ({item.Value[0]} players, {item.Value[1]} tasks, {item.Value[2]} refs, {item.Value[3]} volunteer exemptions)");
            }



            Console.Out.WriteLine("==========================ALL===========================");


            DateTime day = tasks[0].startTime.Date;
            double dayCost = 0;
            foreach (Task t in tasks)
            {
                if (day != t.startTime.Date)
                {
                    Console.Out.WriteLine("Daycost: " + dayCost);
                    dayCost = 0;

                    day = t.startTime.Date;
                    Console.Out.WriteLine("==================\n");
                }

                var playersOnTask = players.Where(p => p.tasks.Contains(t)).ToList();
                if (playersOnTask.Count < 1)
                {
                    Console.Out.WriteLine("!!!!!!!!!!!!!!!!!!!!!!! Noone found for task: " + t);
                }
                else if (playersOnTask.Count == 1)
                {
                    Console.Out.WriteLine((playersOnTask[0].name + ": ").PadRight(25) + t + "\tcost: " + playersOnTask[0].getCostCurrentTask(t));
                    dayCost += playersOnTask[0].getCostCurrentTask(t);
                }
                else
                {
                    throw new Exception("Multiple people on single task???");
                }
            }
            Console.Out.WriteLine("Daycost: " + dayCost);


            Console.Out.WriteLine("===========================VS2==========================");
            foreach (Player p in players)
            {
                if (p.IsQualifiedReferee(Qualifications.RefereeQualification.VS2_A))
                {
                    printPlayerTasks(p);
                }
            }

            Console.Out.WriteLine("===========================VS1==========================");
            foreach (Player p in players)
            {
                if (p.IsQualifiedReferee(Qualifications.RefereeQualification.VS1) && !p.IsQualifiedReferee(Qualifications.RefereeQualification.VS2_A))
                {
                    printPlayerTasks(p);
                }
            }

            Console.Out.WriteLine("=========================TOP 10%============================");
            players = players.OrderByDescending(p => p.getCurrentCost()).ToList();

            foreach (Player p in players.Take(players.Count / 10))
            {
                printPlayerTasks(p);
            }

            Console.Out.WriteLine("=========================BOT 10%============================");
            var playersWithoutExemption = players.OrderBy(p => p.getCurrentCost()).Where(p => !p.Exemption).ToList();

            foreach (Player p in playersWithoutExemption.Take(playersWithoutExemption.Count / 10))
            {
                printPlayerTasks(p);
            }

            Console.Out.WriteLine("=========================NEW============================");
            foreach (Task t in tasks)
            {
                if(!t.presetTask)
                {
                    if(t.person == null)
                    {
                        Console.Out.WriteLine($"{"TODO!".PadRight(40)} {t}");
                    } else
                    {
                        Console.Out.WriteLine($"{($"{t.person.name} ({t.person.ShortTeamName()})").PadRight(40)} {t}");
                    }

                }
            }

            Console.Out.WriteLine("=====================WARNINGS===========================");
            foreach(Player p in players)
            {
                p.checkOverlappingTasks();
            }



            //to file
            string barSchedule = "Datum,Begin,Eind,Persoon 1,Persoon 2\n";
            var prevDate = bar[0]?.startTime.Date;
            foreach (BarShift bs in bar)
            {
                //new day
                if (bs.startTime.Date != prevDate)
                {
                    prevDate = bs.startTime.Date;
                    barSchedule += "\n";
                }

                barSchedule += bs + "\n";
            }
            System.IO.File.WriteAllText("bar schedule.csv", barSchedule);

            players = players.OrderBy(p => p.teamNames[0]).ThenByDescending(p => p.getCurrentCost()).ToList();

            string playerList = "";
            foreach (Player p in players)
            {
                playerList += p.ToCSV() + "\n";
            }
            System.IO.File.WriteAllText("players.csv", playerList);

            string schedule = "Datum, tijd, team thuis, team uit, scheidsrechter, teller, veld, zaal\n";
            foreach (Match m in matches)
            {
                schedule += m.ToCSV() + "\n";
            }
            System.IO.File.WriteAllText("schedule.csv", schedule);

            Console.Out.WriteLine($"Task count: {tasks.Count()}");
            Console.In.ReadLine();
        }


        static void printPlayerTasks(Player p)
        {
            Console.Out.WriteLine($"{p.name} ({p.ShortTeamName()}) Exemption: {p.Exemption} referee: {p.RefereeQualification} Cost: {p.getCurrentCost()}");
            p.tasks = p.tasks.OrderBy(t => t.startTime).ToList();

            int tasksOnSameDay = 0;
            DateTime previousTaskDate = new DateTime();
            foreach (Task t in p.tasks)
            {
                if (previousTaskDate == t.startTime.Date)
                {
                    tasksOnSameDay++;
                }

                var myMatches = p.getMatchOnDay(t.startTime).Where(m => m != null).ToList();
                string matchString = "";
                if (myMatches.Count > 0)
                {
                    matchString = myMatches.Select(m => m.ToString()).Aggregate((a, b) => a + "." + b);
                }
                Console.Out.WriteLine("Match: " + matchString.PadRight(50) + "\tTask: " + t + "\tcost: " + p.getCostCurrentTask(t).ToString("n2"));

                previousTaskDate = t.startTime.Date;
            }

            if (tasksOnSameDay > 0)
            {
                Console.Out.WriteLine("WARNING!! MULTIPLE TASKS ON SAME DAY: " + tasksOnSameDay);
            }

            Console.Out.WriteLine("");
        }
    }
}
