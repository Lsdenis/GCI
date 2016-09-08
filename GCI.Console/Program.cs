using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using GCI.Console.Enums;
using GCI.Console.LessonData;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace GCI.Console
{
    internal class Program
    {
        private static readonly string[] Scopes = {CalendarService.Scope.Calendar};
        private static readonly string ApplicationName = "GCI";

        public const int CurrentWeek = 2;
        public const int StartDate = 5;

        private static void Main(string[] args)
        {
            UserCredential credential;
            System.Console.OutputEncoding = Encoding.Unicode;

            using (var stream =
                new FileStream("client_id.json", FileMode.Open, FileAccess.Read))
            {
                var credPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials\\calendar-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                System.Console.WriteLine("Credential file saved to: " + credPath);
            }

            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });

            Task.Run(async () =>
            {
                var lessons = await GetLessons();

                for (int i = 9; i < 31; i++)
                {
                    var date = new DateTime(2016, 9, i);

                    var currentWeekDay = lessons.FirstOrDefault(day => day.DayOfWeek == date.DayOfWeek);
                    if (currentWeekDay == null)
                    {
                        continue;
                    }

                    var currentLessons = currentWeekDay.Lessons;

                    var currentWeekOffset = (i - StartDate) / 7;

                    while (currentWeekOffset > 4)
                    {
                        currentWeekOffset -= 4;
                    }

                    var currentWeek = CurrentWeek + currentWeekOffset;

                    if (currentWeek == 5)
                    {
                        currentWeek = 1;
                    }

                    foreach (var lesson in currentLessons.Where(day => day.WeekNumbers.Contains(currentWeek) && day.SubGroup != 1))
                    {
                        ProcessLesson(service, lesson, date);
                    }
                }
            }).Wait();

            System.Console.Read();
        }

        private static void ProcessLesson(CalendarService service, Lesson lesson, DateTime date)
        {
            var @event = new Event();
            @event.Created = DateTime.Now;
            var typeOfLesson = GetTypeOfLesson(lesson.LessonType);
            @event.Summary = string.Format("{0}\n{1} - {2} - {3}", typeOfLesson, lesson.Subject, lesson.Auditory, lesson.LastName);
            @event.ColorId = GetColorOfLesson(lesson.LessonType);
            @event.Description = string.Format("{0} {1} {2}", lesson.LastName, lesson.FirstName, lesson.SecondName);
            @event.Start = new EventDateTime { DateTime = new DateTime(date.Year, date.Month, date.Day, lesson.StartAt.Hour, lesson.StartAt.Minute,
                lesson.StartAt.Second)
            };
            @event.End = new EventDateTime {
                DateTime = new DateTime(date.Year, date.Month, date.Day, lesson.EndAt.Hour, lesson.EndAt.Minute,
                lesson.EndAt.Second)
            };

            service.Events.Insert(@event, "falk3kb9opccsotlihs3s8i958@group.calendar.google.com").Execute();
        }

        private static string GetTypeOfLesson(LessonType lessonType)
        {
            switch (lessonType)
            {
                case LessonType.Lection:
                    return "Лекция";
                case LessonType.Labs:
                    return "Лабораторная работа";
                case LessonType.Practical:
                    return "Практическое занятие";
                default:
                    throw new ArgumentOutOfRangeException(nameof(lessonType), lessonType, null);
            }
        }

        private static string GetColorOfLesson(LessonType lessonType)
        {
            switch (lessonType)
            {
                case LessonType.Lection:
                    return "2";
                case LessonType.Labs:
                    return "6";
                case LessonType.Practical:
                    return "9";
                default:
                    throw new ArgumentOutOfRangeException(nameof(lessonType), lessonType, null);
            }
        }

        private static async Task<List<WeekDay>> GetLessons()
        {
            var httpClient = new HttpClient();
            var xmlResponseData = await httpClient.GetAsync("http://www.bsuir.by/schedule/rest/schedule/21540");
            using (var stream = new MemoryStream())
            {
                await xmlResponseData.Content.CopyToAsync(stream);

                stream.Position = 0;
                var root = XElement.Load(stream);
                var weeksXml = root.Elements().ToList();
                var weekDays = new List<WeekDay>();

                for (var i = 0; i < weeksXml.Count(); i++)
                {
                    var week = weeksXml[i];

                    var dayLessons = ParseDayLessons(week);
                    weekDays.Add(dayLessons);
                }

                return weekDays;
            }
        }

        private static WeekDay ParseDayLessons(XContainer week)
        {
            var weekDay = new WeekDay();

            var lessons = week.Elements().ToList();

            foreach (var lesson in lessons)
            {
                if (lesson.Name.LocalName.Equals("weekDay"))
                {
                    switch (lesson.Value)
                    {
                        case "Понедельник":
                        {
                            weekDay.DayOfWeek = DayOfWeek.Monday;
                            break;
                        }
                        case "Вторник":
                        {
                            weekDay.DayOfWeek = DayOfWeek.Tuesday;
                            break;
                        }
                        case "Среда":
                        {
                            weekDay.DayOfWeek = DayOfWeek.Wednesday;
                            break;
                        }
                        case "Четверг":
                        {
                            weekDay.DayOfWeek = DayOfWeek.Thursday;
                            break;
                        }
                        case "Пятница":
                        {
                            weekDay.DayOfWeek = DayOfWeek.Friday;
                            break;
                        }
                        case "Суббота":
                        {
                            weekDay.DayOfWeek = DayOfWeek.Saturday;
                            break;
                        }
                    }

                    continue;
                }

                var lessonItem = new Lesson();

                foreach (var particularLesson in lesson.Elements())
                {
                    if (particularLesson.Name.LocalName.Equals("employee"))
                    {
                        foreach (var employeeValue in particularLesson.Elements())
                        {
                            switch (employeeValue.Name.LocalName)
                            {
                                case "firstName":
                                {
                                    lessonItem.FirstName = employeeValue.Value;
                                    break;
                                }
                                case "middleName":
                                {
                                    lessonItem.SecondName = employeeValue.Value;
                                    break;
                                }
                                case "lastName":
                                {
                                    lessonItem.LastName = employeeValue.Value;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        switch (particularLesson.Name.LocalName)
                        {
                            case "auditory":
                            {
                                lessonItem.Auditory = particularLesson.Value;
                                break;
                            }
                            case "numSubgroup":
                            {
                                lessonItem.SubGroup = int.Parse(particularLesson.Value);
                                break;
                            }
                            case "lessonType":
                            {
                                switch (particularLesson.Value)
                                {
                                    case "ЛК":
                                    {
                                        lessonItem.LessonType = LessonType.Lection;
                                        break;
                                    }
                                    case "ПЗ":
                                    {
                                        lessonItem.LessonType = LessonType.Practical;
                                        break;
                                    }
                                    case "ЛР":
                                    {
                                        lessonItem.LessonType = LessonType.Labs;
                                        break;
                                    }
                                }

                                break;
                            }
                            case "subject":
                            {
                                lessonItem.Subject = particularLesson.Value;
                                break;
                            }
                            case "weekNumber":
                            {
                                lessonItem.WeekNumbers.Add(int.Parse(particularLesson.Value));
                                break;
                            }
                            case "lessonTime":
                            {
                                var separatorIndex = particularLesson.Value.IndexOf("-", StringComparison.InvariantCultureIgnoreCase);
                                var startString = particularLesson.Value.Substring(0, particularLesson.Value.Length - separatorIndex - 1);
                                var endString = particularLesson.Value.Substring(separatorIndex + 1, particularLesson.Value.Length - separatorIndex - 1);

                                lessonItem.StartAt = DateTime.Parse(startString);
                                lessonItem.EndAt = DateTime.Parse(endString);
                                break;
                            }
                        }
                    }
                }

                weekDay.Lessons.Add(lessonItem);
            }

            return weekDay;
        }
    }
}