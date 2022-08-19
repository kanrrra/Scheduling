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

        string dateIndex = "datum";
        string timeIndex = "tijd";
        string homeHeaderName = "team thuis";
        string awayHeaderName = "team uit";
        string gymIndex = "zaal";
        string refereeIndex = "scheidsrechter";
        string scoreIndex = "teller";
        string fieldIndex = "veld";

        public Reader()
        {
        }

        //misc
        private DateTime dateFromString(string date, string time)
        {
            string[] dateTokens = date.Split(new char[] { '-', '/' });
            string[] timeTokens = time.Split(new char[] { ':', '.' });

            DateTime dateTime = new DateTime(int.Parse(dateTokens[0]), int.Parse(dateTokens[1]), int.Parse(dateTokens[2]),
                int.Parse(timeTokens[0]), int.Parse(timeTokens[1]), 0);

            return dateTime;
        }

        //players
        private Player createPlayerFromString(string playerString)
        {
            string[] tokens = playerString.Split(new char[] { ',', ';', '\t' });
            string name = tokens[0].Trim();

            if (name.Length < 1) return null;

            string teamName = tokens[1].Trim();
            if(teamName.Length == 0)
            {
                teamName = "V";

                Console.Out.WriteLine($"Found player {name} without team. Assigning to team v (volunteer)");
            }

            double currentCost = 0;
            if(tokens.Length > 4 && tokens[4].Length > 0)
            {
                currentCost = double.Parse(tokens[4]);
            }

            bool exemption = false;
            if (tokens.Length > 5 && tokens[5].Length > 0)
            {
                exemption = true;
            }

            return new Player(name, teamName, tokens[2].Trim(), dateFromString(tokens[3], "00:00"), currentCost, exemption);
        }

        //bar
        private BarShift createBarshiftFromString(string barString)
        {
            if (barString.Trim().Length < 1) return null;

            string[] tokens = barString.Split(',');
            if (tokens[1].Length < 1) return null;
            if (barString.ToLower().Contains("extra"))
            {
                return null;
            }

            // if there is no date, use the previous date
            if (tokens[0].Length < 1)
            {
                tokens[0] = previousFirstCell;
            } else
            {
                previousFirstCell = tokens[0];
            }

            DateTime startTime = dateFromString(tokens[0], tokens[1]);
            DateTime endTime = dateFromString(tokens[0], tokens[2]);

            string person1 = Regex.Replace(tokens[3], @"\(.*?\)", "").Trim();
            string person2 = Regex.Replace(tokens[4], @"\(.*?\)", "").Trim();



            if (startTime.DayOfWeek != DayOfWeek.Saturday)
            {
                if(person1.Trim().Length < 1)
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
            List<DateException> dateExceptions = new List<DateException>();

            string line;

            System.IO.StreamReader file = new System.IO.StreamReader(exceptionPath);

            //skip first line (headers)
            file.ReadLine();

            while ((line = file.ReadLine()) != null)
            {
                if (line.Trim().Length == 0) continue;

                var de = createDateExceptionFromProgramString(line);
                if(de != null) dateExceptions.Add(de);
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
                } else
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
            List<BarShift> barshifts = new List<BarShift>();

            string line;

            System.IO.StreamReader file = new System.IO.StreamReader(barPath);
            //skip first line (headers)
            file.ReadLine();

            while ((line = file.ReadLine()) != null)
            {
                if (line.Trim().Length == 0) continue;

                var bs = createBarshiftFromString(line);
                if(bs != null) barshifts.Add(bs);
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
            List<Match> matches = new List<Match>();

            
            System.IO.StreamReader file = new System.IO.StreamReader(path);

            //skip first line
            string line = file.ReadLine();
            if(line == null)
            {
                return matches;
            }

            setHeaderFileMatchIndices(line);


            while ((line = file.ReadLine()) != null)
            {
                if (line.Length < 1) continue;

                var match = createMatchFromProgramString(line);

                if (match != null)
                {
                    if(matches.Count > 0 &&  match == matches[matches.Count - 1])
                    {
                        matches.Add(new Match(match.opponent, match.teamName, match.GetProgramStartTime(), "", "", false, match.field));
                    } else
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

            for(int i = 0; i < tokens.Length; i++)
            {
                matchFileIndices[tokens[i].ToLower().Trim()] = i;
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

    }
}