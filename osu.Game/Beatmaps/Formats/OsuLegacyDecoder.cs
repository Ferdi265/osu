﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OpenTK.Graphics;
using osu.Game.Beatmaps.Timing;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Beatmaps.ControlPoints;

namespace osu.Game.Beatmaps.Formats
{
    public class OsuLegacyDecoder : BeatmapDecoder
    {
        public static void Register()
        {
            AddDecoder<OsuLegacyDecoder>(@"osu file format v14");
            AddDecoder<OsuLegacyDecoder>(@"osu file format v13");
            AddDecoder<OsuLegacyDecoder>(@"osu file format v12");
            AddDecoder<OsuLegacyDecoder>(@"osu file format v11");
            AddDecoder<OsuLegacyDecoder>(@"osu file format v10");
            AddDecoder<OsuLegacyDecoder>(@"osu file format v9");
            AddDecoder<OsuLegacyDecoder>(@"osu file format v8");
            AddDecoder<OsuLegacyDecoder>(@"osu file format v7");
            AddDecoder<OsuLegacyDecoder>(@"osu file format v6");
            AddDecoder<OsuLegacyDecoder>(@"osu file format v5");
            // TODO: Not sure how far back to go, or differences between versions
        }

        private ConvertHitObjectParser parser;

        private readonly Dictionary<string, string> variables = new Dictionary<string, string>();

        private LegacySampleBank defaultSampleBank;
        private int defaultSampleVolume = 100;

        private readonly int beatmapVersion;

        public OsuLegacyDecoder()
        {
        }

        public OsuLegacyDecoder(string header)
        {
            beatmapVersion = int.Parse(header.Substring(17));
        }

        private enum Section
        {
            None,
            General,
            Editor,
            Metadata,
            Difficulty,
            Events,
            TimingPoints,
            Colours,
            HitObjects,
            Variables,
        }

        private void handleGeneral(Beatmap beatmap, string key, string value)
        {
            var metadata = beatmap.BeatmapInfo.Metadata;
            switch (key)
            {
                case @"AudioFilename":
                    metadata.AudioFile = value;
                    break;
                case @"AudioLeadIn":
                    beatmap.BeatmapInfo.AudioLeadIn = int.Parse(value);
                    break;
                case @"PreviewTime":
                    metadata.PreviewTime = int.Parse(value);
                    break;
                case @"Countdown":
                    beatmap.BeatmapInfo.Countdown = int.Parse(value) == 1;
                    break;
                case @"SampleSet":
                    defaultSampleBank = (LegacySampleBank)Enum.Parse(typeof(LegacySampleBank), value);
                    break;
                case @"SampleVolume":
                    defaultSampleVolume = int.Parse(value);
                    break;
                case @"StackLeniency":
                    beatmap.BeatmapInfo.StackLeniency = float.Parse(value, NumberFormatInfo.InvariantInfo);
                    break;
                case @"Mode":
                    beatmap.BeatmapInfo.RulesetID = int.Parse(value);

                    switch (beatmap.BeatmapInfo.RulesetID)
                    {
                        case 0:
                            parser = new Rulesets.Objects.Legacy.Osu.ConvertHitObjectParser();
                            break;
                        case 1:
                            parser = new Rulesets.Objects.Legacy.Taiko.ConvertHitObjectParser();
                            break;
                        case 2:
                            parser = new Rulesets.Objects.Legacy.Catch.ConvertHitObjectParser();
                            break;
                        case 3:
                            parser = new Rulesets.Objects.Legacy.Mania.ConvertHitObjectParser();
                            break;
                    }
                    break;
                case @"LetterboxInBreaks":
                    beatmap.BeatmapInfo.LetterboxInBreaks = int.Parse(value) == 1;
                    break;
                case @"SpecialStyle":
                    beatmap.BeatmapInfo.SpecialStyle = int.Parse(value) == 1;
                    break;
                case @"WidescreenStoryboard":
                    beatmap.BeatmapInfo.WidescreenStoryboard = int.Parse(value) == 1;
                    break;
            }
        }

