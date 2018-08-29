namespace Neo4j4Net
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;
    using Neo4j.Driver.V1;
    using Neo4jClient;
    using Neo4jClient.Cypher;
    using Newtonsoft.Json;
    using TransactionScopeOption = Neo4jClient.Transactions.TransactionScopeOption;

    internal class Program
    {
        private static void Main()
        {
//                        Neo4jDriverVersion.Run();
            Neo4jClientVersion.Run();
            

            Console.WriteLine("ENTER exits.");
            Console.ReadLine();
        }
    }

    #region Neo4j.Driver
    public class Neo4jDriverVersion
    {
      
        public static void Run()
        {
            using (var driver = GraphDatabase.Driver("bolt://localhost:7687"))
            {
                using (var session = driver.Session())
                {
                    using (var tx = session.BeginTransaction()) { 
                        IStatementResult results = tx.Run("MATCH (m:Movie) RETURN m");
                        foreach (IRecord result in results)
                        {
                            var node = result["m"].As<INode>();
                            var title = node.Properties["title"]?.As<string>();
                            var released = node.Properties["released"]?.As<int>();
                            var tagline = node.Properties.ContainsKey("tagline")
                                ? node.Properties["tagline"]?.As<string>()
                                : "No Tagline";

                            var movie = new Movie
                            {
                                Title = title,
                                Released = released ?? -1,
                                Tagline = tagline
                            };

                            Console.WriteLine(movie);
                        }
                    }
                }
            }
        }
    }
    #endregion Neo4j.Driver

    #region Neo4jClient
    public class Neo4jClientVersion
    {
//        public static void Run()
//        {
//            using (var gc = new GraphClient(new Uri("http://localhost:7998/db/data")))
//            {
//                gc.Connect();
//
//                //MATCH (m:Movie) 
//                //RETURN m
//
//                var query = gc.Cypher.Match($"(m:{Movie.Label})").Return(m => m.As<Movie>());
//
//                Console.WriteLine(query.Query.DebugQueryText);
//
//                foreach (var movie in query.Results)
//                    Console.WriteLine(movie);
//            }
//        }
        /*
         public static void Run()
        {
            using (var gc = new GraphClient(new Uri("http://localhost:7998/db/data")))
            {
                gc.Connect();

                //MATCH (m:Movie) 
                //RETURN m

                var query = gc.Cypher
                    .Match($"(m:{Movie.Label})")
                    .Where((Movie m) => m.Released > 1999)
                    .Return(m => m.As<Movie>());

                Console.WriteLine(query.Query.DebugQueryText);

                foreach (var movie in query.Results)
                    Console.WriteLine(movie);
            }
        }
         */

         public static void Run()
        {
            using (var gc = new GraphClient(new Uri("http://localhost:7474/db/data"), "neo4j", "neo"))
            {
gc.Connect();
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)) { 
                    //MATCH (p:Person)-[:ACTED_IN]->(m:Movie) 
                    //RETURN m, DISTINCT COLLECT(a)

                    var query = new CypherFluentQuery(gc)
                        .Match($"(a:{Person.Label})-[:{MovieRelationships.ActedIn}]->(m:{Movie.Label})")
                        .Where((Movie m) => m.Released > 2000)
                        .Return((m, a) => new
                        {
                            Movie = m.As<Movie>(),
                            Actors = a.CollectAsDistinct<Person>()
                        });
                    Console.WriteLine(query.Query.DebugQueryText);

                    foreach (var movie in query.Results)
                        Console.WriteLine(movie);

                    scope.Complete();
                }
            }
        }
    }
    #endregion Neo4jClient

    public static class MovieRelationships
    {
        /// <summary><c>(Person)-[:ACTED_IN]-&gt;(Movie)</c></summary>
        /// <remarks> Represents the <c>ACTED_IN</c> relationship from <c>Person</c> to <c>Movie</c>, i.e. Person ACTED IN Movie</remarks>
        public const string ActedIn = "ACTED_IN";
    }

    public class Person
    {
        public const string Label = "Person";

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class Movie
    {
        public const string Label = "Movie";

        [JsonProperty("tagline")]
        public string Tagline { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("released")]
        public int Released { get; set; }

        public override string ToString()
        {
            return $"{Title} ({Released}) - '{(string.IsNullOrWhiteSpace(Tagline) ? "No Tagline" : Tagline)}'";
        }
    }

    public class ActionMovie : Movie
    {
        public new static string Label => $"{Movie.Label}:Action";
    }
}