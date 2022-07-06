using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components
{
    public class CollectorComponent : LogicComponent
    {
        public override string ComponentName => "Data Collector";

        private LiveSplitState State { get; set; }
        private CollectorSettings Settings { get; set; }

        private readonly HttpClient wb;

        public int Resets { get; set; }

        public CollectorComponent(LiveSplitState state)
        {
            State = state;
            Settings = new CollectorSettings();

            wb = new HttpClient();

            Resets = 0;

            State.OnStart += StartToSheet;
            State.OnSplit += SplitToSheet;
            State.OnSkipSplit += SkipSheet;
            State.OnUndoSplit += UndoSheet;
            State.OnReset += EndSheet;
        }

        public async Task UpdateData(bool isStart = false)
        {
            // Get as early as possible.
            TimeSpan? CurrentTime = State.CurrentTime[State.CurrentTimingMethod];

            StringBuilder builder = new StringBuilder();

            builder.Append(Resets.ToString() + ",")
                .Append(State.CurrentSplit.Name + ",")
                .Append(State.Run.Last().PersonalBestSplitTime.RealTime.Value.ToString(@"hh\:mm\:ss\.f") + ",");

            if (!isStart)
                builder.Append(CurrentTime.Value.ToString(@"hh\:mm\:ss\.f") + ",");
            else
                builder.Append(",");

            builder.Append(GetDelta("Personal Best") + ",")
                .Append(GetPrediction() + ",")
                .Append(SumOfBest.CalculateSumOfBest(State.Run).Value.ToString(@"hh\:mm\:ss\.f"));
            /*
                + GetSegmentTime() + "," // Segment Time.
                + GetAllDeltas(true); // Get all segment deltas.
            SavedText = SavedText.Substring(0, SavedText.Length - 1);
            */

            await PostDataAsync("split", builder.ToString());

            string GetAttemptValues()
            {
                int FinishedRunsInHistory = State.Run.AttemptHistory.Where(x => x.Time.RealTime != null).Count();
                var totalFinishedRunsCount = FinishedRunsInHistory + (State.CurrentPhase == TimerPhase.Ended ? 1 : 0);
                return string.Format("{0},{1}", totalFinishedRunsCount, State.Run.AttemptCount);
            }

            string GetSegmentTime()
            {
                if (State.CurrentSplitIndex != 0)
                    return LiveSplitStateHelper.GetPreviousSegmentTime(State, State.CurrentSplitIndex - 1, State.CurrentTimingMethod).Value.ToString(@"hh\:mm\:ss\.f");
                else
                    return "";
            }

            string GetPrediction(string comparison = "Best Segments")
            {
                if (State.CurrentPhase == TimerPhase.Ended)
                {
                    return State.Run.Last().SplitTime[State.CurrentTimingMethod].Value.ToString(@"hh\:mm\:ss\.f");
                }

                // Directly copied from LiveSplit.RunPrediction :D
                TimeSpan? delta = LiveSplitStateHelper.GetLastDelta(State, State.CurrentSplitIndex, comparison, State.CurrentTimingMethod) ?? TimeSpan.Zero;
                var liveDelta = State.CurrentTime[State.CurrentTimingMethod] - State.CurrentSplit.Comparisons[comparison][State.CurrentTimingMethod];
                if (liveDelta > delta)
                    delta = liveDelta;
                return (delta + State.Run.Last().Comparisons[comparison][State.CurrentTimingMethod]).Value.ToString(@"hh\:mm\:ss\.f");
            }

            string GetAllDeltas(bool segmentDelta = false)
            {
                string output = "";
                foreach (var x in State.Run.Comparisons)
                {
                    output += GetDelta(x, segmentDelta) + ",";
                }
                return output;
            }

            string GetDelta(string comparison, bool segmentDelta = false)
            {
                if (State.CurrentSplitIndex != 0)
                {
                    int SplitIndex = (State.CurrentPhase == TimerPhase.Ended ? State.CurrentSplitIndex - 1 : State.CurrentSplitIndex);

                    TimeSpan? delta = (segmentDelta ?
                        LiveSplitStateHelper.GetPreviousSegmentDelta(State, State.CurrentSplitIndex - 1, comparison, State.CurrentTimingMethod)
                        : LiveSplitStateHelper.GetLastDelta(State, SplitIndex, comparison, State.CurrentTimingMethod));


                    if (delta.HasValue)
                    {
                        string PlusMinus;
                        string timestring;

                        if (delta.Value > TimeSpan.Zero)
                            PlusMinus = "+";
                        else
                            PlusMinus = "-";

                        if (State.CurrentSplitIndex <= 0)
                            timestring = "";
                        else if (delta.Value.Minutes == 0)
                            timestring = PlusMinus + delta.Value.ToString(@"ss\.f");
                        else
                            timestring = PlusMinus + delta.Value.ToString(@"mm\:ss\.f");

                        return timestring;
                    }
                    else
                        return "";
                }
                else
                    return "";
            }
        }


        public async void SplitToSheet(object sender, EventArgs e)
        {
            await UpdateData();
        }
        public async void StartToSheet(object sender, EventArgs e)
        {
            await UpdateData(true);
        }

        public async void SkipSheet(object sender, EventArgs e)
        {
            await PostDataAsync("skip", State.CurrentSplit.Name);
        }
         
        public async void EndSheet(object sender, TimerPhase value)
        {
            Resets++;
            await PostDataAsync("end", "");
        }

        public async void UndoSheet(object sender, EventArgs e)
        {
            await PostDataAsync("undo", "");
        }

        public async Task PostDataAsync(string action, string text)
        {
            try
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    ["action"] = action,
                    ["identifier"] = Settings.Path,
                    ["data"] = text
                });

                var response = await wb.PostAsync(Settings.URL, content);
            } catch {  }
        }

        public override void Dispose()
        {
            State.OnStart -= SplitToSheet;
            State.OnSplit -= SplitToSheet;
            State.OnSkipSplit -= SplitToSheet;
            State.OnUndoSplit -= UndoSheet;
            State.OnReset -= EndSheet;
            wb.Dispose();
        }

        public override XmlNode GetSettings(XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public override void SetSettings(XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) {  }
    }
}
