namespace DocumentDB.Samples.Queries
{
    using Shared.Util;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Threading.Tasks;

    //------------------------------------------------------------------------------------------------
    // This sample demonstrates the use of LINQ and SQL Query Grammar to query DocumentDB Service
    // For additional examples using the SQL query grammer refer to the SQL Query Tutorial available 
    // at https://azure.microsoft.com/documentation/articles/documentdb-sql-query/.
    // There is also an interactive Query Demo web application where you can try out different 
    // SQL queries available at https://www.documentdb.com/sql/demo.  
    //------------------------------------------------------------------------------------------------

    public class Program
    {
        private static DocumentClient client;

        // Assign an id for your database & collection 
        private static readonly string DatabaseName = "samples";
        private static readonly string CollectionName = "query-samples";

        // Read the DocumentDB endpointUrl and authorizationKeys from config
        // These values are available from the Azure Management Portal on the DocumentDB Account Blade under "Keys"
        // NB > Keep these values in a safe & secure location. Together they provide Administrative access to your DocDB account
        private static readonly string endpointUrl = ConfigurationManager.AppSettings["EndPointUrl"];
        private static readonly string authorizationKey = ConfigurationManager.AppSettings["AuthorizationKey"];

        // Set to true for this sample since it deals with different kinds of queries.
        private static readonly FeedOptions DefaultOptions = new FeedOptions { EnableCrossPartitionQuery = true, PopulateQueryMetrics = true };

        public static void Main(string[] args)
        {
            try
            {
                //Get a Document client
                using (client = new DocumentClient(new Uri(endpointUrl), authorizationKey, 
                    new ConnectionPolicy { ConnectionMode = ConnectionMode.Gateway, ConnectionProtocol = Protocol.Https }))
                {
                    RunDemoAsync(DatabaseName, CollectionName).Wait();
                }
            }
#if !DEBUG
            catch (Exception e)
            {
                LogException(e);
            }
#endif
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        private static async Task RunDemoAsync(string databaseId, string collectionId)
        {
            Database database = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseId });
            DocumentCollection collection = await GetOrCreateCollectionAsync(databaseId, collectionId);

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);

            await CreateDocuments(collectionUri);

            //--------------------------------------------------------------------------------------------------------
            // There are three ways of writing queries in the .NET SDK for DocumentDB, 
            // using the SQL Query Grammar, using LINQ Provider with Query and with Lambda. 
            // This sample will show how to write SQL Query Grammar using asynchronous requests.
            // 
            // This sample will also show how to display request charges for throughput analysis and estimation.
            //--------------------------------------------------------------------------------------------------------

            // Querying for equality using a single filter
            await QueryWithOneFilter(collectionUri);

            // Querying for equality using a double filter
            await QueryWithTwoFilters(collectionUri);

            // Querying using range operators like >, <, >=, <=
            await QueryWithRangeOperatorsDateTimes(collectionUri);

            // Query a single join
            await QueryWithSingleJoin(collectionUri);

            // Query with a double join
            await QueryWithDoubleJoin(collectionUri);

