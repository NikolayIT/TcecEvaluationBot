namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery;

    public class ChessPosDbProxy
    {
        private readonly object @lock = new object();

        public ChessPosDbProxy(string address, int port, int numTries = 3, int millisecondsBetweenTries = 1000)
        {
            this.IsOpen = false;
            this.Path = string.Empty;

            var processName = "chess_pos_db";

            /* If the app crashes then it orphans the process. Shouldn't happen.
               The following workaround also limits the number of concurrent dbs to 1.*/
            /*
            {
                var collidingProcessses = System.Diagnostics.Process.GetProcessesByName(processName);
                System.Diagnostics.Debug.WriteLine("Killing " + collidingProcessses.Length + " colliding processes...");
                foreach (var process in collidingProcessses)
                {
                    process.Kill();
                }
            }
            */

            this.Process = new Process();
            this.Process.StartInfo.FileName = processName + ".exe";
            this.Process.StartInfo.Arguments = string.Format("tcp --port {0}", port);
            System.Diagnostics.Debug.WriteLine(this.Process.StartInfo.Arguments);

            // TODO: Setting this to true makes the program hang after a few queries
            //       Find a fix as it will be needed for reporting progress to the user.
            // process.StartInfo.RedirectStandardOutput = true;
            this.Process.StartInfo.RedirectStandardInput = true;
            this.Process.StartInfo.UseShellExecute = false;
            this.Process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            this.Process.StartInfo.CreateNoWindow = true;
            this.Process.Start();

            while (numTries-- > 0)
            {
                try
                {
                    this.Client = new TcpClient(address, port);

                    break;
                }
                catch (SocketException)
                {
                    if (numTries == 0)
                    {
                        this.Process.Kill();
                        throw new InvalidDataException("Cannot open communication channel with the database.");
                    }
                    else
                    {
                        Thread.Sleep(millisecondsBetweenTries);
                    }
                }
                catch
                {
                    this.Process.Kill();
                    throw;
                }
            }
        }

        public bool IsOpen { get; private set; }

        public string Path { get; private set; }

        private TcpClient Client { get; set; }

        private Process Process { get; set; }

        private Dictionary<string, DatabaseSupportManifest> SupportManifests { get; set; } = null;

        private DatabaseManifest Manifest { get; set; } = null;

        public void FetchSupportManifest()
        {
            lock (this.@lock)
            {
                var stream = this.Client.GetStream();
                this.SendMessage(stream, "{\"command\":\"support\"}");

                var response = this.ReceiveMessage(stream);
                var json = JObject.Parse(response);
                if (json.ContainsKey("error"))
                {
                    throw new InvalidDataException("Cannot fetch database info.");
                }

                this.SupportManifests = new Dictionary<string, DatabaseSupportManifest>();
                JObject supportManifestsJson = json["support_manifests"] as JObject;
                foreach ((string key, var value) in supportManifestsJson)
                {
                    this.SupportManifests.Add(key, new DatabaseSupportManifest(value.Value<JObject>()));
                }
            }
        }

        public string GetDefaultDatabaseSchema()
        {
            return this.GetRichestDatabaseSchema();
        }

        public string GetRichestDatabaseSchema()
        {
            int bestR = int.MaxValue;
            string bestName = string.Empty;

            foreach ((string name, var manifest) in this.GetSupportManifests())
            {
                int r = this.GetDatabaseFormatRichness(manifest);
                if (bestName == string.Empty || r > bestR)
                {
                    bestName = name;
                    bestR = r;
                }
            }

            return bestName;
        }

        public Dictionary<string, DatabaseSupportManifest> GetSupportManifests()
        {
            if (this.SupportManifests == null)
            {
                this.FetchSupportManifest();
            }

            return this.SupportManifests;
        }

        public DatabaseSupportManifest GetSupportManifest()
        {
            if (!this.IsOpen)
            {
                return null;
            }

            if (this.SupportManifests == null)
            {
                this.FetchSupportManifest();
            }

            return this.SupportManifests[this.GetDatabaseSchema()];
        }

        public void FetchManifest()
        {
            lock (this.@lock)
            {
                if (!this.IsOpen)
                {
                    return;
                }

                var stream = this.Client.GetStream();
                this.SendMessage(stream, "{\"command\":\"manifest\"}");

                var response = this.ReceiveMessage(stream);
                var json = JObject.Parse(response);
                if (json.ContainsKey("error"))
                {
                    throw new InvalidDataException("Cannot fetch database manifest.");
                }

                this.Manifest = new DatabaseManifest(json["manifest"].Value<JObject>());
            }
        }

        public IList<string> GetSupportedDatabaseSchemas()
        {
            if (this.SupportManifests == null)
            {
                this.FetchSupportManifest();
            }

            return new List<string>(this.SupportManifests.Keys);
        }

        public MergeMode GetMergeMode()
        {
            if (!this.IsOpen)
            {
                throw new InvalidOperationException("Database is not open");
            }

            if (this.SupportManifests == null)
            {
                this.FetchSupportManifest();
            }

            return this.SupportManifests[this.GetDatabaseSchema()].MergeMode;
        }

        public string GetDatabaseSchema()
        {
            if (this.Manifest == null)
            {
                this.FetchManifest();
            }

            return this.Manifest.Schema;
        }

        public Dictionary<string, List<DatabaseMergableFile>> GetMergableFiles()
        {
            lock (this.@lock)
            {
                if (!this.IsOpen)
                {
                    throw new InvalidOperationException("Database is not open");
                }

                var stream = this.Client.GetStream();
                this.SendMessage(stream, "{\"command\":\"mergable_files\"}");

                var response = this.ReceiveMessage(stream);
                var json = JObject.Parse(response);
                if (json.ContainsKey("error"))
                {
                    throw new InvalidDataException("Cannot fetch mergable files.");
                }

                var mergableFiles = new Dictionary<string, List<DatabaseMergableFile>>();
                JObject mergableFilesJson = json["mergable_files"] as JObject;
                foreach ((string partition, var value) in mergableFilesJson)
                {
                    var list = new List<DatabaseMergableFile>();
                    foreach (var val in (JArray)value)
                    {
                        list.Add(new DatabaseMergableFile(val.Value<JObject>()));
                    }

                    mergableFiles.Add(partition, list);
                }

                return mergableFiles;
            }
        }

        public DatabaseInfo GetInfo()
        {
            lock (this.@lock)
            {
                DatabaseInfo info = new DatabaseInfo(this.Path, this.IsOpen);

                if (this.IsOpen)
                {
                    var stream = this.Client.GetStream();
                    this.SendMessage(stream, "{\"command\":\"stats\"}");

                    var response = this.ReceiveMessage(stream);
                    var json = JObject.Parse(response);
                    if (json.ContainsKey("error"))
                    {
                        throw new InvalidDataException("Cannot fetch database info.");
                    }
                    else
                    {
                        info.SetStatsFromJson(json);
                    }
                }

                return info;
            }
        }

        public void Open(string path)
        {
            lock (this.@lock)
            {
                if (this.IsOpen)
                {
                    this.Close();
                }

                this.Path = path;
                path = path.Replace("\\", "\\\\"); // we stringify it naively to json so we need to escape manually

                var stream = this.Client.GetStream();
                this.SendMessage(stream, "{\"command\":\"open\",\"database_path\":\"" + path + "\"}");

                while (true)
                {
                    var response = this.ReceiveMessage(stream);
                    var json = JObject.Parse(response);
                    if (json.ContainsKey("error"))
                    {
                        throw new InvalidDataException("Cannot open database. " + json["error"].ToString());
                    }
                    else if (json.ContainsKey("finished"))
                    {
                        if (json["finished"].Value<bool>() == true)
                        {
                            break;
                        }
                    }
                }

                this.IsOpen = true;
            }
        }

        public QueryResponse Query(string fen)
        {
            string query = "{\"command\":\"query\", \"query\":{\"continuations\":{\"fetch_children\":true,\"fetch_first_game\":true,\"fetch_first_game_for_each_child\":true,\"fetch_last_game\":false,\"fetch_last_game_for_each_child\":false},\"levels\":[\"human\",\"engine\",\"server\"],\"positions\":[{\"fen\":\"" + fen + "\"}],\"results\":[\"win\",\"loss\",\"draw\"],\"token\":\"toktok\",\"transpositions\":{\"fetch_children\":true,\"fetch_first_game\":true,\"fetch_first_game_for_each_child\":true,\"fetch_last_game\":false,\"fetch_last_game_for_each_child\":false},\"retractions\":{\"fetch_first_game_for_each\":true,\"fetch_last_game_for_each\":false}}}";
            return this.ExecuteQuery(query);
        }

        public QueryResponse Query(string fen, string san)
        {
            string query = "{\"command\":\"query\", \"query\":{\"continuations\":{\"fetch_children\":true,\"fetch_first_game\":true,\"fetch_first_game_for_each_child\":true,\"fetch_last_game\":false,\"fetch_last_game_for_each_child\":false},\"levels\":[\"human\",\"engine\",\"server\"],\"positions\":[{\"fen\":\"" + fen + "\", \"move\":\"" + san + "\"}],\"results\":[\"win\",\"loss\",\"draw\"],\"token\":\"toktok\",\"transpositions\":{\"fetch_children\":true,\"fetch_first_game\":true,\"fetch_first_game_for_each_child\":true,\"fetch_last_game\":false,\"fetch_last_game_for_each_child\":false},\"retractions\":{\"fetch_first_game_for_each\":true,\"fetch_last_game_for_each\":false}}}";
            return this.ExecuteQuery(query);
        }

        public void Close()
        {
            lock (this.@lock)
            {
                if (!this.IsOpen)
                {
                    return;
                }

                this.Manifest = null;

                this.Path = string.Empty;
                this.IsOpen = false;

                try
                {
                    var stream = this.Client.GetStream();
                    this.SendMessage(stream, "{\"command\":\"close\"}");

                    var responseJson = JObject.Parse(this.ReceiveMessage(stream));
                    if (responseJson.ContainsKey("error"))
                    {
                        throw new InvalidDataException(responseJson["error"].ToString());
                    }
                }
                catch
                {
                }
            }
        }

        public void Dispose()
        {
            lock (this.@lock)
            {
                try
                {
                    var stream = this.Client.GetStream();
                    this.SendMessage(stream, "{\"command\":\"exit\"}");
                    this.Process.WaitForExit();
                }
                catch
                {
                }

                this.Client.Close();
            }
        }

        public void Dump(List<string> pgns, string outPath, List<string> tempPaths, int minCount, int maxPly, Action<JObject> callback)
        {
            lock (this.@lock)
            {
                var stream = this.Client.GetStream();

                var json = new JObject
                {
                    { "command", "dump" },
                    { "output_path", outPath },
                };
                json.Add("temporary_paths", new JArray(tempPaths));
                json.Add("min_count", minCount);
                json.Add("max_ply", maxPly);
                json.Add("report_progress", true);

                json.Add("pgns", new JArray(pgns));

                this.SendMessage(stream, json.ToString());

                while (true)
                {
                    var response = this.ReceiveMessage(stream);
                    var responseJson = JObject.Parse(response);
                    if (responseJson.ContainsKey("error"))
                    {
                        throw new InvalidDataException(responseJson["error"].ToString());
                    }
                    else if (responseJson.ContainsKey("operation"))
                    {
                        if (responseJson["operation"].Value<string>() == "import"
                            || responseJson["operation"].Value<string>() == "dump")
                        {
                            callback.Invoke(responseJson);
                        }

                        if (responseJson["operation"].Value<string>() == "dump")
                        {
                            if (responseJson["finished"].Value<bool>() == true)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void Create(JObject json, Action<JObject> callback)
        {
            lock (this.@lock)
            {
                var stream = this.Client.GetStream();

                this.SendMessage(stream, json.ToString());

                while (true)
                {
                    var response = this.ReceiveMessage(stream);
                    var responseJson = JObject.Parse(response);
                    if (responseJson.ContainsKey("error"))
                    {
                        throw new InvalidDataException(responseJson["error"].ToString());
                    }
                    else if (responseJson.ContainsKey("operation"))
                    {
                        if (responseJson["operation"].Value<string>() == "import"
                            || responseJson["operation"].Value<string>() == "merge")
                        {
                            callback.Invoke(responseJson);
                        }
                        else if (responseJson["operation"].Value<string>() == "create")
                        {
                            if (responseJson["finished"].Value<bool>() == true)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void Append(JObject json, Action<JObject> callback)
        {
            lock (this.@lock)
            {
                var stream = this.Client.GetStream();

                this.SendMessage(stream, json.ToString());

                while (true)
                {
                    var response = this.ReceiveMessage(stream);
                    var responseJson = JObject.Parse(response);
                    if (responseJson.ContainsKey("error"))
                    {
                        throw new InvalidDataException(responseJson["error"].ToString());
                    }
                    else if (responseJson.ContainsKey("operation"))
                    {
                        if (responseJson["operation"].Value<string>() == "import"
                            || responseJson["operation"].Value<string>() == "merge")
                        {
                            callback.Invoke(responseJson);
                        }
                        else if (responseJson["operation"].Value<string>() == "append")
                        {
                            if (responseJson["finished"].Value<bool>() == true)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void Merge(string partition, List<string> names, List<string> temps, ulong? maxSpace, Action<JObject> callback)
        {
            lock (this.@lock)
            {
                JObject request = new JObject
                {
                    { "command", "merge" },
                    { "report_progress", true },
                    { "partition", partition },
                    { "files", new JArray(names) },
                    { "temporary_paths", new JArray(temps) },
                };
                if (maxSpace.HasValue)
                {
                    request.Add("temporary_space", maxSpace.Value);
                }

                var stream = this.Client.GetStream();

                this.SendMessage(stream, request.ToString());

                while (true)
                {
                    var response = this.ReceiveMessage(stream);
                    var responseJson = JObject.Parse(response);
                    if (responseJson.ContainsKey("error"))
                    {
                        throw new InvalidDataException(responseJson["error"].ToString());
                    }
                    else if (responseJson.ContainsKey("operation"))
                    {
                        if (responseJson["operation"].Value<string>() == "merge")
                        {
                            callback.Invoke(responseJson.Value<JObject>());

                            if (responseJson.ContainsKey("finished") && responseJson["finished"].Value<bool>() == true)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        internal IList<string> GetSupportedExtensionsForType(string name)
        {
            if (this.SupportManifests == null)
            {
                this.FetchSupportManifest();
            }

            return this.SupportManifests[name].SupportedExtensions;
        }

        private string ReceiveMessage(NetworkStream stream)
        {
            uint maxMessageLength = 4 * 1024 * 1024;
            uint xorValue = 3173045653u;

            byte[] readLengthBuffer = new byte[8];
            if (stream.Read(readLengthBuffer, 0, 8) != 8)
            {
                throw new InvalidDataException("Message length not received in one packet.");
            }

            uint length = 0;
            length += readLengthBuffer[3];
            length *= 256;
            length += readLengthBuffer[2];
            length *= 256;
            length += readLengthBuffer[1];
            length *= 256;
            length += readLengthBuffer[0];
            uint lengthXor = 0;
            lengthXor += readLengthBuffer[7];
            lengthXor *= 256;
            lengthXor += readLengthBuffer[6];
            lengthXor *= 256;
            lengthXor += readLengthBuffer[5];
            lengthXor *= 256;
            lengthXor += readLengthBuffer[4];
            lengthXor ^= xorValue;

            if (length != lengthXor)
            {
                throw new InvalidDataException("Length doesn't match length xor.");
            }
            else if (length == 0)
            {
                return string.Empty;
            }

            if (length > maxMessageLength)
            {
                throw new InvalidDataException("Message too long.");
            }

            byte[] readBuffer = new byte[length];
            int totalRead = 0;
            while (totalRead < length)
            {
                int leftToRead = (int)length - totalRead;
                totalRead += stream.Read(readBuffer, totalRead, leftToRead);
            }

            var response = Encoding.UTF8.GetString(readBuffer);
            return response;
        }

        private int GetDatabaseFormatRichness(DatabaseSupportManifest manifest)
        {
            int r = 0;

            if (manifest.HasWhiteElo || manifest.HasBlackElo)
            {
                r += 1;
            }

            if (manifest.HasCount)
            {
                r += 1;
            }

            if (manifest.HasEloDiff)
            {
                r += 1;
            }

            if (manifest.HasReverseMove)
            {
                r += 1;
            }

            if (manifest.HasFirstGame)
            {
                r += 1;
            }

            if (manifest.HasLastGame)
            {
                r += 1;
            }

            if (manifest.AllowsFilteringTranspositions)
            {
                r += 1;
            }

            if (manifest.AllowsFilteringByEloRange)
            {
                r += 1;
            }

            if (manifest.AllowsFilteringByMonthRange)
            {
                r += 1;
            }

            return r;
        }

        private QueryResponse ExecuteQuery(string query)
        {
            lock (this.@lock)
            {
                var stream = this.Client.GetStream();

                this.SendMessage(stream, query);

                var responseJson = JObject.Parse(this.ReceiveMessage(stream));

                if (responseJson.ContainsKey("error"))
                {
                    throw new InvalidDataException(responseJson["error"].ToString());
                }

                return QueryResponse.FromJson(responseJson);
            }
        }

        private void SendMessage(NetworkStream stream, string message)
        {
            uint xorValue = 3173045653u;

            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            uint size = (uint)bytes.Length;
            uint xoredSize = size ^ xorValue;

            byte[] sizeStr = new byte[8];
            sizeStr[0] = (byte)(size % 256);
            size /= 256;
            sizeStr[1] = (byte)(size % 256);
            size /= 256;
            sizeStr[2] = (byte)(size % 256);
            size /= 256;
            sizeStr[3] = (byte)size;

            sizeStr[4] = (byte)(xoredSize % 256);
            xoredSize /= 256;
            sizeStr[5] = (byte)(xoredSize % 256);
            xoredSize /= 256;
            sizeStr[6] = (byte)(xoredSize % 256);
            xoredSize /= 256;
            sizeStr[7] = (byte)xoredSize;

            stream.Write(sizeStr, 0, 8);
            stream.Write(bytes, 0, bytes.Length);
        }
    }

    public enum MergeMode
    {
        None,
        Consecutive,
        Any,
    }

    public class DatabaseMergableFile
    {
        public DatabaseMergableFile(JObject json)
        {
            this.Name = json["name"].Value<string>();
            this.Size = json["size"].Value<ulong>();
        }

        public string Name { get; private set; }

        public ulong Size { get; private set; }
    }

    public class DatabaseSingleLevelStats
    {
        public DatabaseSingleLevelStats(JObject json)
        {
            this.NumGames = json["num_games"].Value<ulong>();
            this.NumPositions = json["num_positions"].Value<ulong>();
            this.TotalWhiteElo = json["total_white_elo"].Value<ulong>();
            this.TotalBlackElo = json["total_black_elo"].Value<ulong>();
            this.NumGamesWithElo = json["num_games_with_elo"].Value<ulong>();
            this.NumGamesWithDate = json["num_games_with_date"].Value<ulong>();

            if (this.NumGamesWithElo > 0)
            {
                this.MinElo = json["min_elo"].Value<ulong>();
                this.MaxElo = json["max_elo"].Value<ulong>();
            }

            if (this.NumGamesWithDate > 0)
            {
                this.MinDate = Date.FromString(json["min_date"].Value<string>(), '-');
                this.MaxDate = Date.FromString(json["max_date"].Value<string>(), '-');
            }
        }

        public DatabaseSingleLevelStats(DatabaseSingleLevelStats other)
        {
            this.NumGames = other.NumGames;
            this.NumPositions = other.NumPositions;
            this.TotalWhiteElo = other.TotalWhiteElo;
            this.TotalBlackElo = other.TotalBlackElo;
            this.NumGamesWithElo = other.NumGamesWithElo;
            this.NumGamesWithDate = other.NumGamesWithDate;
            this.MinElo = other.MinElo;
            this.MaxElo = other.MaxElo;
            this.MinDate = other.MinDate;
            this.MaxDate = other.MaxDate;
        }

        public ulong NumGames { get; private set; }

        public ulong NumPositions { get; private set; }

        public ulong TotalWhiteElo { get; private set; }

        public ulong TotalBlackElo { get; private set; }

        public ulong NumGamesWithElo { get; private set; }

        public ulong NumGamesWithDate { get; private set; }

        public ulong MinElo { get; private set; }

        public ulong MaxElo { get; private set; }

        public Date MinDate { get; private set; }

        public Date MaxDate { get; private set; }

        public void Add(DatabaseSingleLevelStats other)
        {
            this.NumGames += other.NumGames;
            this.NumPositions += other.NumPositions;
            this.TotalWhiteElo += other.TotalWhiteElo;
            this.TotalBlackElo += other.TotalBlackElo;

            if (this.NumGamesWithElo == 0)
            {
                this.MinElo = other.MinElo;
                this.MaxElo = other.MaxElo;
            }
            else if (other.NumGamesWithElo != 0)
            {
                this.MinElo = Math.Min(this.MinElo, other.MinElo);
                this.MaxElo = Math.Min(this.MaxElo, other.MaxElo);
            }

            if (this.NumGamesWithDate == 0)
            {
                this.MinDate = other.MinDate;
                this.MaxDate = other.MaxDate;
            }
            else if (other.NumGamesWithDate != 0)
            {
                this.MinDate = Date.Min(this.MinDate, other.MinDate);
                this.MaxDate = Date.Max(this.MaxDate, other.MaxDate);
            }

            this.NumGamesWithElo += other.NumGamesWithElo;
            this.NumGamesWithDate += other.NumGamesWithDate;
        }
    }

    public class DatabaseSingleLevelImportStats : DatabaseSingleLevelStats
    {
        public DatabaseSingleLevelImportStats(JObject json)
            : base(json)
        {
            this.NumSkippedGames = json["num_skipped_games"].Value<ulong>();
        }

        public DatabaseSingleLevelImportStats(DatabaseSingleLevelImportStats other)
            : base(other)
        {
            this.NumSkippedGames = other.NumSkippedGames;
        }

        public ulong NumSkippedGames { get; private set; }

        public void Add(DatabaseSingleLevelImportStats other)
        {
            base.Add(other);
            this.NumSkippedGames += other.NumSkippedGames;
        }
    }

    public class DatabaseStats
    {
        public DatabaseStats(JObject json)
        {
            this.StatsByLevel = new Dictionary<GameLevel, DatabaseSingleLevelStats>
            {
                { GameLevel.Engine, new DatabaseSingleLevelStats(json["engine"].Value<JObject>()) },
                { GameLevel.Human, new DatabaseSingleLevelStats(json["human"].Value<JObject>()) },
                { GameLevel.Server, new DatabaseSingleLevelStats(json["server"].Value<JObject>()) },
            };
        }

        public Dictionary<GameLevel, DatabaseSingleLevelStats> StatsByLevel { get; private set; }

        public DatabaseSingleLevelStats GetTotal()
        {
            DatabaseSingleLevelStats total = new DatabaseSingleLevelStats(this.StatsByLevel[GameLevel.Engine]);
            total.Add(this.StatsByLevel[GameLevel.Human]);
            total.Add(this.StatsByLevel[GameLevel.Server]);
            return total;
        }
    }

    public class DatabaseImportStats
    {
        public DatabaseImportStats(JObject json)
        {
            this.StatsByLevel = new Dictionary<GameLevel, DatabaseSingleLevelImportStats>
            {
                { GameLevel.Engine, new DatabaseSingleLevelImportStats(json["engine"].Value<JObject>()) },
                { GameLevel.Human, new DatabaseSingleLevelImportStats(json["human"].Value<JObject>()) },
                { GameLevel.Server, new DatabaseSingleLevelImportStats(json["server"].Value<JObject>()) },
            };
        }

        public Dictionary<GameLevel, DatabaseSingleLevelImportStats> StatsByLevel { get; private set; }

        public DatabaseSingleLevelImportStats GetTotal()
        {
            DatabaseSingleLevelImportStats total = new DatabaseSingleLevelImportStats(this.StatsByLevel[GameLevel.Engine]);
            total.Add(this.StatsByLevel[GameLevel.Human]);
            total.Add(this.StatsByLevel[GameLevel.Server]);
            return total;
        }
    }

    public class DatabaseManifest
    {
        public DatabaseManifest(JObject json)
        {
            this.Schema = json["schema"].Value<string>();
            this.Version = json["version"].Value<string>();
        }

        public string Schema { get; private set; }

        public string Version { get; private set; }
    }

    public class DatabaseSupportManifest
    {
        public DatabaseSupportManifest(JObject json)
        {
            this.SupportedExtensions = new List<string>();

            foreach (var ext in json["supported_file_types"].Value<JArray>())
            {
                this.SupportedExtensions.Add(ext.Value<string>());
            }

            switch (json["merge_mode"].Value<string>())
            {
                case "none":
                    this.MergeMode = MergeMode.None;
                    break;

                case "consecutive":
                    this.MergeMode = MergeMode.Consecutive;
                    break;

                case "any":
                    this.MergeMode = MergeMode.Any;
                    break;
            }

            this.MaxGames = json["max_games"].Value<ulong>();
            this.MaxPositions = json["max_positions"].Value<ulong>();
            this.MaxInstancesOfSinglePosition = json["max_instances_of_single_position"].Value<ulong>();

            this.HasOneWayKey = json["has_one_way_key"].Value<bool>();
            if (this.HasOneWayKey)
            {
                this.EstimatedMaxCollisions = json["estimated_max_collisions"].Value<ulong>();
                this.EstimatedMaxPositionsWithNoCollisions = json["estimated_max_positions_with_no_collisions"].Value<ulong>();
            }

            this.HasCount = json["has_count"].Value<bool>();

            this.HasEloDiff = json["has_elo_diff"].Value<bool>();
            if (this.HasEloDiff)
            {
                this.MaxAbsEloDiff = json["max_abs_elo_diff"].Value<ulong>();
                this.MaxAverageAbsEloDiff = json["max_average_abs_elo_diff"].Value<ulong>();
            }

            this.HasWhiteElo = json["has_white_elo"].Value<bool>();
            this.HasBlackElo = json["has_black_elo"].Value<bool>();
            if (this.HasWhiteElo || this.HasBlackElo)
            {
                this.MinElo = json["min_elo"].Value<ulong>();
                this.MaxElo = json["max_elo"].Value<ulong>();
                this.HasCountWithElo = json["has_count_with_elo"].Value<bool>();
            }

            this.HasFirstGame = json["has_first_game"].Value<bool>();
            this.HasLastGame = json["has_last_game"].Value<bool>();

            this.AllowsFilteringTranspositions = json["allows_filtering_transpositions"].Value<bool>();
            this.HasReverseMove = json["has_reverse_move"].Value<bool>();

            this.AllowsFilteringByEloRange = json["allows_filtering_by_elo_range"].Value<bool>();
            this.EloFilterGranularity = json["elo_filter_granularity"].Value<ulong>();

            this.AllowsFilteringByMonthRange = json["allows_filtering_by_month_range"].Value<bool>();
            this.MonthFilterGranularity = json["month_filter_granularity"].Value<ulong>();

            this.MaxBytesPerPosition = json["max_bytes_per_position"].Value<ulong>();

            if (json.ContainsKey("estimated_average_bytes_per_position"))
            {
                this.EstimatedAverageBytesPerPosition = Optional<ulong>.Create(json["estimated_average_bytes_per_position"].Value<ulong>());
            }
            else
            {
                this.EstimatedAverageBytesPerPosition = Optional<ulong>.CreateEmpty();
            }
        }

        public IList<string> SupportedExtensions { get; private set; }

        public MergeMode MergeMode { get; private set; }

        public ulong MaxGames { get; private set; }

        public ulong MaxPositions { get; private set; }

        public ulong MaxInstancesOfSinglePosition { get; private set; }

        public bool HasOneWayKey { get; private set; }

        public ulong EstimatedMaxCollisions { get; private set; }

        public ulong EstimatedMaxPositionsWithNoCollisions { get; private set; }

        public bool HasCount { get; private set; }

        public bool HasEloDiff { get; private set; }

        public ulong MaxAbsEloDiff { get; private set; }

        public ulong MaxAverageAbsEloDiff { get; private set; }

        public bool HasWhiteElo { get; private set; }

        public bool HasBlackElo { get; private set; }

        public ulong MinElo { get; private set; }

        public ulong MaxElo { get; private set; }

        public bool HasCountWithElo { get; private set; }

        public bool HasFirstGame { get; private set; }

        public bool HasLastGame { get; private set; }

        public bool AllowsFilteringTranspositions { get; private set; }

        public bool HasReverseMove { get; private set; }

        public bool AllowsFilteringByEloRange { get; private set; }

        public ulong EloFilterGranularity { get; private set; }

        public bool AllowsFilteringByMonthRange { get; private set; }

        public ulong MonthFilterGranularity { get; private set; }

        public ulong MaxBytesPerPosition { get; private set; }

        public Optional<ulong> EstimatedAverageBytesPerPosition { get; private set; }
    }

    public class DatabaseInfo
    {
        public DatabaseInfo(string path, bool isOpen)
        {
            this.Path = path;
            this.IsOpen = isOpen;
            this.Stats = null;
        }

        public bool IsOpen { get; private set; }

        public string Path { get; private set; }

        public DatabaseStats Stats { get; private set; }

        public void SetStatsFromJson(JObject json)
        {
            this.Stats = new DatabaseStats(json);
        }
    }
}
