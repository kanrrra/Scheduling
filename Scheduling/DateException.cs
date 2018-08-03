using System;

namespace Scheduling
{
    class DateException
    {
        public DateTime date;
        public string teamName;

        public DateException(DateTime date, string teamName)
        {
            this.date = date;
            this.teamName = teamName;
        }
    }
}
