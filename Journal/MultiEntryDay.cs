using System;

namespace AdamOneilSoftware
{
    internal class MultiEntryDay
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Count { get; set; }

        public DateTime Date
        {
            get { return new DateTime(Year, Month, Day); }
        }
    }
}