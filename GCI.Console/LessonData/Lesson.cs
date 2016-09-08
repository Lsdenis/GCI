using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCI.Console.LessonData
{
    public class Lesson
    {
        public string Auditory { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string SecondName { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public string Subject { get; set; }

        public List<int> WeekNumbers { get; set; }
    }
}
