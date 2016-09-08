using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using GCI.Console.LessonData;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;

namespace GCI.Console
{
    internal class Program
    {
        private static string[] Scopes = { CalendarService.Scope.Calendar };
        private static string ApplicationName = "GCI";

        private static void Main(string[] args)
        {
            UserCredential credential;
            System.Console.OutputEncoding = Encoding.Unicode;

            //			using (var stream =
            //				new FileStream("client_id.json", FileMode.Open, FileAccess.Read))
            //			{
            //				var credPath = Environment.GetFolderPath(
            //					Environment.SpecialFolder.Personal);
            //				credPath = Path.Combine(credPath, ".credentials\\calendar-dotnet-quickstart.json");
            //
            //				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            //					GoogleClientSecrets.Load(stream).Secrets,
            //					Scopes,
            //					"user",
            //					CancellationToken.None,
            //					new FileDataStore(credPath, true)).Result;
            //				System.Console.WriteLine("Credential file saved to: " + credPath);
            //			}

            // Create Google Calendar API service.
            //            var service = new CalendarService(new BaseClientService.Initializer()
            //            {
            //                HttpClientInitializer = credential,
            //                ApplicationName = ApplicationName,
            //            });

            Task.Run(async () =>
            {
                var a = await GetLessons();

                //                service.CalendarList.Get("73jdose7dqi6634fkbk3039458@group.calendar.google.com").Execute();

                //				var @event = new Event();
                //				@event.Created = DateTime.Now;
                //				@event.Summary = "Test";
                //				@event.ColorId = "1";
                //				@event.Description = "Description";
                //				@event.Start = new EventDateTime() { DateTime = DateTime.Now };
                //				@event.End = new EventDateTime() { DateTime = DateTime.Now.AddHours(2) };

                //				service.Events.Insert(@event, "73jdose7dqi6634fkbk3039458@group.calendar.google.com").Execute();
            }).Wait();

            System.Console.Read();
        }

        private static async Task<object> GetLessons()
        {
            var httpClient = new HttpClient();
            var xmlResponseData = await httpClient.GetAsync("http://www.bsuir.by/schedule/rest/schedule/21540");
            using (var stream = new MemoryStream())
            {
                await xmlResponseData.Content.CopyToAsync(stream);

                stream.Position = 0;
                var root = XElement.Load(stream);
                var forWeeks = root.Elements().ToList();

                for (var i = 0; i < forWeeks.Count(); i++)
                {
                    var week = forWeeks[i];

                    SetWeekValue(week);
                }

                //                var xmlTextReader = new XmlTextReader(stream);
                //
                //				while (xmlTextReader.Read())
                //				{
                //                    if (xmlTextReader.is)
                //
                //				    if (xmlTextReader.Name.Equals(""))
                //                        {
                //				        var a = xmlTextReader.Name;
                //				    }
                //
                //					var b = xmlTextReader.Value;
                //				}
            }


            return null;
        }

        private static WeekDay SetWeekValue(XContainer week)
        {
            var weekDay = new WeekDay();

            var lessons = week.Elements().ToList();

            foreach (XElement lesson in lessons)
            {
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

                weekDay.Lessons.Add(lessonItem);
            }

            return weekDay;
        }
    }
}
