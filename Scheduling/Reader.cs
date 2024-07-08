using DocumentFormat.OpenXml.Vml;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Scheduling
{
    internal class Reader
    {
        static Regex csvSplit = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)", RegexOptions.Compiled);

        string previousFirstCell;

        public static string[] SplitCsv(string line)
        {
            List<string> result = new List<string>();
            StringBuilder currentStr = new StringBuilder("");
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++) // For each character
            {
                if (line[i] == '\"') // Quotes are closing or opening
                    inQuotes = !inQuotes;
                else if (line[i] == ',') // Comma
                {
                    if (!inQuotes) // If not in quotes, end of current string, add it to result
                    {
                        result.Add(currentStr.ToString());
                        currentStr.Clear();
                    }
                    else
                        currentStr.Append(line[i]); // If in quotes, just add it 
                }
                else // Add any other character to current string
                    currentStr.Append(line[i]);
            }
            result.Add(currentStr.ToString());
            return result.ToArray(); // Return array of all strings
        }

        Dictionary<string, int> matchFileIndices = new Dictionary<string, int>();

        string dateIndex = "yyyymmdd";
        string timeIndex = "tijd";
        string homeHeaderName = "team thuis";
        string awayHeaderName = "team uit";
        string gymIndex = "zaal";
        string refereeIndex = "scheidsrechter";
        string scoreIndex = "teller";
        string fieldIndex = "veld";
        string startTimeIndex = "begin";
        string endTimeIndex = "einde";
        string personOneIndex = "persoon 1";
        string personTwoIndex = "persoon 2";

        Dictionary<string, int> barFileIndices = new Dictionary<string, int>();

        public Reader()
        {
        }

        //misc YYMMDD
        private DateTime dateFromString(string date, string time)
        {
            string[] dateTokens = date.Split(new char[] { '-', '/' });
            string[] timeTokens = time.Split(new char[] { ':', '.' });

            int year = int.Parse(dateTokens[0]);
            int month = int.Parse(dateTokens[1]);
            int day = int.Parse(dateTokens[2]);

            int hour = int.Parse(timeTokens[0]);
            int minute = int.Parse(timeTokens[1]);

            DateTime dateTime = new DateTime(year, month, day, hour, minute, 0);

            return dateTime;
        }

        //players
        private Player createPlayerFromString(string playerString)
        {
            string[] tokens = playerString.Split(new char[] { ',', ';', '\t' });
            string name = tokens[0].Trim();

            if (name.Length < 1) return null;

            string teamName = tokens[1].Trim();
            if (teamName.Length == 0)
            {
                teamName = "V";

                Console.Out.WriteLine($"Found player {name} without team. Assigning to team v (volunteer)");
            }

            double currentCost = 0;
            if (tokens.Length > 4 && tokens[4].Length > 0)
            {
                currentCost = double.Parse(tokens[4]);
            }

            bool exemption = false;
            if (tokens.Length > 5 && tokens[5].Length > 0)
            {
                exemption = true;
            }

            if (tokens[3].Length < 1)
            {
                tokens[3] = "1980/01/01";
                Console.Out.WriteLine($"No date of birth found for playyer {name}. Using default 1980/1/1");
            }

            return new Player(name, teamName, tokens[2].Trim(), dateFromString(tokens[3], "00:00"), currentCost, exemption);
        }

        //bar
        private BarShift createBarshiftFromString(string barString)
        {
            if (barString.Trim().Length < 1) return null;

            string[] tokens = SplitCsv(barString);

            if (tokens[barFileIndices[startTimeIndex]].Length < 1) return null;

            if (barString.ToLower().Contains("extra"))
            {
                return null;
            }

            string date = tokens[barFileIndices[dateIndex]];

            // if there is no date, use the previous date
            if (date.Length < 1)
            {
                date = previousFirstCell;
            }
            else
            {
                previousFirstCell = date;
            }


            DateTime startTime = dateFromString(date, tokens[barFileIndices[startTimeIndex]]);
            DateTime endTime = dateFromString(date, tokens[barFileIndices[endTimeIndex]]);

            string person1 = Regex.Replace(tokens[barFileIndices[personOneIndex]], @"\(.*?\)", "").Trim();
            string person2 = Regex.Replace(tokens[barFileIndices[personTwoIndex]], @"\(.*?\)", "").Trim();



            if (startTime.DayOfWeek != DayOfWeek.Saturday)
            {
                if (person1.Trim().Length < 1)
                {
                    person1 = "volunteer";
                }
                if (person2.Trim().Length < 1)
                {
                    person2 = "volunteer";
                }
            }

            return new BarShift(startTime, endTime, person1, person2);
        }


        public List<DateException> readExceptionsFromProgram(string exceptionPath)
        {
            Console.Out.WriteLine("Reading unavailable dates");

            List<DateException> dateExceptions = new List<DateException>();

            string line;

            System.IO.StreamReader file = new System.IO.StreamReader(exceptionPath, Encoding.UTF8);

            //skip first line (headers)
            file.ReadLine();

            while ((line = file.ReadLine()) != null)
            {
                if (line.Trim().Length == 0) continue;

                var de = createDateExceptionFromProgramString(line);
                if (de != null) dateExceptions.Add(de);
            }

            file.Close();

            return dateExceptions;
        }

        //date busy
        private DateException createDateExceptionFromProgramString(string dateExceptionString)
        {
            string[] tokens = SplitCsv(dateExceptionString);

            string date = tokens[matchFileIndices[dateIndex]];

            if (date.Length < 1) return null;

            string gym = tokens[matchFileIndices[gymIndex]];

            if (gym != "Kruisboog")
            {
                int awayIndex = matchFileIndices[awayHeaderName];

                string taurusTeam = "";
                int taurusIndex = tokens[awayIndex].IndexOf("Taurus");
                if (taurusIndex < 0)
                {
                    int homeIndex = matchFileIndices[homeHeaderName];
                    taurusIndex = tokens[homeIndex].IndexOf("Taurus");
                    taurusTeam = tokens[homeIndex].Substring(taurusIndex);
                }
                else
                {
                    taurusTeam = tokens[awayIndex].Substring(taurusIndex);
                }

                if (taurusTeam == "")
                {
                    return null;
                }


                return new DateException(dateFromString(date, "00:00").Date, taurusTeam);
            }

            return null;
        }

        public List<BarShift> readBarShifts(string barPath)
        {
            Console.Out.WriteLine("Reading bar shifts");

            List<BarShift> barshifts = new List<BarShift>();

            System.IO.StreamReader file = new System.IO.StreamReader(barPath, Encoding.UTF8);

            //skip first line
            string line = file.ReadLine();
            if (line == null)
            {
                Console.Out.WriteLine("readBarShifts read line null");
                return barshifts;
            }

            setHeaderFileBarIndices(line);

            while ((line = file.ReadLine()) != null)
            {
                if (line.Trim().Length == 0) continue;

                var bs = createBarshiftFromString(line);
                if (bs != null) barshifts.Add(bs);
            }

            file.Close();

            return barshifts;
        }

        //teams
        //name, level
        private Team createTeamFromString(string teamString)
        {
            string[] tokens = teamString.Split(',');

            string name = tokens[0].Trim(' ');
            string level = tokens[1].Trim(' ');

            int additionalPeopleRequired = int.Parse(tokens[2]);
            int flagsRequired = int.Parse(tokens[3]);
            int additionalPreMatchTime = int.Parse(tokens[4]);
            int matchDurationMinutes = int.Parse(tokens[5]);

            return new Team(name, level, additionalPeopleRequired, flagsRequired, additionalPreMatchTime, matchDurationMinutes);
        }

        public List<Team> readTeams(string path)
        {
            Console.Out.WriteLine("Reading teams");

            List<Team> teams = new List<Team>();

            string line;

            System.IO.StreamReader file = new System.IO.StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                if (line.Trim().Length == 0) continue;

                teams.Add(createTeamFromString(line));
            }

            file.Close();

            return teams;
        }

        public List<Player> readPlayers(string path)
        {
            Console.Out.WriteLine("Reading players");

            List<Player> players = new List<Player>();

            string line;
            bool header = true;

            System.IO.StreamReader file = new System.IO.StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                if (header)
                {
                    header = false;
                    continue;
                }
                line = line.Trim();
                if (line.Length == 0) continue;
                if (line[0] == '#') continue;
                var player = createPlayerFromString(line);

                if (player != null) players.Add(player);
                else Console.Out.WriteLine("Skipped reading player on line: [" + line + "]");
            }

            file.Close();


            return players;
        }

        public List<Match> readProgram(string path)
        {
            Console.Out.WriteLine("Reading program");

            List<Match> matches = new List<Match>();

            System.IO.StreamReader file = new System.IO.StreamReader(path, Encoding.UTF8);

            //skip first line
            string line = file.ReadLine();
            if (line == null)
            {
                Console.Out.WriteLine("readProgram read line null");
                return matches;
            }

            setHeaderFileMatchIndices(line);


            while ((line = file.ReadLine()) != null)
            {
                if (line.Length < 1) continue;
                if (line[0] == '#') continue;

                var match = createMatchFromProgramString(line);

                if (match != null)
                {
                    if (matches.Count > 0 && match == matches[matches.Count - 1])
                    {
                        // for duplicate matches i.e. Taurus vs Taurus
                        matches.Add(new Match(match.opponent, match.teamName, match.GetProgramStartTime(), "", "", false, match.field));
                    }
                    else
                    {
                        matches.Add(match);
                    }

                }
            }

            file.Close();

            return matches;
        }

        private void setHeaderFileMatchIndices(string line)
        {
            string[] tokens = line.Split(new char[] { ',', ';', '\t' });

            for (int i = 0; i < tokens.Length; i++)
            {
                matchFileIndices[tokens[i].ToLower().Trim()] = i;
            }
        }

        private void setHeaderFileBarIndices(string line)
        {
            string[] tokens = line.Split(new char[] { ',', ';', '\t' });

            for (int i = 0; i < tokens.Length; i++)
            {
                barFileIndices[tokens[i].ToLower().Trim()] = i;
            }
        }

        //matches
        private Match createMatchFromProgramString(string matchString)
        {
            string[] tokens = SplitCsv(matchString);

            string date = tokens[matchFileIndices[dateIndex]];
            string time = tokens[matchFileIndices[timeIndex]];
            string gym = tokens[matchFileIndices[gymIndex]];

            if (date.Length < 1) return null;
            if (time.Length < 1) return null;

            var matchDate = dateFromString(date, time);

            if (gym != "Kruisboog")
            {
                return null;
            }


            string homeTeam = tokens[matchFileIndices[homeHeaderName]].Substring(tokens[matchFileIndices[homeHeaderName]].IndexOf("Taurus"));
            string awayTeam = tokens[matchFileIndices[awayHeaderName]];
            string referee = tokens[matchFileIndices[refereeIndex]];
            string score = tokens[matchFileIndices[scoreIndex]];

            string field = tokens[matchFileIndices[fieldIndex]];

            //official start time
            return new Match(homeTeam, awayTeam, matchDate, referee, score, true, field);
        }

        public HashSet<string> readVolunteers(string path)
        {
            Console.Out.WriteLine("Reading volunteers");

            HashSet<string> result = new HashSet<string>();

            System.IO.StreamReader file = new System.IO.StreamReader(path, Encoding.UTF8);
            string line;

            while ((line = file.ReadLine()) != null)
            {
                if (line.Length < 1) continue;

                string[] tokens = SplitCsv(line);
                result.Add(tokens[0]);
            }

            return result;

        }

    }
}