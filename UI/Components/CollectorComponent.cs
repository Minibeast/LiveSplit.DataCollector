using System;
using System.Net;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.Model;

namespace LiveSplit.UI.Components
{
    public class CollectorComponent : LogicComponent
    {
        public override string ComponentName => "Data Collector";

        private LiveSplitState State { get; set; }
        private CollectorSettings Settings { get; set; }

        private WebClient wb;

        public CollectorComponent(LiveSplitState state)
        {
            State = state;
            Settings = new CollectorSettings();

            wb = new WebClient();

            State.OnStart += SplitToSheet;
            State.OnSplit += SplitToSheet;
            State.OnSkipSplit += SkipSheet;
            State.OnUndoSplit += UndoSheet;
            State.OnReset += EndSheet;
        }
        public void SplitToSheet(object sender, EventArgs e)
        {
            // Get as early as possible.
            TimeSpan? CurrentTime = State.CurrentTime[State.CurrentTimingMethod];

            // Old version saved text in the component, this is completely worthless and a waste of memory. Let the server handle it.
            string SavedText;

            SavedText = GetAttemptValues() + "," // Finished runs , Total attempt count.
                + State.CurrentSplit.Name + "," // Name.
                + CurrentTime.Value.ToString(@"hh\:mm\:ss\.fff") + "," // Current time.
                + CurrentTime.Value.ToString(@"hh\:mm\:ss") + "," // Current time again.
                + GetAllDeltas() // Get all Deltas.
                + GetPrediction() + "," // BPT.
                + GetSegmentTime() + "," // Segment Time.
                + GetAllDeltas(true); // Get all segment deltas.
            SavedText = SavedText.Substring(0, SavedText.Length - 1);

            Task.Run(() => POST_DATA("split", SavedText));

            string GetAttemptValues()
            {
                int FinishedRunsInHistory = State.Run.AttemptHistory.Where(x => x.Time.RealTime != null).Count();
                var totalFinishedRunsCount = FinishedRunsInHistory + (State.CurrentPhase == TimerPhase.Ended ? 1 : 0);
                return string.Format("{0},{1}", totalFinishedRunsCount, State.Run.AttemptCount);
            }

            string GetSegmentTime()
            {
                if (State.CurrentSplitIndex != 0)
                    return LiveSplitStateHelper.GetPreviousSegmentTime(State, State.CurrentSplitIndex - 1, State.CurrentTimingMethod).Value.ToString(@"hh\:mm\:ss\.ff");
                else
                    return "";
            }

            string GetPrediction(string comparison = "Best Segments")
            {
                if (State.CurrentPhase == TimerPhase.Ended)
                {
                    return State.Run.Last().SplitTime[State.CurrentTimingMethod].Value.ToString(@"hh\:mm\:ss\.ff");
                }

                // Directly copied from LiveSplit.RunPrediction :D
                TimeSpan? delta = LiveSplitStateHelper.GetLastDelta(State, State.CurrentSplitIndex, comparison, State.CurrentTimingMethod) ?? TimeSpan.Zero;
                var liveDelta = State.CurrentTime[State.CurrentTimingMethod] - State.CurrentSplit.Comparisons[comparison][State.CurrentTimingMethod];
                if (liveDelta > delta)
                    delta = liveDelta;
                return (delta + State.Run.Last().Comparisons[comparison][State.CurrentTimingMethod]).Value.ToString(@"hh\:mm\:ss\.ff");
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
                            timestring = PlusMinus + delta.Value.ToString(@"ss\.ff");
                        else
                            timestring = PlusMinus + delta.Value.ToString(@"mm\:ss\.ff");

                        return timestring;
                    }
                    else
                        return "";
                }
                else
                    return "";
            }
        }

        public void SkipSheet(object sender, EventArgs e)
        {
            Task.Run(() => POST_DATA("skip", State.CurrentSplit.Name));
        }

        public void EndSheet(object sender, TimerPhase value)
        {
            Task.Run(() => POST_DATA("end", ""));
        }

        public void UndoSheet(object sender, EventArgs e)
        {
            Task.Run(() => POST_DATA("undo", ""));
        }

        private void POST_DATA(string action, string text)
        {
            string output_info = Settings.Path;
            var data = new NameValueCollection();
            data["action"] = action;
            data["identifier"] = output_info;
            data["data"] = text;

            var response = wb.UploadValues(Settings.URL, "POST", data);
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
