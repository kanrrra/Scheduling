using System;
using System.Collections.Generic;

namespace Scheduling
{
    internal class Reader
    {
        public Reader()
        {
        }

        //misc
        private DateTime dateFromString(string date, string time)
        {
            string[] dateTokens = date.Split(new char[] { '-', '/' });
            string[] timeTokens = time.Split(new char[] { ':', '.' });

            DateTime dateTime = new DateTime(int.Parse(dateTokens[2]), int.Parse(dateTokens[1]), int.Parse(dateTokens[0]),
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
            double currentCost = tokens.Length > 4 ? double.Parse(tokens[4]) : 0;

            return new Player(name, teamName, tokens[2].Trim(), dateFromString(tokens[3], "00:00"), currentCost);
        }

        //matches
        private Match createMatchFromString(string matchString)
        {
            string[] tokens = matchString.Split(new char[] { ',', ';', '\t' });

            //official start time
            return new Match(tokens[2], dateFromString(tokens[0], tokens[1]));
        }

        //date busy
        private DateException createDateExceptionFromString(string dateExceptionString)
        {
            string[] tokens = dateExceptionString.Split(',');

            return new DateException(dateFromString(tokens[0], "00:00").Date, tokens[1].Trim());
        }

        //bar
        private BarShift createBarshiftFromString(string barString)
        {
            if (barString.Trim().Length < 1) return null;

            string[] tokens = barString.Split(',');
            if (tokens[0].Length < 1) return null;
            if (barString.ToLower().Contains("extra"))
            {
                return null;
            }

            return new BarShift(dateFromString(tokens[0], tokens[1]), dateFromString(tokens[0], tokens[2]), tokens[3], tokens[4]);
        }

        public List<DateException> readExceptions(string exceptionPath)
        {
            List<DateException> dateExceptions = new List<DateException>();

            string line;

            System.IO.StreamReader file = new System.IO.StreamReader(exceptionPath);
            while ((line = file.ReadLine()) != null)
            {
                if (line.Trim().Length == 0) continue;

                dateExceptions.Add(createDateExceptionFromString(line));
            }

            file.Close();

            return dateExceptions;
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
            string[] tokens = dateExceptionString.Split(',');

            string date = tokens[0];

            if (date.Length < 1) return null;

            string gym = tokens[11];

            if (gym != "Kruisboog")
            {
                string awayTeam = tokens[3];
                
                return new DateException(dateFromString(date, "00:00").Date, awayTeam);
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
            return new Team(tokens[0].Trim(' '), tokens[1].Trim(' '), int.Parse(tokens[2]), int.Parse(tokens[3]), int.Parse(tokens[4]), int.Parse(tokens[5]));
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

            System.IO.StreamReader file = new System.IO.StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
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

            string line;
            
            System.IO.StreamReader file = new System.IO.StreamReader(path);

            //skip first line
            file.ReadLine();

            while ((line = file.ReadLine()) != null)
            {
                var match = createMatchFromProgramString(line);

                if (match != null) matches.Add(match);
            }

            file.Close();

            return matches;
        }

        //matches
        private Match createMatchFromProgramString(string matchString)
        {
            string[] tokens = matchString.Split(new char[] { ',', ';', '\t' });

            string date = tokens[0];
            string time = tokens[1];
            string homeTeam = tokens[2];
            string awayTeam = tokens[3];
            string gym = tokens[11];

            if (date.Length < 1) return null;
            var matchDate = dateFromString(date, time);

            if (matchDate.DayOfWeek != DayOfWeek.Saturday)
                return null;

            if (gym == "Kruisboog")
            {
                //official start time
                return new Match(homeTeam, matchDate);
            } 

            return null;
        }

        //matches
        public List<Match> readMatches(string path)
        {
            List<Match> matches = new List<Match>();

            string line;

            System.IO.StreamReader file = new System.IO.StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                matches.Add(createMatchFromString(line));
            }

            file.Close();

            return matches;
        }

    }
}