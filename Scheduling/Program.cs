﻿using System;
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
        static string exceptionPath = "C:/data/exceptions.csv";

        static void Main(string[] args)
        {
            Reader r = new Reader();
            
            List<Match> matches = r.readMatches(matchPath);
            List<Player> players = r.readPlayers(playersPath);
            List<Team> teams = r.readTeams(teamsPath);
            List<BarShift> bar = r.readBarShifts(barPath);
            List<DateException> dateExceptions = r.readExceptions(exceptionPath);

            Planner p = new Planner(matches, players, teams, bar, dateExceptions);


            foreach (Team t in teams){
                Console.Out.WriteLine(t);
                Console.Out.WriteLine("");
            }



            p.generateSchema();

            Console.In.ReadLine();
        }
    }
}