        private void handleEditor(Beatmap beatmap, string key, string value)
        {
            switch (key)
            {
                case @"Bookmarks":
                    beatmap.BeatmapInfo.StoredBookmarks = value;
                    break;
                case @"DistanceSpacing":
                    beatmap.BeatmapInfo.DistanceSpacing = double.Parse(value, NumberFormatInfo.InvariantInfo);
                    break;
                case @"BeatDivisor":
                    beatmap.BeatmapInfo.BeatDivisor = int.Parse(value);
                    break;
                case @"GridSize":
                    beatmap.BeatmapInfo.GridSize = int.Parse(value);
                    break;
                case @"TimelineZoom":
                    beatmap.BeatmapInfo.TimelineZoom = double.Parse(value, NumberFormatInfo.InvariantInfo);
                    break;
            }
        }

        private void handleMetadata(Beatmap beatmap, string key, string value)
        {
            var metadata = beatmap.BeatmapInfo.Metadata;
            switch (key)
            {
                case @"Title":
                    metadata.Title = value;
                    break;
                case @"TitleUnicode":
                    metadata.TitleUnicode = value;
                    break;
                case @"Artist":
                    metadata.Artist = value;
                    break;
                case @"ArtistUnicode":
                    metadata.ArtistUnicode = value;
                    break;
                case @"Creator":
                    metadata.Author = value;
                    break;
                case @"Version":
                    beatmap.BeatmapInfo.Version = value;
                    break;
                case @"Source":
                    beatmap.BeatmapInfo.Metadata.Source = value;
                    break;
                case @"Tags":
                    beatmap.BeatmapInfo.Metadata.Tags = value;
                    break;
                case @"BeatmapID":
                    beatmap.BeatmapInfo.OnlineBeatmapID = int.Parse(value);
                    break;
                case @"BeatmapSetID":
                    beatmap.BeatmapInfo.OnlineBeatmapSetID = int.Parse(value);
                    metadata.OnlineBeatmapSetID = int.Parse(value);
                    break;
            }
        }

        private void handleDifficulty(Beatmap beatmap, string key, string value)
        {
            var difficulty = beatmap.BeatmapInfo.Difficulty;
            switch (key)
            {
                case @"HPDrainRate":
                    difficulty.DrainRate = float.Parse(value, NumberFormatInfo.InvariantInfo);
                    break;
                case @"CircleSize":
                    difficulty.CircleSize = float.Parse(value, NumberFormatInfo.InvariantInfo);
                    break;
                case @"OverallDifficulty":
                    difficulty.OverallDifficulty = float.Parse(value, NumberFormatInfo.InvariantInfo);
                    break;
                case @"ApproachRate":
                    difficulty.ApproachRate = float.Parse(value, NumberFormatInfo.InvariantInfo);
                    break;
                case @"SliderMultiplier":
                    difficulty.SliderMultiplier = float.Parse(value, NumberFormatInfo.InvariantInfo);
                    break;
                case @"SliderTickRate":
                    difficulty.SliderTickRate = float.Parse(value, NumberFormatInfo.InvariantInfo);
                    break;
            }
        }

        /// <summary>
        /// Decodes any beatmap variables present in a line into their real values.
        /// </summary>
        /// <param name="line">The line which may contains variables.</param>
        private void decodeVariables(ref string line)
        {
            while (line.IndexOf('$') >= 0)
            {
                string[] split = line.Split(',');
                for (int i = 0; i < split.Length; i++)
                {
                    var item = split[i];
                    if (item.StartsWith("$") && variables.ContainsKey(item))
                        split[i] = variables[item];
                }

                line = string.Join(",", split);
            }
        }

