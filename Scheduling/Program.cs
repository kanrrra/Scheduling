using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Scheduling
{
    class Program
    {
        static string matchPath = "C:/data/matches.csv";
        static string playersPath = "C:/data/players.csv";
        static string teamsPath = "C:/data/teams.csv";
        static string barPath = "C:/data/bar.csv";
        static string volunteersPath = "C:/data/volunteers.csv";

        static void Main(string[] args)
        {
            Reader r = new Reader();

            List<Match> matches = r.readProgram(matchPath);
            List<Player> players = r.readPlayers(playersPath);
            List<Team> teams = r.readTeams(teamsPath);
            List<BarShift> bar = r.readBarShifts(barPath);
            List<DateException> dateExceptions = r.readExceptionsFromProgram(matchPath);
            HashSet<string> volunteers = r.readVolunteers(volunteersPath);

            Planner planner = new Planner(matches, players, teams, bar, dateExceptions, volunteers);


            //foreach (Team t in teams){
            //    Console.Out.WriteLine(t);

            //    Console.Out.WriteLine("Matches: ");
            //    foreach(Match m in t.matches)
            //    {
            //        Console.Out.WriteLine("\t" + m + "  " + m.opponent);
            //    }
            //    Console.Out.WriteLine("");

            //    //Console.Out.WriteLine("Days busy: ");
            //    //foreach(DateTime dt in t.unavailableDates)
            //    //{
            //    //    Console.Out.WriteLine("\t" + dt);
            //    //}
            //    Console.Out.WriteLine("");
            //}



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

            // Team  utilization
            foreach (var item in summary)
            {
                Console.Out.WriteLine($"Team {item.Key} has {item.Value[1] / (double)(item.Value[0] - item.Value[3]):F1} tasks/player and {item.Value[2] / (double)item.Value[0]:F1} refs/player ({item.Value[0]} players, {item.Value[1]} tasks, {item.Value[2]} refs, {item.Value[3]} volunteer exemptions)");
            }

            Console.Out.WriteLine("==========================ALL===========================");

            DateTime lastDay = tasks[0].startTime.Date;
            double dayCost = 0;
            foreach (Task t in tasks)
            {
                if (lastDay != t.startTime.Date)
                {
                    Console.Out.WriteLine("Daycost: " + dayCost);
                    dayCost = 0;

                    lastDay = t.startTime.Date;
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
                if (!t.presetTask)
                {
                    if (t.person == null)
                    {
                        Console.Out.WriteLine($"{"TODO!".PadRight(40)} {t}");
                    }
                    else
                    {
                        Console.Out.WriteLine($"{($"{t.person.name} ({t.person.ShortTeamName()})").PadRight(40)} {t}");
                    }

                }
            }

            Console.Out.WriteLine("=====================WARNINGS===========================");
            foreach (Player p in players)
            {
                p.checkOverlappingTasks();
            }

            Console.Out.WriteLine($"Task count: {tasks.Count()}");

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

            string schedule = "Datum, Tijd, Team thuis, Team uit, Scheidsrechter, Teller, Veld, Zaal\n";
            foreach (Match m in matches)
            {
                schedule += m.ToCSV() + "\n";
            }
            System.IO.File.WriteAllText("schedule.csv", schedule);

            // Publication friendly outputs
            matches = matches.OrderBy(m => m.GetProgramStartTime()).ThenBy(m => m.field).ToList();
            using (var workbook = new XLWorkbook())
            {
                addBarSheet(workbook, bar);

                int rowsPerPage = 35;
                var worksheet = workbook.Worksheets.Add("Taken");
                int column = 1;
                int row = 1;


                worksheet.Column(1).Style.NumberFormat.Format = "yyyy-mm-dd";
                worksheet.Column(2).Style.NumberFormat.Format = "HH:MM";

                DateTime lastDate = new DateTime();
                long lastTime = 0;

                for (int matchIdx = 0; matchIdx < matches.Count; matchIdx++)
                {
                    Match m = matches[matchIdx];
                    int rowOnPage = (row - 1) % rowsPerPage + 1;

                    // new day so check if the next section fits the remainder of the page
                    if (lastDate != m.GetProgramStartTime().Date)
                    {
                        if (matches.Count(match => match.GetProgramStartTime().Date == m.GetProgramStartTime().Date) > rowsPerPage - rowOnPage)
                        {
                            row += rowsPerPage - rowOnPage + 1;
                            rowOnPage = 1;
                        }
                    }

                    if (rowOnPage == 1)
                    {
                        addHeader(worksheet, row, column);
                        // row will be added by the 'new date' section
                    }


                    // Date
                    if (lastDate != m.GetProgramStartTime().Date)
                    {
                        row++;
                        lastDate = m.GetProgramStartTime().Date;
                        worksheet.Cell(row, column).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(row, column++).Value = m.GetProgramStartTime();

                        lastTime = 0;
                    }
                    else
                    {
                        column++;
                    }

                    // Time
                    if (lastTime != m.GetProgramStartTime().Ticks)
                    {
                        worksheet.Cell(row, column++).Value = m.GetProgramStartTime();
                        worksheet.Range(row, 2, row, 7).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        lastTime = m.GetProgramStartTime().Ticks;
                    }
                    else
                    {
                        column++;
                    }

                    worksheet.Cell(row, column++).Value = m.teamName;
                    worksheet.Cell(row, column++).Value = m.opponent;

                    var personel = m.GetProgramValues();
                    worksheet.Cell(row, column++).Value = personel[0];
                    worksheet.Cell(row, column++).Value = personel[1];
                    worksheet.Cell(row, column++).Value = m.field;


                    row++;
                    column = 1;
                }

                worksheet.Columns().AdjustToContents();


                var perTeamTasks = tasks.Where(t => t.person != null).OrderBy(t => t.person.teamNames[0]).ThenBy(t => t.schedulingStartTime).ToList();
                lastDay = perTeamTasks[0].startTime.Date;
                string lastTeam = "";
                foreach (Task t in perTeamTasks)
                {
                    if (lastDay != t.startTime.Date)
                    {
                        lastDay = t.startTime.Date;
                        row++;
                    }

                    if (lastTeam != t.person.teamNames[0])
                    {
                        row = 1;
                        column = 1;

                        lastTeam = t.person.teamNames[0];
                        worksheet.Columns().AdjustToContents();
                        worksheet = workbook.AddWorksheet(lastTeam);

                        worksheet.Cell(row, column++).Value = "Naam";
                        worksheet.Cell(row, column++).Value = "Datum";
                        worksheet.Cell(row, column++).Value = "Start tijd";
                        worksheet.Cell(row, column++).Value = "Eind tijd";
                        worksheet.Cell(row, column++).Value = "Taak";

                        worksheet.Range(1, 1, 1, column).Style.Font.Bold = true;

                        worksheet.Column(2).Style.NumberFormat.Format = "yyyy-mm-dd";
                        worksheet.Column(3).Style.NumberFormat.Format = "HH:MM";
                        worksheet.Column(4).Style.NumberFormat.Format = "HH:MM";

                        row++;
                        column = 1;
                    }

                    var playersOnTask = players.Where(p => p.tasks.Contains(t)).ToList();
                    if (playersOnTask.Count > 0)
                    {
                        worksheet.Cell(row, column++).Value = playersOnTask[0].name;
                        worksheet.Cell(row, column++).Value = t.startTime.Date;
                        worksheet.Cell(row, column++).Value = t.startTime.TimeOfDay;
                        worksheet.Cell(row, column++).Value = t.endTime.TimeOfDay;

                        string taskName;
                        switch (t.type)
                        {
                            case TaskType.Referee:
                                taskName = "Scheidsrechter";
                                break;
                            case TaskType.ScoreKeeping:
                                taskName = "Teller";
                                break;
                            case TaskType.BarKeeper:
                                taskName = "Bardienst";
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        worksheet.Cell(row, column++).Value = $"{taskName} {t.Note}";
                        column = 1;
                        row++;
                    }
                }

                workbook.SaveAs("Takenrooster 20xx-20xx xxxxx helft vx.xlsx");
            }
            Console.Out.WriteLine("Done!");
            Console.In.ReadLine();
        }


        static void addBarSheet(IXLWorkbook workbook, List<BarShift> bar)
        {
            int rowsPerPage = 48;

            int column = 1;
            int row = 1;
            var worksheet = workbook.AddWorksheet("Bar");

            worksheet.Column(2).Style.NumberFormat.Format = "yyyy-mm-dd";
            worksheet.Column(3).Style.NumberFormat.Format = "HH:MM";
            worksheet.Column(4).Style.NumberFormat.Format = "HH:MM";

            var lastDate = new DateTime();

            for (int barIdx = 0; barIdx < bar.Count; barIdx++)
            {
                BarShift bs = bar[barIdx];
                int rowOnPage = (row - 1) % rowsPerPage + 1;

                // new day so check if the next section fits the remainder of the page
                if (bs.startTime.Date != lastDate)
                {
                    if (bar.Count(bar => bar.startTime.Date == bs.startTime.Date) > rowsPerPage - rowOnPage)
                    {
                        row += rowsPerPage - rowOnPage + 1;
                        rowOnPage = 1;
                    }
                }


                if (rowOnPage == 1)
                {
                    addBarHeader(worksheet, row);
                    // row will be added by the 'new date' section
                }

                if (bs.startTime.Date != lastDate)
                {
                    row++;
                    lastDate = bs.startTime.Date;
                    worksheet.Cell(row, column++).Value = bs.startTime.ToShortDateString();
                }
                else
                {
                    column++;
                }

                worksheet.Cell(row, column++).Value = bs.startTime.ToShortTimeString();

                if (barIdx == bar.Count - 1 || bar[barIdx].startTime.Date != bar[barIdx + 1].startTime.Date)
                {
                    worksheet.Cell(row, column++).Value = "Sluit";
                }
                else
                {
                    worksheet.Cell(row, column++).Value = bs.endTime.ToShortTimeString();

                }
                worksheet.Cell(row, column++).Value = bs.personel[0];
                worksheet.Cell(row, column++).Value = bs.personel[1];

                column = 1;
                row++;
            }
            worksheet.Columns().AdjustToContents();
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

        static void addBarHeader(IXLWorksheet worksheet, int row)
        {
            int column = 1;

            worksheet.Cell(row, column++).Value = "Datum";
            worksheet.Cell(row, column++).Value = "Begin";
            worksheet.Cell(row, column++).Value = "Eind";
            worksheet.Cell(row, column++).Value = "Persoon 1";
            worksheet.Cell(row, column).Value = "Persoon 2";

            worksheet.Range(row, 1, row, column).Style.Font.Bold = true;
            worksheet.Range(row, 1, row, column).Style.Border.BottomBorder = XLBorderStyleValues.Thick;
        }

        static void addHeader(IXLWorksheet worksheet, int row, int column)
        {
            worksheet.Cell(row, column++).Value = "Datum";
            worksheet.Cell(row, column++).Value = "Tijd";
            worksheet.Cell(row, column++).Value = "Team thuis";
            worksheet.Cell(row, column++).Value = "Team uit";
            worksheet.Cell(row, column++).Value = "Scheidsrechter";
            worksheet.Cell(row, column++).Value = "Teller";
            worksheet.Cell(row, column).Value = "Veld";

            worksheet.Range(row, 1, row, column).Style.Font.Bold = true;
            worksheet.Range(row, 1, row, column).Style.Border.BottomBorder = XLBorderStyleValues.Thick;

        }
    }

}
