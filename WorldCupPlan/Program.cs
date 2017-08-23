using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WorldCupPlan
{
    class Program
    {
        static void Main(string[] args)
        {
            var allLines = File.ReadAllLines(@"D:\WorldCup\matches-complete.csv");
            var matches = new List<Match>();
            for (int i = 0; i < allLines.Length; i = i + 4)
            {
                var date = allLines[i].Substring(0, allLines[i].IndexOf(","));
                var time = allLines[i + 1].Substring(0, allLines[i + 1].IndexOf(","));
                string timeFormat = "2008 - 09 - 15T" + time.Substring(0, 5) + ":00.0000000+0" + time.Substring(time.IndexOf("+") + 1, 1) + ":00";
                var dateTime = DateTime.Parse(date);
                var timeParse = DateTime.Parse(timeFormat);
                dateTime = dateTime.Add(timeParse.TimeOfDay);
                var matchTeams = allLines[i + 2].Split(',');
                var localTeam = matchTeams[0].Trim();
                var visitorTeam = matchTeams[2].Trim();
                var matchName = matchTeams[1].Trim();

                var venue = allLines[i + 3];
                if (venue.StartsWith("\""))
                {
                    venue = venue.Replace('"', ' ');
                }
                var venueData = venue.Split(',');
                var stadiumName = venueData[0].Trim();
                var cityName = venueData[1].Trim();

                var matchObj = new Match();
                matchObj.Local = localTeam;
                matchObj.Visitor = visitorTeam;
                matchObj.MatchName = matchName;
                matchObj.MatchDateTimeUTC = dateTime.ToUniversalTime();

                var venueObj = new Venue();
                venueObj.HostCity = cityName;
                venueObj.StadiumName = stadiumName;
                matchObj.Venue = venueObj;

                matches.Add(matchObj);
            }

            var hostCities = matches.Select(s => s.Venue.HostCity).Distinct().ToArray();

            //Validate Host Cities
            Console.WriteLine("Number of Host Cities: {0} ", hostCities.Length);
            Console.WriteLine("Number of Venues: {0}", matches.Select(s => s.Venue.StadiumName).Distinct().Count());

            var distanceMatrix = new int[hostCities.Length, hostCities.Length];
            var key = "AIzaSyCt6No6Gmz_rsyN7BTBY__EGCpK_W1bprQ";
            var googleUri = "https://maps.googleapis.com/maps/api/distancematrix/json?units=metric&origins={0}&destinations={1}&key={2}";

            for (int i = 0; i < hostCities.Length; i++)
            {
                for (int j = 0; j < hostCities.Length; j++)
                {
                    if (i != j)
                    {
                        var uriA = Uri.EscapeUriString(hostCities[i] + ",Russia");
                        var uriB = Uri.EscapeUriString(hostCities[j] + ",Russia");
                        var targetUri = string.Format(googleUri, uriA, uriB, key);
                        var distanceMetres = -1;

                        try
                        {
                            var response = GetUri(targetUri);
                            dynamic responseParsed = JsonConvert.DeserializeObject(response);
                            if (responseParsed.rows != null)
                            {
                                foreach (dynamic row in responseParsed.rows)
                                {
                                    if (row.elements != null)
                                    {
                                        foreach (dynamic element in row.elements)
                                        {
                                            if (element.distance.value != null)
                                            {
                                                distanceMetres = element.distance.value;
                                            }
                                        }
                                    }
                                }
                            }

                            if (distanceMetres != -1)
                            {
                                distanceMatrix[i, j] = distanceMetres;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("There was an error calculating distance from {0} to {1}. Exception {2}", hostCities[i], hostCities[j], e.ToString());
                        }
                    }
                }
            }

            //By Default start Trip on Moscow

            var teams = matches.Select(s => s.Local).Union(matches.Select(s => s.Visitor)).Distinct();
            var teamsDistance = new List<Tuple<string,int,string>>() ;
            foreach (var team in teams)
            {
                var teamName = team;
                var teamMatches = matches.Where(s => s.Local == teamName || s.Visitor == teamName);
                var originCity = "Moscow";
                var totalDistance = 0;
                var path = "";
                Console.WriteLine("Team {0} will follow this path", teamName);

                teamMatches = teamMatches.OrderBy(s => s.MatchDateTimeUTC);

                foreach (var match in teamMatches)
                {
                    var destinationCity = match.Venue.HostCity;
                    var distance = distanceMatrix[IndexOf(hostCities, originCity), IndexOf(hostCities, destinationCity)];
                    Console.WriteLine("Trip from {0} to {1}: {2} kms for Match on {3} ", originCity, destinationCity, distance / 1000, match.MatchDateTimeUTC);
                    originCity = destinationCity;
                    totalDistance += distance;
                    path += destinationCity + ",";
                }
                teamsDistance.Add(new Tuple<string, int,string>(teamName, totalDistance, path.Substring(0, path.Length - 1)));
                Console.WriteLine("Team {0} will travel a total of {1} kms", teamName, totalDistance/1000);
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine("Teams ordered by distance to travel");
            foreach (var tuple in teamsDistance.OrderBy( s => s.Item2))
            {
                Console.WriteLine("Team {0}, Distance: {1} kms, Cities: {2} ", tuple.Item1, tuple.Item2/1000, tuple.Item3);
            }
            
            Console.ReadLine();
        }

        public static string GetUri(string uri)
        {
            WebRequest request = WebRequest.Create(uri);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();

            return responseFromServer;
        }

        public static int IndexOf(string [] array, string value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == value)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