        private void handleEvents(Beatmap beatmap, string line)
        {
            decodeVariables(ref line);

            string[] split = line.Split(',');

            EventType type;
            if (!Enum.TryParse(split[0], out type))
                throw new InvalidDataException($@"Unknown event type {split[0]}");

            // Todo: Implement the rest
            switch (type)
            {
                case EventType.Video:
                case EventType.Background:
                    string filename = split[2].Trim('"');

                    if (type == EventType.Background)
                        beatmap.BeatmapInfo.Metadata.BackgroundFile = filename;

                    break;
                case EventType.Break:
                    var breakEvent = new BreakPeriod
                    {
                        StartTime = double.Parse(split[1], NumberFormatInfo.InvariantInfo),
                        EndTime = double.Parse(split[2], NumberFormatInfo.InvariantInfo)
                    };

                    if (!breakEvent.HasEffect)
                        return;

                    beatmap.Breaks.Add(breakEvent);
                    break;
            }
        }

        private void handleTimingPoints(Beatmap beatmap, string line)
        {
            string[] split = line.Split(',');

            double time = double.Parse(split[0].Trim(), NumberFormatInfo.InvariantInfo);
            double beatLength = double.Parse(split[1].Trim(), NumberFormatInfo.InvariantInfo);
            double speedMultiplier = beatLength < 0 ? -beatLength / 100.0 : 1;

            TimeSignatures timeSignature = TimeSignatures.SimpleQuadruple;
            if (split.Length >= 3)
                timeSignature = split[2][0] == '0' ? TimeSignatures.SimpleQuadruple : (TimeSignatures)int.Parse(split[2]);

            LegacySampleBank sampleSet = defaultSampleBank;
            if (split.Length >= 4)
                sampleSet = (LegacySampleBank)int.Parse(split[3]);

            //SampleBank sampleBank = SampleBank.Default;
            //if (split.Length >= 5)
            //    sampleBank = (SampleBank)int.Parse(split[4]);

            int sampleVolume = defaultSampleVolume;
            if (split.Length >= 6)
                sampleVolume = int.Parse(split[5]);

            bool timingChange = true;
            if (split.Length >= 7)
                timingChange = split[6][0] == '1';

            bool kiaiMode = false;
            bool omitFirstBarSignature = false;
            if (split.Length >= 8)
            {
                int effectFlags = int.Parse(split[7]);
                kiaiMode = (effectFlags & 1) > 0;
                omitFirstBarSignature = (effectFlags & 8) > 0;
            }

            string stringSampleSet = sampleSet.ToString().ToLower();
            if (stringSampleSet == @"none")
                stringSampleSet = @"normal";

            DifficultyControlPoint difficultyPoint = beatmap.ControlPointInfo.DifficultyPointAt(time);
            SoundControlPoint soundPoint = beatmap.ControlPointInfo.SoundPointAt(time);
            EffectControlPoint effectPoint = beatmap.ControlPointInfo.EffectPointAt(time);

            if (timingChange)
            {
                beatmap.ControlPointInfo.TimingPoints.Add(new TimingControlPoint
                {
                    Time = time,
                    BeatLength = beatLength,
                    TimeSignature = timeSignature
                });
            }

            if (speedMultiplier != difficultyPoint.SpeedMultiplier)
            {
                beatmap.ControlPointInfo.DifficultyPoints.Add(new DifficultyControlPoint
                {
                    Time = time,
                    SpeedMultiplier = speedMultiplier
                });
            }

            if (stringSampleSet != soundPoint.SampleBank || sampleVolume != soundPoint.SampleVolume)
            {
                beatmap.ControlPointInfo.SoundPoints.Add(new SoundControlPoint
                {
                    Time = time,
                    SampleBank = stringSampleSet,
                    SampleVolume = sampleVolume
                });
            }

            if (kiaiMode != effectPoint.KiaiMode || omitFirstBarSignature != effectPoint.OmitFirstBarLine)
            {
                beatmap.ControlPointInfo.EffectPoints.Add(new EffectControlPoint
                {
                    Time = time,
                    KiaiMode = kiaiMode,
                    OmitFirstBarLine = omitFirstBarSignature
                });
            }
        }