            // Uncomment to Cleanup
            // await client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(databaseId));
        }

        private static async Task QueryWithOneFilter(Uri collectionUri)
        {
            Console.WriteLine("Simple query equality. Find family where id = 'AndersenFamily'");
            Console.WriteLine("SELECT * FROM Families f WHERE f.id = 'AndersenFamily'");
            Console.WriteLine();

            // Query using a single filter on id
            IDocumentQuery <dynamic> equalityQuery = client.CreateDocumentQuery(collectionUri,
                "SELECT * FROM Families f WHERE f.id = 'AndersenFamily'", DefaultOptions).AsDocumentQuery();

            // Asynchronous call to perform the query
            FeedResponse<dynamic> result = await equalityQuery.ExecuteNextAsync();

            // Display our results
            foreach (var item in result.ToList())
            {
                Console.WriteLine(item);
            }

            // Display request charge from asynchronous response
            Console.WriteLine("Request Charge: {0}", result.RequestCharge);
            Console.WriteLine("Press enter key to continue********************************************");
            Console.ReadKey();
            Console.WriteLine();
        }

        private static async Task QueryWithTwoFilters(Uri collectionUri)
        {
            Console.WriteLine("Filter on two properties. Find families where id is 'AndersenFamily' OR city is 'NY'");
            Console.WriteLine("SELECT f.LastName AS Name, f.Address.City AS City \n" + 
                "\t FROM Families f \n " +
                "\t WHERE f.id = 'AndersenFamily' OR f.Address.City = 'NY'");
            Console.WriteLine();

            // Query using a double filter on id and city
            IDocumentQuery<dynamic> q = client.CreateDocumentQuery(collectionUri,
                "SELECT f.LastName AS Name, f.Address.City AS City " +
                "FROM Families f " +
                "WHERE f.id = 'AndersenFamily' OR f.Address.City = 'NY'", DefaultOptions).AsDocumentQuery();

            // Asynchronous call to perform the query
            FeedResponse<dynamic> result = await q.ExecuteNextAsync();

            // Display our results
            foreach (var item in result.ToList())
            {
                Console.WriteLine("The {0} family live in {1}", item.Name, item.City);
            }

            // Display request charge from asynchronous response
            Console.WriteLine("Request Charge: {0}", result.RequestCharge);
            Console.WriteLine("Press enter key to continue********************************************");
            Console.ReadKey();
            Console.WriteLine("");
        }

        private static async Task QueryWithRangeOperatorsDateTimes(Uri collectionUri)
        {
            Console.WriteLine("Query using a range operator on a date time");
            Console.WriteLine("SELECT * FROM c WHERE c.RegistrationDate >= <<DateTime.UtcNow.AddDays(-3).ToString(\"o\"))>>");
            Console.WriteLine();

            // Query using a range operator on a datetime.
            IDocumentQuery<dynamic> q = client.CreateDocumentQuery(collectionUri,
                string.Format("SELECT * FROM c WHERE c.RegistrationDate >= '{0}'",
                DateTime.UtcNow.AddDays(-3).ToString("o")), DefaultOptions).AsDocumentQuery();

            // Asynchronous call to perform the query
            FeedResponse<dynamic> result = await q.ExecuteNextAsync();

            // Display our results
            foreach (var item in result.ToList())
            {
                Console.WriteLine("The {0} family registered within the last 3 days", item.LastName);
            }

            // Display request charge from asynchronous response
            Console.WriteLine("Request Charge: {0}", result.RequestCharge);
            Console.WriteLine("Press enter key to continue********************************************");
            Console.ReadKey();
            Console.WriteLine();
        }

        private static async Task QueryWithSingleJoin(Uri collectionUri)
        {
            Console.WriteLine("Query using a single join on Families and Children");
            Console.WriteLine("SELECT f.id \n" +
                "\tFROM Families f \n" +
                "\tJOIN c IN f.Children");
            Console.WriteLine();

            // Query using a single join on families and children
            IDocumentQuery<dynamic> q = client.CreateDocumentQuery(collectionUri,
                "SELECT f.id " +
                "FROM Families f " +
                "JOIN c IN f.Children", DefaultOptions).AsDocumentQuery();

            // Asynchronous call to perform the query
            FeedResponse<dynamic> result = await q.ExecuteNextAsync();

            // Display our results
            foreach (var item in result)
            {
                Console.WriteLine(JsonConvert.SerializeObject(item));
            }

            // Display request charge from asynchronous response
            Console.WriteLine("Request Charge: {0}", result.RequestCharge);
            Console.WriteLine("Press enter key to continue********************************************");
            Console.ReadKey();
            Console.WriteLine("");
        }

        private static async Task QueryWithDoubleJoin(Uri collectionUri)
        {
            Console.WriteLine("Query using a double join on Families, Children, and Pet");
            Console.WriteLine("SELECT f.id as family, c.FirstName AS child, p.GivenName AS pet \n" +
                "\tFROM Families f \n" +
                "\tJOIN c IN f.Children \n" +
                "\tJOIN p IN c.Pets ");

            // Query using a single join on families and children
            IDocumentQuery<dynamic> q = client.CreateDocumentQuery(collectionUri,
                "SELECT f.id as family, c.FirstName AS child, p.GivenName AS pet " +
                "FROM Families f " +
                "JOIN c IN f.Children " +
                "JOIN p IN c.Pets ", DefaultOptions).AsDocumentQuery();

            // Asynchronous call to perform the query
            FeedResponse<dynamic> result = await q.ExecuteNextAsync();

            // Display our results
            foreach (var item in result)
            {
                Console.WriteLine(JsonConvert.SerializeObject(item));
            }

            // Display request charge from asynchronous response
            Console.WriteLine("Request Charge: {0}", result.RequestCharge);
        }

        /// <summary>
        /// Creates the documents used in this Sample
        /// </summary>
        /// <param name="collectionUri">The selfLink property for the DocumentCollection where documents will be created.</param>
        /// <returns>None</returns>
        private static async Task CreateDocuments(Uri collectionUri)
        {
            Family AndersonFamily = new Family
            {
                Id = "AndersenFamily",
                LastName = "Andersen",
                Parents = new Parent[] 
                {
                    new Parent { FirstName = "Thomas" },
                    new Parent { FirstName = "Mary Kay"}
                },
                Children = new Child[] 
                {
                    new Child
                    { 
                        FirstName = "Henriette Thaulow", 
                        Gender = "female", 
                        Grade = 5, 
                        Pets = new [] 
                        {
                            new Pet { GivenName = "Fluffy" } 
                        }
                    } 
                },
                Address = new Address { State = "WA", County = "King", City = "Seattle" },
                IsRegistered = true,
                RegistrationDate = DateTime.UtcNow.AddDays(-1)
            };

            await client.UpsertDocumentAsync(collectionUri, AndersonFamily);

            Family WakefieldFamily = new Family
            {
                Id = "WakefieldFamily",
                LastName = "Wakefield",
                Parents = new[] {
                    new Parent { FamilyName= "Wakefield", FirstName= "Robin" },
                    new Parent { FamilyName= "Miller", FirstName= "Ben" }
                },
                Children = new Child[] {
                    new Child
                    {
                        FamilyName= "Merriam", 
                        FirstName= "Jesse", 
                        Gender= "female", 
                        Grade= 8,
                        Pets= new Pet[] {
                            new Pet { GivenName= "Goofy" },
                            new Pet { GivenName= "Shadow" }
                        }
                    },
                    new Child
                    {
                        FirstName= "Lisa", 
                        Gender= "female", 
                        Grade= 1
                    }
                },
                Address = new Address { State = "NY", County = "Manhattan", City = "NY" },
                IsRegistered = false,
                RegistrationDate = DateTime.UtcNow.AddDays(-30)
            };

            await client.UpsertDocumentAsync(collectionUri, WakefieldFamily);
        }

        /// <summary>
        /// Get a DocuemntCollection by id, or create a new one if one with the id provided doesn't exist.
        /// </summary>
        /// <param name="id">The id of the DocumentCollection to search for, or create.</param>
        /// <returns>The matched, or created, DocumentCollection object</returns>
        private static async Task<DocumentCollection> GetOrCreateCollectionAsync(string databaseId, string collectionId)
        {
            DocumentCollection collectionDefinition = new DocumentCollection();
            collectionDefinition.Id = collectionId;
            collectionDefinition.IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });
            collectionDefinition.PartitionKey.Paths.Add("/LastName");

            return await client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(databaseId),
                collectionDefinition,
                new RequestOptions { OfferThroughput = 400 });
        }

        /// <summary>
        /// Log exception error message to the console
        /// </summary>
        /// <param name="e">The caught exception.</param>
        private static void LogException(Exception e)
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            Exception baseException = e.GetBaseException();
            if (e is DocumentClientException)
            {
                DocumentClientException de = (DocumentClientException)e;
                Console.WriteLine("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
            }
            else
            {
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }

            Console.ForegroundColor = color;
        }

        private static void Assert(string message, bool condition)
        {
            if (!condition)
            {
                throw new ApplicationException(message);
            }
        }

        private static void AssertSequenceEqual(string message, List<Family> list1, List<Family> list2)
        {
            if (!string.Join(",", list1.Select(family => family.Id).ToArray()).Equals(
                string.Join(",", list1.Select(family => family.Id).ToArray())))
            {
                throw new ApplicationException(message);
            }
        }

        internal sealed class Parent
        {
            public string FamilyName { get; set; }
            public string FirstName { get; set; }
        }

        internal sealed class Child
        {
            public string FamilyName { get; set; }
            public string FirstName { get; set; }
            public string Gender { get; set; }
            public int Grade { get; set; }
            public Pet[] Pets { get; set; }
        }

        internal sealed class Pet
        {
            public string GivenName { get; set; }
        }

        internal sealed class Address
        {
            public string State { get; set; }
            public string County { get; set; }
            public string City { get; set; }
        }

        internal sealed class Family
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }

            public string LastName { get; set; }

            public Parent[] Parents { get; set; }

            public Child[] Children { get; set; }

            public Address Address { get; set; }

            public bool IsRegistered { get; set; }

            public DateTime RegistrationDate { get; set;  }
        }
    }
}
