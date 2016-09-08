using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCI.Console.LessonData
{
    public class WeekDay
    {
        public IList<Lesson> Lessons { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
    }
}