        private void handleColours(Beatmap beatmap, string key, string value, ref bool hasCustomColours)
        {
            string[] split = value.Split(',');

            if (split.Length != 3)
                throw new InvalidOperationException($@"Color specified in incorrect format (should be R,G,B): {value}");

            byte r, g, b;
            if (!byte.TryParse(split[0], out r) || !byte.TryParse(split[1], out g) || !byte.TryParse(split[2], out b))
                throw new InvalidOperationException(@"Color must be specified with 8-bit integer components");

            if (!hasCustomColours)
            {
                beatmap.ComboColors.Clear();
                hasCustomColours = true;
            }

            // Note: the combo index specified in the beatmap is discarded
            if (key.StartsWith(@"Combo"))
            {
                beatmap.ComboColors.Add(new Color4
                {
                    R = r / 255f,
                    G = g / 255f,
                    B = b / 255f,
                    A = 1f,
                });
            }
        }

        protected override Beatmap ParseFile(StreamReader stream)
        {
            return new LegacyBeatmap(base.ParseFile(stream));
        }

        public override Beatmap Decode(StreamReader stream)
        {
            return new LegacyBeatmap(base.Decode(stream));
        }

        protected override void ParseFile(StreamReader stream, Beatmap beatmap)
        {
            beatmap.BeatmapInfo.BeatmapVersion = beatmapVersion;

            Section section = Section.None;
            bool hasCustomColours = false;

            string line;
            while ((line = stream.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith(" ") || line.StartsWith("_") || line.StartsWith("//"))
                    continue;

                if (line.StartsWith(@"osu file format v"))
                {
                    beatmap.BeatmapInfo.BeatmapVersion = int.Parse(line.Substring(17));
                    continue;
                }

                if (line.StartsWith(@"[") && line.EndsWith(@"]"))
                {
                    if (!Enum.TryParse(line.Substring(1, line.Length - 2), out section))
                        throw new InvalidDataException($@"Unknown osu section {line}");
                    continue;
                }

                string key = null, value = null;

                switch (section)
                {
                    case Section.General:
                    case Section.Editor:
                    case Section.Metadata:
                    case Section.Difficulty:
                    case Section.Colours:
                        key = line.Remove(line.IndexOf(':')).Trim();
                        value = line.Substring(line.IndexOf(':') + 1).Trim();
                        break;
                    case Section.Variables:
                        key = line.Remove(line.IndexOf('=')).Trim();
                        value = line.Substring(line.IndexOf('=') + 1).Trim();
                        break;
                }

                switch (section)
                {
                    case Section.General:
                        handleGeneral(beatmap, key, value);
                        break;
                    case Section.Editor:
                        handleEditor(beatmap, key, value);
                        break;
                    case Section.Metadata:
                        handleMetadata(beatmap, key, value);
                        break;
                    case Section.Difficulty:
                        handleDifficulty(beatmap, key, value);
                        break;
                    case Section.Events:
                        handleEvents(beatmap, line);
                        break;
                    case Section.TimingPoints:
                        handleTimingPoints(beatmap, line);
                        break;
                    case Section.Colours:
                        handleColours(beatmap, key, value, ref hasCustomColours);
                        break;
                    case Section.HitObjects:
                        var obj = parser.Parse(line);

                        if (obj != null)
                            beatmap.HitObjects.Add(obj);

                        break;
                    case Section.Variables:
                        variables[key] = value;
                        break;
                }
            }

            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.BeatmapInfo.Difficulty);
        }

        internal enum LegacySampleBank
        {
            None = 0,
            Normal = 1,
            Soft = 2,
            Drum = 3
        }

        internal enum EventType
        {
            Background = 0,
            Video = 1,
            Break = 2,
            Colour = 3,
            Sprite = 4,
            Sample = 5,
            Animation = 6
        }
    }
}
