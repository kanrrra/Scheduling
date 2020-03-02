using System;

namespace Scheduling
{
    class BarShift
    {
        public DateTime startTime;
        public DateTime endTime;

        public string[] personel = new string[2];

        public BarShift(DateTime start, DateTime end, string person1, string person2)
        {
            startTime = start;
            endTime = end;

            personel[0] = person1.Trim();
            personel[1] = person2.Trim();
        }

        
        public override string ToString()
        {
            return startTime.ToShortDateString() + "," + startTime.ToShortTimeString() + "," + endTime.ToShortTimeString() + "," + personel[0] + "," + personel[1];
        }
    }
}