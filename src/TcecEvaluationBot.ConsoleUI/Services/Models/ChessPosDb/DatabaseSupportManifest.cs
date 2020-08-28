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
}
