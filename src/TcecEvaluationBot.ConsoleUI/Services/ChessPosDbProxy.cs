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
}
