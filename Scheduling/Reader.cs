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
            string[] tokens = playerString.Split(',');

            return new Player(tokens[0].Trim(), tokens[1].Trim(), tokens[2].Trim(), dateFromString(tokens[3], "00:00"));
        }

        //matches
        private Match createMatchFromString(string matchString)
        {
            string[] tokens = matchString.Split(new char[] { ',', ';', '\t' });

            //official start time
            return new Match(tokens[2], dateFromString(tokens[0], tokens[1]));
        }

        //bar
        private BarShift createBarshiftFromString(string barString)
        {
            string[] tokens = barString.Split(',');

            return new BarShift(dateFromString(tokens[0], tokens[1]), dateFromString(tokens[0], tokens[2]));
        }

        public List<BarShift> readBarShifts(string barPath)
        {
            List<BarShift> barshifts = new List<BarShift>();

            string line;

            System.IO.StreamReader file = new System.IO.StreamReader(barPath);
            while ((line = file.ReadLine()) != null)
            {
                if (line.Trim().Length == 0) continue;

                barshifts.Add(createBarshiftFromString(line));
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
                if (line.Trim().Length == 0) continue;
                
                players.Add(createPlayerFromString(line));
            }

            file.Close();


            return players;
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