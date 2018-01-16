using System;

namespace Scheduling
{
    class BarShift
    {
        public DateTime startTime;
        public DateTime endTime;

        public Player[] personel = new Player[2];

        public BarShift(DateTime start, DateTime end)
        {
            startTime = start;
            endTime = end;
        }
    }
}