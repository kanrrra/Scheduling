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

            string teamname = "";
            foreach (Player p in players)
            {
                if(p.teamNames[0] != teamname)
                {
                    teamname = p.teamNames[0];
                    Console.Out.WriteLine("==================");
                    Console.Out.WriteLine(teamname);
                }

                printPlayerTasks(p);
            }

            Console.Out.WriteLine("=====================================================");


            DateTime day = tasks[0].startTime.Date;
            double dayCost = 0;
            foreach(Task t in tasks)
            {
                if(day != t.startTime.Date)
                {
                    Console.Out.WriteLine("Daycost: " + dayCost);
                    dayCost = 0;

                    day = t.startTime.Date;
                    Console.Out.WriteLine("==================\n");
                }

                var playersOnTask = players.Where(p => p.tasks.Contains(t)).ToList();
                if(playersOnTask.Count > 0)
                {
                    Console.Out.WriteLine((playersOnTask[0].name + ": ").PadRight(25) + t + "\tcost: " + playersOnTask[0].getCostCurrentTask(t));
                    dayCost += playersOnTask[0].getCostCurrentTask(t);
                } else
                {
                    Console.Out.WriteLine("!!!!!!!!!!!!!!!!!!!!!!! Noone found for task: " + t);
                }
            }
            Console.Out.WriteLine("Daycost: " + dayCost);


            Console.Out.WriteLine("=====================================================");
            foreach (Player p in players)
            {
                if (p.IsQualifiedReferee(Qualifications.RefereeQualification.VS2))
                {
                    printPlayerTasks(p);
                }
            }


            //to file
            string barSchedule = "";
            foreach(BarShift bs in bar)
            {
                barSchedule += bs + "\n";
            }
            System.IO.File.WriteAllText("bar schedule.csv", barSchedule);
            
            players = players.OrderBy(p => p.teamNames[0]).ThenByDescending(p => p.getCurrentCost()).ToList();

            string playerList = "";
            foreach(Player p in players)
            {
                playerList += p.ToCSV() + "\n";
            }
            System.IO.File.WriteAllText("players.csv", playerList);

            string schedule = "Datum, tijd, team, scheidsrechter, teller\n";
            foreach(Match m in matches)
            {
                schedule += m.ToCSV() + "\n";
            }
            System.IO.File.WriteAllText("schedule.csv", schedule);


            Console.In.ReadLine();
        }


        static void printPlayerTasks(Player p)
        {

            Console.Out.WriteLine(p.name + " " + p.getCurrentCost());
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
